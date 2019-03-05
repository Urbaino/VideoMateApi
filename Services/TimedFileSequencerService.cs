using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using VideoKategoriseringsApi.Models;

namespace VideoKategoriseringsApi.Services
{
    internal class TimedFileSequencerService : IHostedService, IDisposable
    {
        private readonly ILogger _logger;
        private readonly Settings _settings;

        private Timer _timer;

        public TimedFileSequencerService(ILogger<TimedFileSequencerService> logger, IOptions<Settings> settings)
        {
            _logger = logger;
            _settings = settings.Value;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Background Sequencer Service is starting.");

            _timer = new Timer(DoWorkAsync, null, TimeSpan.Zero, TimeSpan.FromSeconds(_settings.SequencerIntervalSeconds));

            return Task.CompletedTask;
        }

        private async void DoWorkAsync(object state)
        {
            _logger.LogInformation("Background Sequencer Service is working.");

            // Go through all the folder
            var files = new List<FileInfo>();
            var folders = Directory.EnumerateDirectories(_settings.DataPath);

            foreach (var folderPath in folders)
            {
                files.AddRange(Directory.EnumerateFiles(folderPath, "*.json", SearchOption.AllDirectories).Select(p => new FileInfo(p)));

            }
            _logger.LogInformation($"{files.Count()} file{(files.Count() == 1 ? string.Empty : "s")} to process.");

            // Go through all JSON files in the folder
            foreach (var fileInfo in files)
            {
                try
                {
                    await ParseAndProcessFileAsync(fileInfo);
                }
                catch (Exception ex)
                {
                    _logger.LogError(0, ex, $"Error processing file: {fileInfo.FullName}");
                }
            }

            _logger.LogInformation("Background Sequencer Service finished working.");
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Background Sequencer Service is stopping.");

            _timer?.Change(Timeout.Infinite, 0);

            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }

        private async Task ParseAndProcessFileAsync(FileInfo fileInfo)
        {
            VideoFile videoFile;
            try
            {
                var fileText = await File.ReadAllTextAsync(fileInfo.FullName);
                videoFile = JObject.Parse(fileText).ToObject<VideoFile>();
            }
            catch (Exception e)
            {
                _logger.LogError(0, e, $"Could not read and parse JSON file: {fileInfo.FullName}");
                return;
            }

            if (!videoFile.IsStatus(VideoFile.Statuses.categorized)) return;

            foreach (var sequence in videoFile.sequences)
            {
                var startTime = (float)sequence.inPoint;
                var duration = (float)(sequence.outPoint - sequence.inPoint);
                var sequenceId = sequence.id;

                SequenceVideo(videoFile.fileName, fileInfo.DirectoryName, startTime, duration, sequenceId);
            }

            // Change status
            videoFile.SetStatus(VideoFile.Statuses.sequences_has_been_processed);
            await File.WriteAllTextAsync(fileInfo.FullName, JObject.FromObject(videoFile).ToString());

            _logger.LogInformation($"File {fileInfo.FullName} sequenced.");
        }

        private void SequenceVideo(string videoFileName, string folderPath, float startTime, float duration, string sequenceId)
        {
            _logger.LogInformation($"Sequencing {videoFileName}:{sequenceId}");

            var formattedStartTime = startTime.ToString(CultureInfo.InvariantCulture);
            var formattedDuration = duration.ToString(CultureInfo.InvariantCulture);
            var newFilePath = videoFileName.Replace(".MP4", $"_SEQ_{sequenceId}.MP4");

            // -i   Input File
            // -ss  Start Time
            // -t   Duration
            // -n   Do not overwrite if existing
            // -c copy  Preserve quality
            // -loglevel warning    Duh
            var process = new Process()
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "ffmpeg",
                    // FileName = $"{_settings.DataPath}/ffmpeg.exe",
                    Arguments = $"-i {videoFileName} -ss {formattedStartTime} -t {formattedDuration} -n -c copy -loglevel warning {newFilePath}",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WorkingDirectory = folderPath
                }
            };
            process.Start();
            string result = process.StandardOutput.ReadToEnd();
            _logger.LogWarning(result);
            process.WaitForExit();

            _logger.LogInformation($"{sequenceId} done!");
        }
    }
}
