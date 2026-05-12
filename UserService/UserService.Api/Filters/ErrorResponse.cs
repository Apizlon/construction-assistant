namespace UserService.Api.Filters;

/// <summary>
/// Тип возвращаемой ошибки.
/// </summary>
public class ErrorResponse
{
    /// <summary>
    /// Текст ошибки.
    /// </summary>
    public string Message { get; set; } = string.Empty;
    
    /// <summary>
    /// Идентификатор запроса.
    /// </summary>
    public string RequestId { get; set; } = string.Empty;
}