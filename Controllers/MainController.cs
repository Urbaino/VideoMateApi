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
using Microsoft.AspNetCore.Cors;

namespace VideoKategoriseringsApi.Controllers
{
    [Route("api")]
    [EnableCors("AllowAll")]
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
            Console.WriteLine("saving file" + video.location);
            if (video == null)
                return BadRequest("Nu gjorde du fel. Ogiltig JSON.");

            SaveOrUpdateJSONFile(video);
            return Ok();
        }

        [HttpPost("suggestions/save")]
        public IActionResult SaveSuggestions([FromBody]Tag[] suggestions)
        {
            Console.WriteLine("saving suggestions to file");
            if (suggestions == null)
                return BadRequest("Nu gjorde du fel. Ogiltig JSON.");
            var filePath = Path.Combine(Settings.DataPath, "suggestions.json");
            var data = JsonConvert.SerializeObject(suggestions);
            SaveJSONFile(filePath, data);
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
                fileName = DateTime.Now.ToString("yyyy-MM-dd_hh-mm-ss") + "-" + fileName;
                var destinationFilePath = Path.Combine(Settings.DataPath, fileName);
                if (System.IO.File.Exists(destinationFilePath))
                    continue;

                System.IO.File.Copy(filePath, destinationFilePath, false);

                // TODO: Read video file properties.


                SaveOrUpdateJSONFile(new VideoFile(
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

        private void SaveJSONFile(string filepath, string data){
            var filestream = new FileStream(filepath, FileMode.OpenOrCreate, FileAccess.ReadWrite);
            using (var writer = new StreamWriter(filestream))
            {
                writer.Write(data);
                writer.Flush();
                filestream.SetLength(filestream.Position);
            }
        }

        private void SaveOrUpdateJSONFile(VideoFile video)
        {
            var filepath = Path.Combine(Settings.DataPath, Path.GetFileName(video.location) + ".json");
            var existingObject = ReadJSONFile<VideoFile>(filepath);
            string data;
            if(existingObject != null)
            {
                existingObject.comment = video.comment;
                existingObject.exposureRequiresAdjustment = video.exposureRequiresAdjustment;
                existingObject.rotationRequiresAdjustment = video.rotationRequiresAdjustment;
                existingObject.status = video.status;
                existingObject.sequences = video.sequences;
                data = JsonConvert.SerializeObject(existingObject);
            }
            else
            {
                data = JsonConvert.SerializeObject(video);
            }
            this.SaveJSONFile(filepath, data);
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
