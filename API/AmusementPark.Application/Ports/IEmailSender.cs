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

        /// <summary>
        /// Envoie un email applicatif avec ses versions HTML/texte et ses en-têtes explicites.
        /// </summary>
        Task SendAsync(EmailMessage message, CancellationToken cancellationToken);
    }
}
