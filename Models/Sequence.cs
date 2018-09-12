namespace VideoKategoriseringsApi.Models
{
    public class Sequence
    {
        public decimal inPoint { get; set; }
        public decimal outPoint { get; set; }
        public string[] tags { get; set; }
    }
}