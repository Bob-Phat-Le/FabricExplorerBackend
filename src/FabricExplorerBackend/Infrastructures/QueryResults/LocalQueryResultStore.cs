using FabricExplorerBackend.Application.QueryService;
using FabricExplorerBackend.Infrastructures.Options;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace FabricExplorerBackend.Infrastructures.QueryResults
{
    /// <summary>
    /// Local filesystem implementation of <see cref="IQueryResultStore"/>:
    /// <c>&lt;root&gt;/&lt;queryId&gt;/part-00001.parquet ... manifest.json</c>.
    /// Parts are written to <c>*.tmp</c> and renamed on commit, the manifest is written last, so a reader
    /// never sees a half-written result.
    /// </summary>
    public sealed class LocalQueryResultStore : IQueryResultStore
    {
        private const string ManifestFileName = "manifest.json";
        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

        private readonly string root;

        public LocalQueryResultStore(IOptions<QueryResultStorageOptions> options, IHostEnvironment environment)
        {
            var configured = string.IsNullOrWhiteSpace(options.Value.RootPath) ? "query-results" : options.Value.RootPath;
            root = Path.GetFullPath(Path.IsPathRooted(configured)
                ? configured
                : Path.Combine(environment.ContentRootPath, configured));
        }

        public Task<IQueryResultPartWriter> CreatePartWriterAsync(Guid queryId, int partNumber, CancellationToken cancellationToken = default)
        {
            var directory = DirectoryOf(queryId);
            Directory.CreateDirectory(directory);

            var partName = $"part-{partNumber:D5}.parquet";
            var finalPath = Path.Combine(directory, partName);
            var tempPath = finalPath + ".tmp";

            var stream = new FileStream(tempPath, FileMode.Create, FileAccess.ReadWrite, FileShare.None, 81920, FileOptions.Asynchronous);
            return Task.FromResult<IQueryResultPartWriter>(new PartWriter(partName, tempPath, finalPath, stream));
        }

        public Task<Stream> OpenPartReaderAsync(Guid queryId, string partName, CancellationToken cancellationToken = default)
        {
            // Part names come from the manifest, but never let them escape the query folder.
            if (string.IsNullOrWhiteSpace(partName) || !string.Equals(Path.GetFileName(partName), partName, StringComparison.Ordinal))
                throw new ArgumentException("Invalid part name.", nameof(partName));

            Stream stream = new FileStream(
                Path.Combine(DirectoryOf(queryId), partName),
                FileMode.Open, FileAccess.Read, FileShare.Read, 81920, FileOptions.Asynchronous | FileOptions.RandomAccess);
            return Task.FromResult(stream);
        }

        public async Task WriteMetadataAsync(Guid queryId, QueryResultMetadata metadata, CancellationToken cancellationToken = default)
        {
            var directory = DirectoryOf(queryId);
            Directory.CreateDirectory(directory);

            var finalPath = Path.Combine(directory, ManifestFileName);
            var tempPath = finalPath + ".tmp";

            await using (var stream = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, FileOptions.Asynchronous))
                await JsonSerializer.SerializeAsync(stream, metadata, JsonOptions, cancellationToken);

            File.Move(tempPath, finalPath, overwrite: true);
        }

        public async Task<QueryResultMetadata?> ReadMetadataAsync(Guid queryId, CancellationToken cancellationToken = default)
        {
            var path = Path.Combine(DirectoryOf(queryId), ManifestFileName);
            if (!File.Exists(path))
                return null;

            await using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.Asynchronous);
            return await JsonSerializer.DeserializeAsync<QueryResultMetadata>(stream, JsonOptions, cancellationToken);
        }

        public Task DeleteAsync(Guid queryId, CancellationToken cancellationToken = default)
        {
            var directory = DirectoryOf(queryId);
            if (Directory.Exists(directory))
                Directory.Delete(directory, recursive: true);

            return Task.CompletedTask;
        }

        // Guid.ToString("D") only contains hex digits and '-', so the path cannot be tampered with.
        private string DirectoryOf(Guid queryId) => Path.Combine(root, queryId.ToString("D"));

        private sealed class PartWriter(string partName, string tempPath, string finalPath, FileStream stream) : IQueryResultPartWriter
        {
            private bool committed;

            public string PartName => partName;

            public Stream Stream => stream;

            public async Task<long> CommitAsync(CancellationToken cancellationToken = default)
            {
                // The Parquet writer may already have closed the stream when it wrote the footer.
                if (stream.CanWrite)
                    await stream.FlushAsync(cancellationToken);
                await stream.DisposeAsync();

                File.Move(tempPath, finalPath, overwrite: true);
                committed = true;
                return new FileInfo(finalPath).Length;
            }

            public async ValueTask DisposeAsync()
            {
                await stream.DisposeAsync();

                if (!committed)
                {
                    try { File.Delete(tempPath); } catch (IOException) { /* removed with the query folder */ }
                }
            }
        }
    }
}
