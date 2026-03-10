namespace Library.Application.Common;

public sealed record ServiceResult<T>(bool Succeeded, T? Value, string? Error)
{
    public static ServiceResult<T> Success(T value) => new(true, value, null);

    public static ServiceResult<T> Failure(string error) => new(false, default, error);
}
