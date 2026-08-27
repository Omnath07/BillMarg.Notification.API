namespace BillMarg.Notification.API.Services
{
    public interface ISubscriptionRenewalEmailService
    {
        Task<SubscriptionRenewalEmailResult>
            SendSubscriptionRenewalEmailsAsync();
    }

    public class SubscriptionRenewalEmailResult
    {
        public int UsersChecked { get; set; }

        public int EmailsSent { get; set; }

        public int SkippedUsers { get; set; }

        public int FailedEmails { get; set; }

        public int ExpiringInFiveDays { get; set; }

        public int ExpiringInFourDays { get; set; }

        public int ExpiringInThreeDays { get; set; }

        public int ExpiringInTwoDays { get; set; }

        public int ExpiringTomorrow { get; set; }

        public int ExpiringToday { get; set; }

        public int ExpiredOneToFifteenDays { get; set; }
    }
}