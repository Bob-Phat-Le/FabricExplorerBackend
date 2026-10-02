using Azure;
using FabricExplorerBackend.Models.Requests.FabricOperation;
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
        public partial CreateFabricOperationRequest Map(Response response);

        [MapperIgnoreTarget(nameof(Entities.FabricOperation.LastPolledAt))]
        [MapperIgnoreTarget(nameof(Entities.FabricOperation.NextPollAt))]
        [MapperIgnoreTarget(nameof(Entities.FabricOperation.PercentComplete))]
        [MapperIgnoreTarget(nameof(Entities.FabricOperation.CreatedAt))]
        [MapperIgnoreTarget(nameof(Entities.FabricOperation.UpdatedAt))]
        public partial Entities.FabricOperation Map(CreateFabricOperationRequest request);

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
            return TimeSpan.FromSeconds((double)response.GetRetryAfterHeader()!);
        }
    }
}
