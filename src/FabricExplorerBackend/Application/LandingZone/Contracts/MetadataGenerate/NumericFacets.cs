namespace FabricExplorerBackend.Application.LandingZone.Contracts.MetadataGenerate
{
    public sealed class NumericFacets
    {
        public decimal? Min { get; set; }
        public decimal? Max { get; set; }
        public int? TotalDigits { get; set; }
        public int? FractionDigits { get; set; }
    }
}
