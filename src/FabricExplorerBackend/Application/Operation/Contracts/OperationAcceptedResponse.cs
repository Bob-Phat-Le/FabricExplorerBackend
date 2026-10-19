namespace FabricExplorerBackend.Application.Operation.Contracts
{
    public sealed record OperationAcceptedResponse(Guid? OperationId, string Message);
}
