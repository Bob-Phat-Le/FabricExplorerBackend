using FabricExplorerBackend.Application.Configuration.Documents;
using FabricExplorerBackend.Commons.Errors;
using FabricExplorerBackend.Infrastructures.Options;
using FluentValidation;
using Microsoft.Extensions.Options;

namespace FabricExplorerBackend.Application.Configuration.Validation
{
    /// <summary>Limits of a configuration file that has been read. Stops at the first problem.</summary>
    public sealed class ConfigurationDocumentValidator : AbstractValidator<ConfigurationDocument>
    {
        public ConfigurationDocumentValidator(IOptions<ConfigurationTransferOptions> options)
        {
            var maxEntries = options.Value.MaxEntriesPerType;
            var message = $"A file can hold at most {maxEntries} entries per type.";

            ClassLevelCascadeMode = CascadeMode.Stop;

            RuleFor(x => x.Connections)
                .Must(connections => connections is null || connections.Count <= maxEntries)
                    .WithErrorCode(ErrorCodes.Configuration.TooLarge)
                    .WithMessage(message);

            RuleFor(x => x.SavedQueries)
                .Must(savedQueries => savedQueries is null || savedQueries.Count <= maxEntries)
                    .WithErrorCode(ErrorCodes.Configuration.TooLarge)
                    .WithMessage(message);
        }
    }
}
