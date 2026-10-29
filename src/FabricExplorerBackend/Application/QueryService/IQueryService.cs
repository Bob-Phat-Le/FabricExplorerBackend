using FabricExplorerBackend.Application.QueryService.Contracts;
using System.Text.Json;

namespace FabricExplorerBackend.Application.QueryService
{
    public interface IQueryService
    {
        Task<IReadOnlyList<JsonElement>> QueryExecuteAsync(QueryRequest request);
    }
}
