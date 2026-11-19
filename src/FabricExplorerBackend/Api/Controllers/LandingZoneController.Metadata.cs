using FabricExplorerBackend.Api.Contracts;
using FabricExplorerBackend.Api.Extensions;
using FabricExplorerBackend.Application.LandingZone;
using FabricExplorerBackend.Application.LandingZone.Contracts.MetadataGenerate;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FabricExplorerBackend.Api.Controllers
{
    public partial class LandingZoneController : ControllerBase
    {
        [HttpPost("analyze-xsd")]
        public async Task<ActionResult<ApiResponse<AnalyzeResponse>>> AnalyzeAsync([FromBody] AnalyzeRequest request, CancellationToken cancellationToken)
            => (await landingZoneService.AnalyzeXsdAsync(request, cancellationToken)).ToActionResult(this);

        [HttpPost("preview")]
        public async Task<ActionResult<ApiResponse<PreviewResponse>>> PreviewAsync([FromBody] PreviewRequest request, CancellationToken cancellationToken)
            => (await landingZoneService.PreviewAsync(request, cancellationToken)).ToActionResult(this);
    }
}
