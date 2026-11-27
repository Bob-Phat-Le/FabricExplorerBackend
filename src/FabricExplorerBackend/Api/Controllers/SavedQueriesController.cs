using FabricExplorerBackend.Api.Contracts;
using FabricExplorerBackend.Api.Extensions;
using FabricExplorerBackend.Application.SavedQueries;
using FabricExplorerBackend.Application.SavedQueries.Contracts;
using FabricExplorerBackend.Commons.Contracts;
using FabricExplorerBackend.Commons.Paginations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;

namespace FabricExplorerBackend.Api.Controllers
{
    /// <summary>Saved queries of the signed-in user.</summary>
    [EnableCors("MyAllowSpecificOrigin")]
    [Authorize]
    [Route("api/saved-queries")]
    [ApiController]
    public class SavedQueriesController(ISavedQueryService savedQueryService) : ControllerBase
    {
        [HttpGet]
        public async Task<ActionResult<ApiResponse<PagedResponse<IEnumerable<SavedQueryResponse>>>>> GetAll(
            [FromQuery] ListSavedQueriesRequest request, CancellationToken cancellationToken)
            => (await savedQueryService.GetAllAsync(request, cancellationToken)).ToActionResult(this);

        [HttpGet("{id:guid}")]
        public async Task<ActionResult<ApiResponse<SavedQueryResponse>>> GetById(Guid id, CancellationToken cancellationToken)
            => (await savedQueryService.GetByIdAsync(id, cancellationToken)).ToActionResult(this);

        [HttpPost]
        public async Task<ActionResult<ApiResponse<SavedQueryResponse>>> Create(
            [FromBody] CreateSavedQueryRequest request, CancellationToken cancellationToken)
            => (await savedQueryService.CreateAsync(request, cancellationToken)).ToActionResult(this);

        [HttpPatch("{id:guid}")]
        public async Task<ActionResult<ApiResponse<SavedQueryResponse>>> Update(
            Guid id, [FromBody] UpdateSavedQueryRequest request, CancellationToken cancellationToken)
            => (await savedQueryService.UpdateAsync(id, request, cancellationToken)).ToActionResult(this);

        [HttpDelete("{id:guid}")]
        public async Task<ActionResult<ApiResponse<ConfirmationResponse>>> Delete(Guid id, CancellationToken cancellationToken)
            => (await savedQueryService.DeleteAsync(id, cancellationToken)).ToActionResult(this);
    }
}
