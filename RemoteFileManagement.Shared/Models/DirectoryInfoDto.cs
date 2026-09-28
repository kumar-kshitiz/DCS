using System;

namespace RemoteFileManagement.Shared.Models
{
    [Serializable]
    public class DirectoryInfoDto
    {
        public string Name { get; set; }
        public string RelativePath { get; set; }
        public DateTime CreationTime { get; set; }
        public DateTime LastModifiedTime { get; set; }
    }
}
