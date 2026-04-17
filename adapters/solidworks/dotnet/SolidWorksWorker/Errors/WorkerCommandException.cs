namespace SolidWorksMcp.SolidWorksWorker.Errors;

internal sealed class WorkerCommandException : Exception
{
    public WorkerCommandException(
        string code,
        string message,
        bool retryable = false,
        bool resyncRequired = false,
        bool captureObservedState = false,
        Dictionary<string, object?>? details = null,
        Exception? innerException = null)
        : base(message, innerException)
    {
        Code = code;
        Retryable = retryable;
        ResyncRequired = resyncRequired;
        CaptureObservedState = captureObservedState;
        Details = details ?? [];
    }

    public string Code { get; }

    public bool Retryable { get; }

    public bool ResyncRequired { get; }

    public bool CaptureObservedState { get; }

    public Dictionary<string, object?> Details { get; }
}
