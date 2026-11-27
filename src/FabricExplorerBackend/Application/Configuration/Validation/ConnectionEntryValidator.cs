using FabricExplorerBackend.Application.Configuration.Documents;
using FabricExplorerBackend.Application.LandingZone.Contracts.Explorers;
using FabricExplorerBackend.Infrastructures.Helpers.HelperConcretes.LandingZone;
using FluentValidation;
using ConnectionEntity = FabricExplorerBackend.Domain.Entities.Connection;

namespace FabricExplorerBackend.Application.Configuration.Validation
{
    /// <summary>Rules of a connection entry of a configuration file. Rules run in the order they are declared.</summary>
    public sealed class ConnectionEntryValidator : AbstractValidator<ConnectionEntry>
    {
        public const int NameMaxLength = 200;

        private readonly ILandingZoneHelper landingZoneHelper;

        public ConnectionEntryValidator(ILandingZoneHelper landingZoneHelper)
        {
            this.landingZoneHelper = landingZoneHelper;

            RuleFor(x => x.Key)
                .MustBeGuid(ValidationCodes.InvalidKey, "Key must be a non-empty GUID.");

            RuleFor(x => x.Name)
                .Cascade(CascadeMode.Stop)
                .NotEmpty()
                    .WithErrorCode(ValidationCodes.Required)
                    .WithMessage("Name is required.")
                .Must(name => name!.Trim().Length <= NameMaxLength)
                    .WithErrorCode(ValidationCodes.TooLong)
                    .WithMessage($"Name must be at most {NameMaxLength} characters.");

            RuleFor(x => x.WorkspaceId)
                .MustBeGuid(ValidationCodes.InvalidGuid, "Workspace id must be a GUID.");

            RuleFor(x => x.TenantId)
                .MustBeGuid(ValidationCodes.InvalidGuid, "Tenant id must be a GUID.");

            RuleFor(x => x.ClientId)
                .MustBeGuid(ValidationCodes.InvalidGuid, "Client id must be a GUID.");

            // The token endpoint is optional; when present it must be /{tenant}/oauth2[/v2.0]/token of the same tenant.
            When(x => !string.IsNullOrWhiteSpace(x.TokenEndpoint), () =>
            {
                RuleFor(x => x.TokenEndpoint)
                    .Cascade(CascadeMode.Stop)
                    .Must(TokenEndpoints.IsHttpsUrl)
                        .WithErrorCode(ValidationCodes.InvalidTokenEndpoint)
                        .WithMessage("Token endpoint must be an https URL.")
                    .Must(endpoint => TokenEndpoints.TryGetTenantSegment(endpoint, out _))
                        .WithErrorCode(ValidationCodes.InvalidTokenEndpoint)
                        .WithMessage("Token endpoint must look like /{tenant}/oauth2/v2.0/token.")
                    .Must((entry, endpoint) => BelongsToTenantOf(entry, endpoint))
                        .WithErrorCode(ValidationCodes.TenantMismatch)
                        .WithMessage("Token endpoint belongs to a different tenant than tenantId.");
            });

            RuleFor(x => x.Environment)
                .Must(environment => environment is null || environment.Trim().Length <= ConnectionEntity.EnvironmentMaxLength)
                    .WithErrorCode(ValidationCodes.TooLong)
                    .WithMessage($"Environment must be at most {ConnectionEntity.EnvironmentMaxLength} characters.");

            // The Landing Zone URI is checked with the same parser the Landing Zone feature uses.
            RuleFor(x => x.LandingZone)
                .Must((entry, landingZone, context) =>
                {
                    var problem = FindLandingZoneProblem(entry, landingZone);
                    if (problem is null)
                        return true;

                    context.MessageFormatter.AppendArgument("Reason", problem);
                    return false;
                })
                    .WithErrorCode(ValidationCodes.InvalidLandingZone)
                    .WithMessage("{Reason}")
                    .OverridePropertyName("LandingZone.Uri")
                    .When(x => !string.IsNullOrWhiteSpace(x.LandingZone?.Uri));
        }

        /// <summary>An unreadable tenantId is reported by its own rule, so it does not count as a mismatch here.</summary>
        private static bool BelongsToTenantOf(ConnectionEntry entry, string? endpoint)
        {
            if (!ConfigurationGuid.TryParse(entry.TenantId, out var tenantId))
                return true;

            return TokenEndpoints.TryGetTenantSegment(endpoint, out var tenant)
                && string.Equals(tenant, tenantId.ToString("D"), StringComparison.OrdinalIgnoreCase);
        }

        private string? FindLandingZoneProblem(ConnectionEntry entry, LandingZoneEntry? landingZone)
        {
            ConfigurationGuid.TryParse(entry.WorkspaceId, out var workspaceId);

            var parsed = landingZoneHelper.BuildLocation(
                new ConnectionEntity { WorkspaceId = workspaceId },
                new ConfigureLandingZoneRequest(null, landingZone!.Uri));

            return parsed.IsSuccess ? null : parsed.Errors[0].Message;
        }
    }
}
