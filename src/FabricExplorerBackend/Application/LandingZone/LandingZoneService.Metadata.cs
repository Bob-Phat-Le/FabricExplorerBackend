using Azure;
using FabricExplorerBackend.Application.LandingZone.Contracts.MetadataGenerate;
using FabricExplorerBackend.Application.LandingZone.MetadataGenerate;
using FabricExplorerBackend.Commons.Errors;
using FabricExplorerBackend.Commons.Results;
using FabricExplorerBackend.Infrastructures.LandingZone;
using System.Text;

namespace FabricExplorerBackend.Application.LandingZone
{
    public partial class LandingZoneService
    {
        public Result<MetadataCapabilities> GetMetadataCapabilities() => Result<MetadataCapabilities>.Success(metadataFormats.Describe());

        public async Task<Result<AnalyzeResponse>> AnalyzeXsdAsync(AnalyzeRequest request, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (request.File is not { Length: > 0 } file)
                return Result<AnalyzeResponse>.Failure(Error.BadRequest(ErrorCodes.MetadataGenerate.InvalidXsd, "An XSD file is required."));

            using var stream = file.OpenReadStream();
            return xsdAnalyzer.Analyze(stream);
        }

        public async Task<Result<PreviewResponse>> PreviewAsync(PreviewRequest request, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Result<PreviewResponse>.Success(metadataGenerator.Generate(request));
        }

        public async Task<Result<PublishResponse>> PublishAsync(Guid connectionId, PublishRequest request, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var metadata = metadataGenerator.Generate(request.Metadata);
            if (!metadata.IsValid)
                return Error.BadRequest(ErrorCodes.MetadataGenerate.InvalidMetadata, string.Join(" ", metadata.Errors));

            var target = MetadataTarget.Resolve(request.ParentPath, request.SchemaName, request.TableName);

            var opened = await landingZoneHelper.OpenAsync(connectionId, cancellationToken);
            if (!opened.IsSuccess)
                return Result<PublishResponse>.Failure(opened.Errors.ToArray());

            var session = opened.Data!;

            var missing = await landingZoneHelper.EnsureFolderExistsAsync(session, target.ParentFolder, cancellationToken);
            if (missing is not null) return missing;

            var exists = await session.FileSystem.ExistsAsync(target.FilePath, cancellationToken);
            if (exists && !request.Overwrite)
            {
                await FlushLogAsync(connectionId, [NewLog(connectionId, "Generate Metadata", metadata.FileName, target.TableFolder, false, $"{metadata.FileName} already exists")]);
                return Error.Conflict(
                    ErrorCodes.MetadataGenerate.MetadataAlreadyExists,
                    $"{metadata.FileName} already exists in '{LandingZonePaths.Display(target.TableFolder)}'.");
            }

            try
            {
                await session.FileSystem.CreateDirectoryAsync(target.SchemaFolder, cancellationToken);
                await session.FileSystem.CreateDirectoryAsync(target.TableFolder, cancellationToken);

                await using var content = new MemoryStream(Encoding.UTF8.GetBytes(metadata.Json));
                await session.FileSystem.UploadAsync(target.FilePath, content, request.Overwrite, cancellationToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                var reason = ex is RequestFailedException azure ? $"HTTP {azure.Status} {azure.ErrorCode}".TrimEnd() : ex.Message;
                await FlushLogAsync(connectionId, [NewLog(connectionId, "Generate Metadata", metadata.FileName, target.TableFolder, false, reason)]);
                throw;
            }

            var action = exists ? "replaced" : "created";
            await FlushLogAsync(connectionId, [NewLog(connectionId, "Generate Metadata", metadata.FileName, target.TableFolder, true, $"{request.Metadata.FileFormat} metadata {action}")]);

            return Result<PublishResponse>.Success(new PublishResponse(target.FilePath, exists));
        }
    }
}
