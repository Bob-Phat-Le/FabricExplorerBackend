using FabricExplorerBackend.Api.Contracts;
using FabricExplorerBackend.Api.Extensions;
using FabricExplorerBackend.Application.QueryService;
using FabricExplorerBackend.Application.QueryService.Contracts;
using FabricExplorerBackend.Commons.Contracts;
using FabricExplorerBackend.Commons.Extensions;
using FabricExplorerBackend.Commons.Results;
using FabricExplorerBackend.Infrastructures.Options;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace FabricExplorerBackend.Api.Controllers
{
    [EnableCors("MyAllowSpecificOrigin")]
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class QueriesController(
        IQueryService service,
        IOptions<EventStreamingOptions> streamingOptions) : ControllerBase
    {
        /// <summary>
        /// Creates the query and returns immediately (202). The SQL runs in the background; follow the
        /// status with GET {id}/stream (SSE) or GET {id}, then read the rows with GET {id}/result.
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<ApiResponse<QueryAcceptedResponse>>> CreateQueryAsync(
            [FromBody] QueryRequest request,
            CancellationToken cancellationToken)
        {
            var result = await service.CreateQueryAsync(User.GetUserId(), request, cancellationToken);
            if (!result.IsSuccess)
                return result.ToActionResult(this);

            var data = result.Data!;
            Response.Headers.Location = Url.RouteUrl("GetQuery", new { id = data.QueryId });
            return StatusCode(StatusCodes.Status202Accepted,
                new ApiResponse<QueryAcceptedResponse> { StatusCode = 202, Data = data });
        }

        [HttpGet]
        public async Task<ActionResult<ApiResponse<List<QueryResponse>>>> ListQueriesAsync(
            [FromQuery] int skip = 0,
            [FromQuery] int take = 50,
            CancellationToken cancellationToken = default)
            => (await service.GetQueriesAsync(User.GetUserId(), skip, take, cancellationToken)).ToActionResult(this);

        [HttpGet("{id:guid}", Name = "GetQuery")]
        public async Task<ActionResult<ApiResponse<QueryResponse>>> GetQueryAsync(
            Guid id, CancellationToken cancellationToken = default)
            => (await service.GetQueryAsync(User.GetUserId(), id, cancellationToken)).ToActionResult(this);

        /// <summary>
        /// Lifecycle events only (status / completed / error), never rows. The first event is a snapshot of
        /// the current state, so a client that connects late - or reconnects - never misses the outcome.
        /// </summary>
        [HttpGet("{id:guid}/stream")]
        [Produces("text/event-stream")]
        public async Task<IActionResult> StreamAsync(Guid id, CancellationToken cancellationToken)
        {
            var opened = await service.OpenStreamAsync(User.GetUserId(), id, cancellationToken);
            if (!opened.IsSuccess)
                return Result<object>.Failure(opened.Errors[0]).ToActionResult(this).Result!;

            var stream = opened.Data!;
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, stream.Lifetime);
            linked.CancelAfter(streamingOptions.Value.MaxLifetime * 0.8);

            try
            {
                await Response.StreamEventsAsync(stream, 0, streamingOptions.Value.HeartbeatInterval, linked.Token);
            }
            finally
            {
                service.CloseStream(stream);
            }

            return new EmptyResult();
        }

        /// <summary>One page of rows. "Unlimited" queries are read the same way, page by page.</summary>
        [HttpGet("{id:guid}/result")]
        public async Task<ActionResult<ApiResponse<QueryResultPageResponse>>> GetResultPageAsync(
            Guid id,
            [FromQuery] long offset = 0,
            [FromQuery] int limit = 0,
            CancellationToken cancellationToken = default)
            => (await service.GetResultPageAsync(User.GetUserId(), id, offset, limit, cancellationToken)).ToActionResult(this);

        [HttpPost("{id:guid}/cancel")]
        public async Task<ActionResult<ApiResponse<QueryResponse>>> CancelQueryAsync(
            Guid id, CancellationToken cancellationToken = default)
            => (await service.CancelQueryAsync(User.GetUserId(), id, cancellationToken)).ToActionResult(this);

        [HttpDelete("{id:guid}")]
        public async Task<ActionResult<ApiResponse<ConfirmationResponse>>> DeleteQueryAsync(
            Guid id, CancellationToken cancellationToken = default)
            => (await service.DeleteQueryAsync(User.GetUserId(), id, cancellationToken)).ToActionResult(this);
    }
}
