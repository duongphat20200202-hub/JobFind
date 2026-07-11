using System;

namespace BasicProject.Models
{
    public class AdminNotificationItem
    {
        public string IconClass { get; set; }
        public string Title { get; set; }
        public string Message { get; set; }
        public string Url { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}