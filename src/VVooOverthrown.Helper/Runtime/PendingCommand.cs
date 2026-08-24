using VVooOverthrown.Helper.Transport;

namespace VVooOverthrown.Helper.Runtime;

internal sealed class PendingCommand
{
    public PendingCommand(PipeRequest request, TaskCompletionSource<PipeResponse> completion)
    {
        Request = request;
        Completion = completion;
    }

    public PipeRequest Request { get; }

    public TaskCompletionSource<PipeResponse> Completion { get; }
}
