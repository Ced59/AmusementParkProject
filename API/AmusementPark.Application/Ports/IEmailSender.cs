namespace AmusementPark.Application.Ports
{
    /// <summary>
    /// Port applicatif d'envoi d'emails.
    /// </summary>
    public interface IEmailSender
    {
        /// <summary>
        /// Envoie un email applicatif.
        /// </summary>
        Task SendAsync(string to, string subject, string htmlBody, CancellationToken cancellationToken);
    }
}
