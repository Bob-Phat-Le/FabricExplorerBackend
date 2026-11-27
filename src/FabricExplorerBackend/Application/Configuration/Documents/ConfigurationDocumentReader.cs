using FabricExplorerBackend.Application.Configuration.Validation;
using FabricExplorerBackend.Commons.Errors;
using FabricExplorerBackend.Commons.Results;
using FluentValidation;
using System.Text.Json;

namespace FabricExplorerBackend.Application.Configuration.Documents
{
    /// <summary>Turns the uploaded JSON into a <see cref="ConfigurationDocument"/> or says why it cannot be used.</summary>
    public sealed class ConfigurationDocumentReader(
        IValidator<ConfigurationFileHeader> headerValidator,
        IValidator<ConfigurationDocument> documentValidator) : IConfigurationDocumentReader
    {
        // basically, this function is just deserializing the JSON into a ConfigurationDocument; the checks before and after
        // are done by the validators, which also say why a file cannot be used in a structured way.
        public Result<ConfigurationDocument> Read(JsonElement element)
        {
            var header = headerValidator.Validate(ConfigurationFileHeader.From(element));
            if (!header.IsValid)
                return header.ToBadRequest();

            ConfigurationDocument? document;
            try
            {
                document = element.Deserialize<ConfigurationDocument>(ConfigurationJson.Read);
            }
            catch (JsonException ex)
            {
                return Error.BadRequest(ErrorCodes.Configuration.InvalidFormat, $"The configuration file is malformed: {ex.Message}");
            }

            if (document is null)
                return Error.BadRequest(ErrorCodes.Configuration.InvalidFormat, "The configuration file is empty.");

            document.Connections?.RemoveAll(entry => entry is null);
            document.SavedQueries?.RemoveAll(entry => entry is null);

            var content = documentValidator.Validate(document);
            if (!content.IsValid)
                return content.ToBadRequest();

            return Result<ConfigurationDocument>.Success(document);
        }
    }
}
