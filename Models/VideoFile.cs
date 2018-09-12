namespace VideoKategoriseringsApi.Models
{
    public class VideoFile
    {
        public VideoFile(string location, string type, decimal framerate, string resolution)
        {
            this.location = location;
            this.type = type;
            this.framerate = framerate;
            this.resolution = resolution;
        }

        public string location { get; set; }
        public string status { get; set; } = "Not processed";
        public string comment { get; set; }
        public bool rotationRequiresAdjustment { get; set; }
        public bool exposureRequiresAdjustment { get; set; }

        public Sequence[] sequences { get; set; }

        // Dessa ska läsas in med filen
        public string type { get; set; }
        public decimal framerate { get; set; }
        public string resolution { get; set; }
    }
}