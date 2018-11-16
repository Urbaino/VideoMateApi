using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using VideoKategoriseringsApi.Models;

namespace VideoKategoriseringsApi.Controllers
{
    [Route("api")]
    public class MainController : Controller
    {
        private readonly Settings Settings;

        public MainController(IOptions<Settings> settingsOptions)
        {
            Settings = settingsOptions.Value;
        }

        [HttpPost("save")]
        public IActionResult SaveJson([FromBody]VideoFile video)
        {
            if (video == null)
                return BadRequest("Nu gjorde du fel. Ogiltig JSON.");

            SaveJSONFile(video);
            return Ok();
        }

        [HttpGet("files/{status?}")]
        public IActionResult GetAllFiles(string status)
        {
            bool showAll = string.IsNullOrEmpty(status);

            var allJSONFiles = Directory.EnumerateFiles(Settings.DataPath)
                .Where(x => x.EndsWith(".json"))
                .Select(filename => new FileInfo(filename));

            var data = new List<VideoFile>(allJSONFiles.Count());
            foreach (var json in allJSONFiles)
            {
                var videoFile = ReadJSONFile<VideoFile>(json.FullName);
                if (showAll || videoFile.status.ToLowerInvariant().Trim() == status.ToLowerInvariant().Trim())
                {
                    videoFile.location = Settings.VideoLocationBase + videoFile.location;
                    data.Add(videoFile);
                }
            }
            return Ok(data);
        }

        [HttpGet("process")]
        public IActionResult ProcessMemoryCard()
        {
            foreach (var filePath in Directory.EnumerateFiles(Settings.MemoryCardPath))
            {
                var fileName = Path.GetFileName(filePath);
                var destinationFilePath = Path.Combine(Settings.DataPath, DateTime.Now.ToString("yyyy-MM-dd_hh-mm-ss") + "-" + fileName);
                if (System.IO.File.Exists(destinationFilePath))
                    continue;

                System.IO.File.Copy(filePath, destinationFilePath, false);

                // TODO: Read video file properties.


                SaveJSONFile(new VideoFile(
                    fileName,
                    null,
                    0,
                    null
                ));
            }
            return Ok();
        }

        [HttpGet("purge")]
        public IActionResult Purge()
        {
            var allJSONFilePaths = Directory.EnumerateFiles(Settings.DataPath)
                .Where(x => x.EndsWith(".json"));

            foreach (var jsonPath in allJSONFilePaths)
            {
                var videoFile = ReadJSONFile<VideoFile>(jsonPath);
                if (videoFile.status.ToLowerInvariant().Trim() == "deleted")
                {
                    System.IO.File.Delete(jsonPath);
                    System.IO.File.Delete(new string(jsonPath.SkipLast(5).ToArray()));
                }
            }
            return Ok();
        }


        private T ReadJSONFile<T>(string filename)
        {
            using (var filestream = new FileStream(filename, FileMode.Open))
            {
                using (var reader = new StreamReader(filestream))
                {
                    var line = reader.ReadLine();
                    return JsonConvert.DeserializeObject<T>(line);
                }
            }
        }

        private void SaveJSONFile(VideoFile video)
        {
            var data = JsonConvert.SerializeObject(video);
            var filepath = Path.Combine(Settings.DataPath, Path.GetFileName(video.location) + ".json");
            var filestream = new FileStream(filepath, FileMode.OpenOrCreate, FileAccess.ReadWrite);
            using (var writer = new StreamWriter(filestream))
            {
                writer.Write(data);
                writer.Flush();
                filestream.SetLength(filestream.Position);
            }
        }

        private string RunFFMPEG()
        {
            var process = new Process()
            {
                StartInfo = new ProcessStartInfo
                {
                    //FileName = "/bin/bash",
                    //Arguments = $"-c \"{escapedArgs}\"", 					 
                    FileName = "ping",
                    Arguments = $"localhost",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                }
            };

            process.Start();
            string result = process.StandardOutput.ReadToEnd();
            process.WaitForExit();
            return result;
        }
    }
}
