using FabricExplorerBackend.Application.LandingZone.Contracts.MetadataGenerate;

namespace FabricExplorerBackend.Application.LandingZone.MetadataGenerate.Generation
{
    public interface IMetadataGenerator
    {
        /// <summary>Validates the request and renders _metadata.json. Invalid input is reported in the response, never thrown.</summary>
        PreviewResponse Generate(PreviewRequest request);
    }
}
