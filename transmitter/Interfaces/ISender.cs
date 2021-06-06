using System.Threading.Tasks;
using MimeKit;

namespace transmitter.Interfaces
{
    public interface ISender
    {
        Task Send(MimeMessage message);
        bool Prepare(MimeMessage message);
    }
}