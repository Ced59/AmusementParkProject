namespace AmusementPark.WebAPI.Services;

public sealed class ParkDataEditorOperationLease : IDisposable
{
    private readonly Action<string> release;
    private int isDisposed;

    internal ParkDataEditorOperationLease(string operationId, Action<string> release)
    {
        this.OperationId = operationId;
        this.release = release;
    }

    public string OperationId { get; }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref this.isDisposed, 1) == 0)
        {
            this.release(this.OperationId);
        }
    }
}
