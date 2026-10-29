using FabricExplorerBackend.Application.QueryService.Contracts;
using FabricExplorerBackend.Infrastructures.Persistences.Repositories.Interfaces;
using FabricExplorerBackend.Infrastructures.Sql;
using System.Text.Json;

namespace FabricExplorerBackend.Application.QueryService
{
    public class QueryService(ISqlQueryService service, IUnitOfWork unitOfWork) : IQueryService
    {
        public async Task<IReadOnlyList<JsonElement>> QueryExecuteAsync(QueryRequest request)
        {
            var connection = await unitOfWork.ConnectionRepository.GetByIdAsync(request.ConnectionId);

            var response = await service.QueryAsync<JsonElement>(connection!, request.Endpoint, request.Query, map: reader =>
            {
                var row = new Dictionary<string, object?>();

                for (var i = 0; i < reader.FieldCount; i++)
                {
                    row[reader.GetName(i)] =
                        reader.IsDBNull(i) ? null : reader.GetValue(i);
                }

                return JsonSerializer.SerializeToElement(row);
            });

            return response;
        }
    }
}
