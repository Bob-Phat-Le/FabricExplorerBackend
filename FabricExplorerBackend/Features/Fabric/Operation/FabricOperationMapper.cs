using Azure;
using FabricExplorerBackend.Commons.Models.Requests.Fabric.FabricOperation;
using FabricExplorerBackend.Commons.Models.Responses.Fabric.FabricOperation;
using FabricExplorerBackend.Features.Fabric.Operation;
using Microsoft.Fabric.Api.Utils;
using Riok.Mapperly.Abstractions;

namespace FabricExplorerBackend.Mappers.FabricOperation
{
    [Mapper]
    public partial class FabricOperationMapper : IFabricOperationMapper
    {
        [MapPropertyFromSource(nameof(CreateFabricOperationRequest.OperationId), Use = nameof(GetOperationId))]
        [MapPropertyFromSource(nameof(CreateFabricOperationRequest.OperationUrl), Use = nameof(GetOperationUrl))]
        [MapPropertyFromSource(nameof(CreateFabricOperationRequest.RetryAfter), Use = nameof(GetRetryAfter))]
        // 3 trường này không có trong response của Fabric, nơi gọi sẽ điền sau
        [MapperIgnoreTarget(nameof(CreateFabricOperationRequest.ConnectionId))]
        [MapperIgnoreTarget(nameof(CreateFabricOperationRequest.OperationType))]
        [MapperIgnoreTarget(nameof(CreateFabricOperationRequest.ResourceId))]
        public partial CreateFabricOperationRequest Map(Response response);

        // Tên khác nhau giữa request và entity nên phải map tường minh
        [MapProperty(nameof(CreateFabricOperationRequest.OperationId), nameof(Domain.Entities.FabricOperation.FabricOperationId))]
        [MapProperty(nameof(CreateFabricOperationRequest.OperationUrl), nameof(Domain.Entities.FabricOperation.FabricOperationUrl))]
        [MapperIgnoreTarget(nameof(Domain.Entities.FabricOperation.Id))]
        [MapperIgnoreTarget(nameof(Domain.Entities.FabricOperation.Status))]
        [MapperIgnoreTarget(nameof(Domain.Entities.FabricOperation.ErrorCode))]
        [MapperIgnoreTarget(nameof(Domain.Entities.FabricOperation.ErrorMessage))]
        [MapperIgnoreTarget(nameof(Domain.Entities.FabricOperation.LastPolledAt))]
        [MapperIgnoreTarget(nameof(Domain.Entities.FabricOperation.NextPollAt))]
        [MapperIgnoreTarget(nameof(Domain.Entities.FabricOperation.PercentComplete))]
        [MapperIgnoreTarget(nameof(Domain.Entities.FabricOperation.CreatedAt))]
        [MapperIgnoreTarget(nameof(Domain.Entities.FabricOperation.UpdatedAt))]
        public partial Domain.Entities.FabricOperation Map(CreateFabricOperationRequest request);

        public partial FabricOperationResponse Map(Domain.Entities.FabricOperation operation);

        private Guid GetOperationId(Response response)
        {
            return Guid.Parse(response.GetXmsOperationIdHeader());
        }
        private Uri GetOperationUrl(Response response)
        {
            return new Uri(response.GetLocationHeader());
        }
        private TimeSpan GetRetryAfter(Response response)
        {
            // Thiếu header Retry-After thì trả về 0, FabricLroHelper sẽ dùng khoảng poll mặc định
            return response.GetRetryAfterHeader() is { } seconds
                ? TimeSpan.FromSeconds(seconds)
                : TimeSpan.Zero;
        }
    }
}
