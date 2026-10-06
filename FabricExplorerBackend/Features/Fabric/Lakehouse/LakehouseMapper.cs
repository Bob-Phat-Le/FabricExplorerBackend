using FabricExplorerBackend.Commons.Models.Responses.Fabric.FabricClient;
using Riok.Mapperly.Abstractions;

namespace FabricExplorerBackend.Features.Fabric.Lakehouse
{
    [Mapper]
    public partial class LakehouseMapper : ILakehouseMapper
    {
        [MapperIgnoreTarget(nameof(TableResponse.RowCount))]
        [MapperIgnoreTarget(nameof(TableResponse.LastModifiedTime))]
        [MapperIgnoreTarget(nameof(TableResponse.Status))]
        [MapperIgnoreTarget(nameof(TableResponse.StatusMessage))]
        public partial TableResponse Map(FabricTableResponse fabricTableResponse);
    }
}
