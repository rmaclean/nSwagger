namespace API
{
    public class WrappedResponse<T>
    {
        public WrappedResponse(bool success, T data)
        {
            Data = data;
            Succeeded = success;
        }

        public WrappedResponse(bool success)
        {
            Succeeded = success;
        }

        public WrappedResponse(string errorMessage, string errorDetail)
        {
            ErrorMessage = errorMessage;
            ErrorDetail = errorDetail;
            Succeeded = false;
        }

        public T Data { get; }

        public bool Succeeded { get; }

        public string ErrorMessage { get; set; }

        public string ErrorDetail { get; set; }
    }
}