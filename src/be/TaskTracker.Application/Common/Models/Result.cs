global using System.Text.Json.Serialization;

namespace TaskTracker.Application.Common.Models;
public class Result
{
    internal Result(bool succeeded, string message, IEnumerable<string> errors)
    {
        Succeeded = succeeded;
        Message = message;
        Errors = [.. errors];
    }
    public bool Succeeded { get; init; }
    public string Message { get; init; }
    public string[] Errors { get; init; }
    public static Result Success(string message) => new(true, message, []);
    public static Result Failure(string message, IEnumerable<string> errors) => new(false, message, errors);
}

public class Result<T> : Result
{
    [JsonConstructor]
    public Result() : base(false, string.Empty, [])
    {
    }
    internal Result(bool succeeded, string message, IEnumerable<string> errors, T data)
        : base(succeeded, message, errors)
    {
        Data = data;
    }

    public T? Data { get; init; }

    public static new Result Success(string message) => new(true, message, []);
    public static Result<T> Success(string message, T data) => new(true, message, [], data);
    public static new Result<T> Failure(string message, IEnumerable<string> errors) => new(false, message, errors, data: default!);
}
