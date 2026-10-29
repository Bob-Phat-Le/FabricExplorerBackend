namespace FabricExplorerBackend.Features.Operation.Contracts
{
    public sealed record OperationAcceptedResponse(Guid? OperationId, string Message);
}
