using Azure;
using FabricExplorerBackend.Commons.Models.Requests.FabricOperation;
using FabricExplorerBackend.Commons.Models.Responses.Fabric.FabricOperation;
using FabricExplorerBackend.Domain.Enums;
using FabricExplorerBackend.Features.Fabric.Operation;
using Microsoft.Fabric.Api.Utils;

namespace FabricExplorerBackend.Mappers.FabricOperation
{
    // Viết tay thay vì Mapperly: request có thêm các field ngữ cảnh và entity cần Id/Status/NextPollAt
    public class FabricOperationMapper : IFabricOperationMapper
    {
        private const double DefaultRetryAfterSeconds = 5;

        public CreateFabricOperationRequest Map(Response response)
        {
            var retryAfter = response.GetRetryAfterHeader();

            return new CreateFabricOperationRequest
            {
                OperationId = Guid.Parse(response.GetXmsOperationIdHeader()),
                OperationUrl = new Uri(response.GetLocationHeader()),
                RetryAfter = TimeSpan.FromSeconds(retryAfter is null ? DefaultRetryAfterSeconds : Convert.ToDouble(retryAfter))
            };
        }

        public Domain.Entities.FabricOperation Map(CreateFabricOperationRequest request)
        {
            var now = DateTimeOffset.UtcNow;
            return new Domain.Entities.FabricOperation
            {
                Id = Guid.NewGuid(),
                FabricOperationId = request.OperationId,
                FabricOperationUrl = request.OperationUrl?.ToString(),
                OperationType = request.OperationType,
                ConnectionId = request.ConnectionId,
                WorkspaceId = request.WorkspaceId,
                ResourceId = request.ResourceId,
                Status = FabricOperationStatus.Pending,
                NextPollAt = now.Add(request.RetryAfter),
                CreatedAt = now
            };
        }

        public FabricOperationResponse Map(Domain.Entities.FabricOperation operation)
        {
            return new FabricOperationResponse
            {
                Id = operation.Id,
                OperationType = operation.OperationType,
                ResourceId = operation.ResourceId,
                Status = operation.Status,
                PercentComplete = operation.PercentComplete,
                ErrorCode = operation.ErrorCode,
                ErrorMessage = operation.ErrorMessage,
                CreatedAt = operation.CreatedAt,
                UpdatedAt = operation.UpdatedAt
            };
        }
    }
}
