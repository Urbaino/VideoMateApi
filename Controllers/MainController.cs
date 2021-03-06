﻿using System;
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
using Newtonsoft.Json.Linq;

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
            Console.WriteLine("saving file" + video.url);
            if (video == null)
                return BadRequest("Nu gjorde du fel. Ogiltig JSON.");

            SaveOrUpdateJSONFile(video);
            return Ok("Success");
        }

        [HttpPost("tagSuggestions/{type}/save")]
        public IActionResult SaveTagSuggestions(string type, [FromBody]Tag[] suggestions)
        {
            Console.WriteLine("saving suggestions to file");
            if (suggestions == null)
                return BadRequest("Nu gjorde du fel. Ogiltig JSON.");
            var filePath = Path.Combine(Settings.DataPath, type + "_suggestions.json");
            var data = JsonConvert.SerializeObject(suggestions);
            SaveJSONFile(filePath, data);
            return Ok(new Tag());
        }

        [HttpGet("tagSuggestions/{type}")]
        public IActionResult GetAllTagSuggestions(string type)
        {
            var filePath = Path.Combine(Settings.DataPath, type + "_suggestions.json");
            var data = new Tag[0];
            if (System.IO.File.Exists(filePath)){
                data = ReadJSONFile<Tag[]>(filePath);
            }
            return Ok(data);
        }

        [HttpPost("searchIndex/save")]
        public IActionResult SavePregeneratedIndex([FromBody]JObject index)
        {
            Console.WriteLine("saving index to file");
            if (index == null)
                return BadRequest("Nu gjorde du fel. Ogiltig JSON.");
            var filePath = Path.Combine(Settings.DataPath, "search_index.json");
            var data = JsonConvert.SerializeObject(index);
            SaveJSONFile(filePath, data);
            return Ok(new Tag());
        }

        
        [HttpGet("searchIndex")]
        public IActionResult GetPregeneratedIndex()
        {
            var filePath = Path.Combine(Settings.DataPath, "search_index.json");
            var data = new JObject();
            if (System.IO.File.Exists(filePath)){
                data = ReadJSONFile<JObject>(filePath);
            }
            return Ok(data);
        }

        [HttpGet("statistics")]
        public IActionResult GetStatistics()
        {
            var statistics = new Dictionary<String, int>();
            statistics.Add("totalNrOfFiles", 0);
            statistics.Add("nrOfDeletedFiles", 0);
            statistics.Add("nrOfCategorizedFiles", 0);
            statistics.Add("nrOfFilesToCategorize", 0);
            statistics.Add("nrOfProcessedFiles", 0);
            
            
            foreach(var folder in Directory.EnumerateDirectories(Settings.DataPath))
            {
                var dir = folder.Substring(folder.LastIndexOf(Path.DirectorySeparatorChar) + 1);
                foreach(var file in GetAllVideoFilesInDirectory(dir))
                {
                    statistics["totalNrOfFiles"]++;
                    if(file.status.ToLowerInvariant().Trim() == "categorized")
                    {
                        statistics["nrOfCategorizedFiles"]++;
                    }
                    if(file.status.ToLowerInvariant().Trim() == "sequences_has_been_processed")
                    {
                        statistics["nrOfProcessedFiles"]++;
                    }
                    
                    if(file.markedAsDeleted)
                    {
                        statistics["nrOfDeletedFiles"]++;
                    }
                } 
            }
            statistics["nrOfFilesToCategorize"] = statistics["totalNrOfFiles"] - statistics["nrOfDeletedFiles"] - statistics["nrOfCategorizedFiles"];          
            return Ok(statistics);
        }
        [HttpGet("folders")]
        public IActionResult GetAllFolders()
        {
            List<string> result = new List<string>();
            foreach(var folder in Directory.EnumerateDirectories(Settings.DataPath))
            {
                result.Add(folder.Substring(folder.LastIndexOf(Path.DirectorySeparatorChar) + 1));
            }
            
            return Ok(result);
        }

        [HttpGet("files/{folderName?}")]
        public IActionResult GetAllFiles(string folderName)
        {
           // bool showAll = string.IsNullOrEmpty(status);
            List<VideoFile> files = GetAllVideoFilesInDirectory(folderName);
            return Ok(files);
        }

        private List<VideoFile> GetAllVideoFilesInDirectory(String folderName){
            var allJSONFiles = Directory.EnumerateFiles(Settings.DataPath + "/" + folderName)
                .Where(x => x.EndsWith(".json"))
                .Select(filename => new FileInfo(filename));

            var data = new List<VideoFile>(allJSONFiles.Count());
            foreach (var json in allJSONFiles)
            {
                var videoFile = ReadJSONFile<VideoFile>(json.FullName);
               // if (showAll || videoFile.status.ToLowerInvariant().Trim() == status.ToLowerInvariant().Trim())
               // {
                    videoFile.url = getUrl(videoFile);
                    data.Add(videoFile);
               // }
            }
            return data;
        }


        

        [HttpGet("process")]
        public IActionResult ProcessMemoryCard()
        {
            Console.WriteLine("Processing memorycard");
            foreach (var filePath in Directory.EnumerateFiles(Settings.MemoryCardPath))
            {           

                var fileName = Path.GetFileName(filePath);
                Console.WriteLine(" Processing " + fileName);

                DateTime created = System.IO.File.GetLastWriteTime(filePath); //this is apparenly the only(?) way for us to get original created time...
                string dateTimeFileWasCaptured = created.ToString("yyyy-MM-dd_HH-mm-ss");
                string dateFileWasCaptured = created.ToString("yyyy-MM-dd");
                var destinationFolder = dateFileWasCaptured;                                            //2018-02-14
                var destinationFileName = dateTimeFileWasCaptured + "_" + fileName;                     // 2018-02-14_17-22-02_DJI_0639.mov
                var destinationFolderPath = Path.Combine(Settings.DataPath, destinationFolder);        // c:\....\storage\2018-02-14\
                var destinationFullFilePath = Path.Combine(destinationFolderPath, destinationFileName); // c:\....\storage\2018-02-02\2018-02-14_17-22-02_DJI_0639.mov
               
                if (!System.IO.File.Exists(destinationFolderPath)){
                    System.IO.Directory.CreateDirectory(destinationFolderPath);
                }
                if (System.IO.File.Exists(destinationFullFilePath))
                {
                    Console.WriteLine("     File already processed, ignoring.");
                    continue;
                }
                Console.WriteLine("     Copying file to " + destinationFullFilePath);
                System.IO.File.Copy(filePath, destinationFullFilePath, false);
                
                Console.WriteLine("     Extracting first frame from video as thumbnail");
                var thumbNailImageUrl = ExtractFirstFrameAsBase64(destinationFullFilePath);
                // TODO: Read video file properties.

                Console.WriteLine("     Creating metadatafile");
                SaveOrUpdateJSONFile(new VideoFile(
                    destinationFolder,
                    destinationFileName,
                    thumbNailImageUrl,
                    "video/mp4",        //TODO: remove hardcoded value
                    0,
                    null
                ));
            }
            Console.WriteLine("Processing memorycard has completed.");
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
             string data;
            var filepath = getFilePath(video);
            if (System.IO.File.Exists(filepath)){
                var existingObject = ReadJSONFile<VideoFile>(filepath);
                existingObject.comment = video.comment;
                existingObject.exposureRequiresAdjustment = video.exposureRequiresAdjustment;
                existingObject.rotationRequiresAdjustment = video.rotationRequiresAdjustment;
                existingObject.status = video.status;
                existingObject.sequences = video.sequences;
                existingObject.markedAsDeleted = video.markedAsDeleted;
                data = JsonConvert.SerializeObject(existingObject);
            }
            else
            {
                data = JsonConvert.SerializeObject(video);
            }
            this.SaveJSONFile(filepath, data);
        }

        private string ExtractFirstFrameAsBase64(string videoFilePath)
        {
            var process = new Process()
            {
                StartInfo = new ProcessStartInfo
                {			 
                    FileName = "ffmpeg",
                    Arguments = $"-i \"" + videoFilePath + "\" -nostats -loglevel 0 -vframes 1 -f image2 screendump.jpg",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                }
            };
            process.Start();
            string result = process.StandardOutput.ReadToEnd();
            process.WaitForExit();
            var screendumpPath = Path.Combine(Directory.GetCurrentDirectory(), "screendump.jpg");
            var imageContent = ImageToBase64(screendumpPath);
            System.IO.File.Delete(screendumpPath);
            return imageContent;
        }

        public string getUrl(VideoFile video)
        {
            return Settings.VideoLocationBase + video.folder + "/" + video.fileName;
        }
        public string getFilePath(VideoFile video)
        {
            return Path.Combine(Settings.DataPath, video.folder, video.fileName + ".json");
        }

        public string ImageToBase64(string imagePath)   
        {  
            byte[] b = System.IO.File.ReadAllBytes(imagePath);
            return "data:image/jpeg;base64," + Convert.ToBase64String(b);
        } 
    }
}
