using FabricExplorerBackend.Commons;
using FabricExplorerBackend.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Fabric.Api.Lakehouse.Models;

namespace FabricExplorerBackend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LakehousesController(ILakehouseService lakehouseService) : ControllerBase
    {
        //[HttpGet]
        //public async Task<ActionResult<ApiResponse<PagedResponse<Lakehouse>>>> GetAllLakehouse()
        //{
        //    var response = await lakehouseService.GetAllLakehouse();
        //    if (response.IsSuccess)
        //        return Ok(response);
        //    return StatusCode(response.StatusCode, response);
        //}
    }
}
