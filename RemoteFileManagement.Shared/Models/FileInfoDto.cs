using System;

namespace RemoteFileManagement.Shared.Models
{
    [Serializable]
    public class FileInfoDto
    {
        public string Name { get; set; }
        public string Extension { get; set; }
        public long Size { get; set; }
        public DateTime CreationTime { get; set; }
        public DateTime LastModifiedTime { get; set; }
        public string RelativePath { get; set; }
    }
}
