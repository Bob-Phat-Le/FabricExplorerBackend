using FabricExplorerBackend.Api.Contracts;
using FabricExplorerBackend.Api.Extensions;
using FabricExplorerBackend.Application.LandingZone.Contracts.MetadataGenerate;
using Microsoft.AspNetCore.Mvc;

namespace FabricExplorerBackend.Api.Controllers
{
    public partial class LandingZoneController
    {
        private const long MaxXsdUploadBytes = 6L * 1024 * 1024;

        [HttpGet("metadata/capabilities")]
        public ActionResult<ApiResponse<MetadataCapabilities>> GetMetadataCapabilities()
            => landingZoneService.GetMetadataCapabilities().ToActionResult(this);

        [HttpPost("analyze-xsd")]
        [Consumes("multipart/form-data")]
        [RequestSizeLimit(MaxXsdUploadBytes)]
        public async Task<ActionResult<ApiResponse<AnalyzeResponse>>> AnalyzeAsync(
            [FromForm] AnalyzeRequest request,
            CancellationToken cancellationToken)
            => (await landingZoneService.AnalyzeXsdAsync(request, cancellationToken)).ToActionResult(this);

        [HttpPost("preview")]
        public async Task<ActionResult<ApiResponse<PreviewResponse>>> PreviewAsync(
            [FromBody] PreviewRequest request,
            CancellationToken cancellationToken)
            => (await landingZoneService.PreviewAsync(request, cancellationToken)).ToActionResult(this);

        [HttpPost("publish")]
        public async Task<ActionResult<ApiResponse<PublishResponse>>> PublishAsync(
            [FromHeader(Name = "X-Connection-Id")] Guid connectionId,
            [FromBody] PublishRequest request,
            CancellationToken cancellationToken)
            => (await landingZoneService.PublishAsync(connectionId, request, cancellationToken)).ToActionResult(this);
    }
}
