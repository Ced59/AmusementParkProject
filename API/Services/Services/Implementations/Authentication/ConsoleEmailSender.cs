using Microsoft.Extensions.Logging;
using Services.Interfaces.Authentication;

namespace Services.Implementations.Authentication
{
    public class ConsoleEmailSender : IEmailSender
    {
        private readonly ILogger<ConsoleEmailSender> logger;

        public ConsoleEmailSender(ILogger<ConsoleEmailSender> logger)
        {
            this.logger = logger;
        }

        public Task SendAsync(string to, string subject, string plainTextBody, CancellationToken cancellationToken = default)
        {
            logger.LogInformation(
                "Mock email sent. To: {To}\nSubject: {Subject}\nBody:\n{Body}",
                to,
                subject,
                plainTextBody);

            return Task.CompletedTask;
        }
    }
}
