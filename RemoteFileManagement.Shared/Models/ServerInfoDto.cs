using System;

namespace RemoteFileManagement.Shared.Models
{
    [Serializable]
    public class ServerInfoDto
    {
        public string Hostname { get; set; }
        public DateTime ServerStartTime { get; set; }
        public long TotalStorageBytes { get; set; }
        public long UsedStorageBytes { get; set; }
        public long AvailableStorageBytes { get; set; }
        public int RegisteredUserCount { get; set; }
        public int ActiveClientCount { get; set; }
    }
}
