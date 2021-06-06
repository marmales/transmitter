using System;
using System.Linq;
using System.Threading.Tasks;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using transmitter.Interfaces;
using transmitter.Models;

namespace transmitter.Tools
{
    public class Sender : ISender
    {
        private readonly ILogger<Sender> _logger;
        private readonly Credentials _credentials;
        private readonly Smtp _smtp;

        public Sender(ILogger<Sender> logger, IOptions<Smtp> options, IOptions<Credentials> credentials)
        {
            _logger = logger;
            _credentials = credentials.Value;
            _smtp = options.Value;
        }

        public bool Prepare(MimeMessage message)
        {
            try
            {
                var from = $"[FROM: {message.From}] ";
                message.Subject = string.Concat(from, message.Subject);
                message.From.Clear();
                message.From.Add(new MailboxAddress(_smtp.FriendlyName, _credentials.SmtpUsername));
                message.To.Clear();
                message.To.AddRange(_smtp.Recipients.Select(x => new MailboxAddress(string.Empty, x)));
                message.Cc.Clear();
                message.Bcc.Clear();
                return true;
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Incoming message has invalid format.");
                return false;
            }
        }
        public async Task Send(MimeMessage message)
        {
            try
            {
                using var client = new SmtpClient
                {
                    ServerCertificateValidationCallback = (_, _, _, _) => true, 
                    LocalDomain = _credentials.SmtpUsername
                };
                _logger.LogInformation("Sending new message.");
                await client.ConnectAsync(_smtp.Hostname, _smtp.Port, SecureSocketOptions.StartTls);
                await client.AuthenticateAsync(_credentials.SmtpUsername, _credentials.Password);
                await client.SendAsync(message);
                _logger.LogInformation("Email send complete.");
            }
            catch (Exception e)
            {
                _logger.LogInformation(e, "Email send failed.");
                throw;
            }
        }
    }
}