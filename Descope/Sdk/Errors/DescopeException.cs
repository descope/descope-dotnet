using System.Text.Json.Serialization;

namespace Descope;

[Serializable]
public class DescopeException : ApplicationException
{
    public string? ErrorCode { get; set; }
    public string? ErrorDescription { get; set; }
    public string? ErrorMessage { get; set; }

    public DescopeException(string msg) : base(msg) { }

    public DescopeException(string? msg, Exception? ex) : base(msg, ex) { }

    public DescopeException(ErrorDetails errorDetails) : base(message: errorDetails.ExceptionMessage)
    {
        ErrorCode = errorDetails.ErrorCode;
        ErrorDescription = errorDetails.ErrorDescription;
        ErrorMessage = errorDetails.ErrorMessage;
    }

}

public class ErrorDetails
{
    [JsonPropertyName("errorCode")]
    public string ErrorCode { get; set; }

    [JsonPropertyName("errorDescription")]
    public string ErrorDescription { get; set; }
    [JsonPropertyName("errorMessage")]
    public string? ErrorMessage { get; set; }
    public ErrorDetails(string errorCode, string errorDescription, string? errorMessage)
    {
        ErrorCode = errorCode;
        ErrorDescription = errorDescription;
        ErrorMessage = errorMessage;
    }

    public string ExceptionMessage { get => $"[{ErrorCode}]: {ErrorDescription}{(ErrorMessage != null ? $" ({ErrorMessage})" : "")}"; }
}
