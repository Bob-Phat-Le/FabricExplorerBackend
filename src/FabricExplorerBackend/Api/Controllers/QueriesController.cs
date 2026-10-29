using FabricExplorerBackend.Application.QueryService;
using FabricExplorerBackend.Application.QueryService.Contracts;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FabricExplorerBackend.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class QueriesController(IQueryService service) : ControllerBase
    {
        [HttpPost]
        public async Task<IActionResult> TestQueryAsync([FromBody] QueryRequest request)
            => Ok((await service.QueryExecuteAsync(request)));
    }
}
