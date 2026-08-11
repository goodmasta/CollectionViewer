using System;
using System.Threading.Tasks;

namespace CollectionViewer.Utility;

/// <summary>
/// Bridges an async fetch to ImGui's synchronous, once-per-frame Draw() calls: <see cref="Start"/>
/// kicks off the task, <see cref="Poll"/> (called at the top of Draw) cheaply checks whether it
/// finished and copies the outcome into <see cref="Result"/> or <see cref="Error"/> exactly once.
/// Never blocks the calling (render) thread.
/// </summary>
public sealed class AsyncOperation<T>
{
    private Task<T>? task;
    private bool hasResult;

    public T? Result { get; private set; }

    public Exception? Error { get; private set; }

    public bool IsLoading => task != null;

    /// <summary>True once a fetch has completed successfully, even if <see cref="Result"/> itself
    /// is null/default - important for <c>AsyncOperation&lt;T?&gt;</c> where a null result is a
    /// meaningful, successful outcome (e.g. "search found nothing") rather than "not fetched yet".</summary>
    public bool HasResult => hasResult;

    public void Start(Func<Task<T>> factory)
    {
        Error = null;
        hasResult = false;
        task = factory();
    }

    public void Reset()
    {
        task = null;
        Result = default;
        Error = null;
        hasResult = false;
    }

    /// <summary>Call once per frame before reading <see cref="Result"/>/<see cref="Error"/>.</summary>
    public void Poll()
    {
        if (task is not { IsCompleted: true } completed)
            return;

        if (completed.IsFaulted)
        {
            Error = completed.Exception?.GetBaseException() ?? completed.Exception;
        }
        else if (completed.IsCompletedSuccessfully)
        {
            Result = completed.Result;
            hasResult = true;
        }

        task = null;
    }
}
