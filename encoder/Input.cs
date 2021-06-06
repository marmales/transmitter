using CommandLine;

namespace Encoder
{
    public class Input
    {
        [Option("thumbprint")]
        public string Thumbprint { get; set; }
    }
}