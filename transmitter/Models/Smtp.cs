namespace transmitter.Models
{
    public class Smtp
    {
        public string Hostname { get; set; }
        public int Port { get; set; }
        public string FriendlyName { get; set; }
        public string[] Recipients { get; set; }
    }
}