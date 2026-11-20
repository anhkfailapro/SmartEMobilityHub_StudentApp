namespace SmartEMobilityHub.Models
{
    public class OpResult<T>
    {
        public bool Ok => Error == "";
        public string Error { get; init; } = "";
        public string Message { get; init; } = "";
        public T? Value { get; init; }

        public static OpResult<T> Success(T value) => new() { Value = value };

        public static OpResult<T> Fail(string error, string message = "", T? value = default) => new() { Error = error, Message = message, Value = value };
    }
}
