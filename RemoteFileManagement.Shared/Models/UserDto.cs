using System;

namespace RemoteFileManagement.Shared.Models
{
    [Serializable]
    public class UserDto
    {
        public string Username { get; set; }
        public string DisplayName { get; set; }
        public string SessionToken { get; set; }
        public bool IsAuthenticated { get; set; }
    }
}
