namespace VideoKategoriseringsApi.Models
{
    public class Sequence
    {
        public string id {get; set; }
        public decimal inPoint { get; set; }
        public decimal outPoint { get; set; }
        public string[] tags { get; set; }
        public string[] issues { get; set; }
        public string thumbNailImageUrl {get; set; }
    }
}