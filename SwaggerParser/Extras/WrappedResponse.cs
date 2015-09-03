namespace API
{
    public class WrappedResponse<T>
    {
        public WrappedResponse(bool success, T data)
        {
            Data = data;
            Succeeded = success;
        }

        public T Data { get; }

        public bool Succeeded { get; }
    }
}