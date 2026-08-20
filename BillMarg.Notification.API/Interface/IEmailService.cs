 namespace BillMarg.Notification.API.Interface
    {
        public interface IEmailService
        {
            Task<bool> SendEmailAsync(
                string recipientEmail,
                string subject,
                string htmlBody);

            Task<bool> SendEmailWithAttachmentAsync(
                string recipientEmail,
                string subject,
                string htmlBody,
                string attachmentPath);
        }
    }
