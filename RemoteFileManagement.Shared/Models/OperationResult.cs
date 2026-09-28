using System;

namespace RemoteFileManagement.Shared.Models
{
    [Serializable]
    public class OperationResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public string ErrorCode { get; set; }

        public static OperationResult Ok(string message = "Success")
        {
            return new OperationResult
            {
                Success = true,
                Message = message,
                ErrorCode = string.Empty
            };
        }

        public static OperationResult Fail(string message, string errorCode = "ERROR")
        {
            return new OperationResult
            {
                Success = false,
                Message = message,
                ErrorCode = errorCode
            };
        }
    }
}
