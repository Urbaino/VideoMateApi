using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;

namespace VideoKategoriseringsApi.Models
{
    public class VideoFile
    {

        internal bool IsStatus(Statuses s) => status.ToLowerInvariant().Trim() == s.ToString().ToLowerInvariant();
        internal void SetStatus(Statuses newStatus) => status = newStatus.ToString().ToLowerInvariant();

        internal enum Statuses {
            categorized,
            sequences_has_been_processed
        }

        public VideoFile(string folder, string fileName, string thumbNailImageUrl, string type, decimal framerate, string resolution)
        {
            this.folder = folder;
            this.fileName = fileName;
            this.thumbNailImageUrl = thumbNailImageUrl;
            this.type = type;
            this.framerate = framerate;
            this.resolution = resolution;
        }
        public string folder { get; set; }
        public string fileName { get; set; }
        public string thumbNailImageUrl { get; set; }
        public string url;

        public string status { get; set; } = "Not processed";
        public string comment { get; set; }
        public bool rotationRequiresAdjustment { get; set; }
        public bool exposureRequiresAdjustment { get; set; }
        public bool markedAsDeleted {get; set; }

        public Sequence[] sequences { get; set; }

        // Dessa ska läsas in med filen
        public string type { get; set; }
        public decimal framerate { get; set; }
        public string resolution { get; set; }
    }
}