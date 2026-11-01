namespace GiantGroupOpsPlus.Services;

/// <summary>An error with a message that is safe to show to the user.</summary>
public class ApiException : Exception
{
    public int StatusCode { get; }
    public bool IsOffline { get; }
    public bool IsAuth => StatusCode == 401;

    public ApiException(string message, int statusCode = 0, bool isOffline = false, Exception? inner = null)
        : base(message, inner)
    {
        StatusCode = statusCode;
        IsOffline = isOffline;
    }

    /// <summary>True when retrying later could succeed (network trouble, timeouts, server hiccups).</summary>
    public bool IsTransient => IsOffline || StatusCode == 0 || StatusCode == 408 || StatusCode == 429 || StatusCode >= 500;
}
