using FabricExplorerBackend.Application.Configuration.Documents;
using FabricExplorerBackend.Commons.Errors;
using FabricExplorerBackend.Infrastructures.Options;
using FluentValidation;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace FabricExplorerBackend.Application.Configuration.Validation
{
    /// <summary>Is the uploaded file something this build can read at all? Stops at the first problem.</summary>
    public sealed class ConfigurationFileHeaderValidator : AbstractValidator<ConfigurationFileHeader>
    {
        public ConfigurationFileHeaderValidator(IOptions<ConfigurationTransferOptions> options)
        {
            var limits = options.Value;

            ClassLevelCascadeMode = CascadeMode.Stop;

            RuleFor(x => x.Kind)
                .Must(kind => kind == JsonValueKind.Object)
                    .WithErrorCode(ErrorCodes.Configuration.InvalidFormat)
                    .WithMessage("The configuration file must be a JSON object.");

            RuleFor(x => x.RawLength)
                .LessThanOrEqualTo(limits.MaxFileSizeBytes)
                    .WithErrorCode(ErrorCodes.Configuration.TooLarge)
                    .WithMessage($"The configuration file exceeds {limits.MaxFileSizeBytes} bytes.");

            RuleFor(x => x.Format)
                .Must(format => string.Equals(format, ConfigurationFormat.Name, StringComparison.Ordinal))
                    .WithErrorCode(ErrorCodes.Configuration.InvalidFormat)
                    .WithMessage($"The file is not a '{ConfigurationFormat.Name}' document.");

            RuleFor(x => x.FormatVersion)
                .Must(version => version is >= 1)
                    .WithErrorCode(ErrorCodes.Configuration.InvalidFormat)
                    .WithMessage("The file has no valid 'formatVersion'.");

            RuleFor(x => x.FormatVersion)
                .Must(version => limits.SupportedFormatVersions.Contains(version!.Value))
                    .WithErrorCode(ErrorCodes.Configuration.UnsupportedVersion)
                    .WithMessage(header => $"Format version {header.FormatVersion} is not supported. Supported versions: {string.Join(", ", limits.SupportedFormatVersions)}.");
        }
    }
}
