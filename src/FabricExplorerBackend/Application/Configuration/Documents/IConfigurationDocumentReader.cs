using FabricExplorerBackend.Commons.Results;
using System.Text.Json;

namespace FabricExplorerBackend.Application.Configuration.Documents
{
    public interface IConfigurationDocumentReader
    {
        Result<ConfigurationDocument> Read(JsonElement element);
    }
}
