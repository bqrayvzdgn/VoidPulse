namespace VoidPulse.Application.Common;

public record ApiResponse<T>
{
    public bool Success { get; init; }
    public T? Data { get; init; }
    public ApiError? Error { get; init; }
    public PaginationMeta? Meta { get; init; }

    public static ApiResponse<T> Succeed(T data) => new()
    {
        Success = true,
        Data = data
    };

    public static ApiResponse<T> Succeed(T data, PaginationMeta meta) => new()
    {
        Success = true,
        Data = data,
        Meta = meta
    };

    public static ApiResponse<T> Fail(string code, string message, List<FieldError>? details = null) => new()
    {
        Success = false,
        Error = new ApiError
        {
            Code = code,
            Message = message,
            Details = details
        }
    };
}

public record ApiError
{
    public string Code { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public List<FieldError>? Details { get; init; }
}

public record FieldError
{
    public string Field { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
}

public record PaginationMeta
{
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalCount { get; init; }
    public int TotalPages { get; init; }
}
