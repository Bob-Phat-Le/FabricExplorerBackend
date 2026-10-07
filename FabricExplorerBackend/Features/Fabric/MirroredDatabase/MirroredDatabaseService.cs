using FabricExplorerBackend.Commons;
using FabricBackendModel = FabricExplorerBackend.Commons.Models.Responses.Fabric.MirroredDatabase;
using FabricExplorerBackend.Domain.Enums;
using FabricExplorerBackend.Features.Fabric.Context;
using FabricExplorerBackend.Infrastructures.Persistences.Repositories.Interfaces;
using Microsoft.Fabric.Api.MirroredDatabase.Models;

namespace FabricExplorerBackend.Features.Fabric.MirroredDatabase
{
    public class MirroredDatabaseService(
        IMirroredDatabaseMapper mapper,
        IFabricContextFactory fabricContextFactory,
        IUnitOfWork unitOfWork) : IMirroredDatabaseService
    {
        public async Task<Result<FabricBackendModel.MirroredDatabaseDetailResponse>> GetMirroredDatabaseByIdAsync(Guid connectionId, Guid workspaceId, Guid mirroredDatabaseId)
        {
            //try
            //{
            //    var connection = await unitOfWork.ConnectionRepository.GetByIdAsync(connectionId);
            //    if (connection == null)
            //        return Result<MirroredDatabaseDetailResponse>.Failure(ResultStatus.NotFound, "connection not found");

            //    var context = await fabricContextFactory.CreateFabricContextAsync(connection);
            //    if (context == null)
            //        return Result<MirroredDatabaseDetailResponse>.Failure(ResultStatus.BadRequest, "can not create context");

            //    var fabricResponse = await context.Client.MirroredDatabase.Items.GetMirroredDatabaseAsync(workspaceId, mirroredDatabaseId);
            //    var mirroredDatabase = fabricResponse.Value;

            //    var response = mapper.Map(mirroredDatabase);

            //    return Result<MirroredDatabaseDetailResponse>.Success(response);
            //}
            //catch (Exception ex)
            //{
            //    return Result<MirroredDatabaseDetailResponse>.Failure(ResultStatus.InternalError, ex.Message);
            //}
            throw new NotImplementedException();
        }

        public async Task<Result<FabricBackendModel.MirroringDatabaseStatusResponse>> GetMirroringStatus(Guid connectionId, Guid workspaceId, Guid mirroredDatabaseId)
        {
            try
            {
                var connection = await unitOfWork.ConnectionRepository.GetByIdAsync(connectionId);
                if (connection == null)
                    return Result<FabricBackendModel.MirroringDatabaseStatusResponse>.Failure(ResultStatus.NotFound, "connection not found");

                var context = await fabricContextFactory.CreateFabricContextAsync(connection);
                if (context == null)
                    return Result<FabricBackendModel.MirroringDatabaseStatusResponse>.Failure(ResultStatus.BadRequest, "can not create context");

                var fabricMirroringStatusTask = context.Client.MirroredDatabase.Mirroring.GetMirroringStatusAsync(workspaceId, mirroredDatabaseId);
                var fabricMirroringTablesStatusTask = context.Client.MirroredDatabase.Mirroring
                    .GetTablesMirroringStatusAsync(workspaceId, mirroredDatabaseId)
                    .ToListAsync()
                    .AsTask();

                await Task.WhenAll(fabricMirroringTablesStatusTask, fabricMirroringStatusTask);

                var mirroingStatusTaskResponse = await fabricMirroringStatusTask;
                var mirroringTablesStatus = await fabricMirroringTablesStatusTask;

                var mirroringStatus = mirroingStatusTaskResponse.Value;
                var lastSynchronization = mirroringTablesStatus.Max(item => item.Metrics.LastSyncDateTime);
                var recordSynchronized = mirroringTablesStatus.Sum(item => item.Metrics.ProcessedRows);
                var currentLatency = mirroringTablesStatus.Max(item => item.Metrics.LastSyncLatencyInSeconds);

                var response = mapper.Map(mirroringStatus);
                response.CurrentLatency = currentLatency;
                response.LastSynchronization = lastSynchronization;
                response.RecordSynchronized = recordSynchronized;

                return Result<FabricBackendModel.MirroringDatabaseStatusResponse>.Success(response);
            }
            catch (Exception ex)
            {
                return Result<FabricBackendModel.MirroringDatabaseStatusResponse>.Failure(ResultStatus.InternalError, ex.Message);
            }
        }

        public async Task<Result<IEnumerable<FabricBackendModel.TableMirroringStatusResponse>>> ListTablesMirroringStatusAsync(
            Guid connectionId, 
            Guid workspaceId, 
            Guid mirroredDatabaseId)
        {
            try
            {
                var connection = await unitOfWork.ConnectionRepository.GetByIdAsync(connectionId);
                if (connection == null)
                    return Result<IEnumerable<FabricBackendModel.TableMirroringStatusResponse>>.Failure(ResultStatus.NotFound, "connection not found");

                var context = await fabricContextFactory.CreateFabricContextAsync(connection);
                if (context == null)
                    return Result<IEnumerable<FabricBackendModel.TableMirroringStatusResponse>>.Failure(ResultStatus.BadRequest, "can not create context");

                var fabricMirroredDatabaseDefinitions = context.Client.MirroredDatabase.Items
                    .GetMirroredDatabaseDefinitionAsync(workspaceId, mirroredDatabaseId);
                var fabricMirroringTablesStatusTask = context.Client.MirroredDatabase.Mirroring
                    .GetTablesMirroringStatusAsync(workspaceId, mirroredDatabaseId)
                    .ToListAsync()
                    .AsTask();

                await Task.WhenAll(fabricMirroredDatabaseDefinitions, fabricMirroringTablesStatusTask);

                var definitions = await fabricMirroredDatabaseDefinitions;
                var tablesStatus = await fabricMirroringTablesStatusTask;

                var payload = definitions.Value.Definition.Parts
                    .FirstOrDefault(item => item.Path.Equals("mirroring.json", StringComparison.CurrentCultureIgnoreCase))?
                    .Payload;


            }
            catch (Exception ex)
            {
                return Result<IEnumerable<FabricBackendModel.TableMirroringStatusResponse>>.Failure(ResultStatus.InternalError, ex.Message);
            }
            throw new NotImplementedException();
        }
    }
}
