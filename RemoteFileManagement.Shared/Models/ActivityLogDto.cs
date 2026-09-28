using System;

namespace RemoteFileManagement.Shared.Models
{
    [Serializable]
    public class ActivityLogDto
    {
        public DateTime Timestamp { get; set; }
        public string Username { get; set; }
        public string Operation { get; set; }
        public string Target { get; set; }
        public string Status { get; set; }
    }
}
