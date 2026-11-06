namespace StudentManagementSystem.DTOs.Common
{
    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public T? Data { get; set; }
        public IEnumerable<string>? Errors { get; set; }

        public static ApiResponse<T> SuccessResult(T data, string message = "Operation completed successfully.")
        {
            return new ApiResponse<T>
            {
                Success = true,
                Message = message,
                Data = data,
                Errors = null
            };
        }

        public static ApiResponse<T> FailureResult(string message, IEnumerable<string>? errors = null)
        {
            return new ApiResponse<T>
            {
                Success = false,
                Message = message,
                Data = default,
                Errors = errors ?? new List<string> { message }
            };
        }
    }
}

