namespace BillMarg.Notification.API.Model
{
    public class EmailSettings
    {
        public string SmtpServer { get; set; }
        public int Port { get; set; }
        public string FromEmail { get; set; }
        public string Password { get; set; }
        public string FromName { get; set; }
        public string LogoUrl { get; set; }
        public string WebsiteUrl { get; set; }


    }
}
