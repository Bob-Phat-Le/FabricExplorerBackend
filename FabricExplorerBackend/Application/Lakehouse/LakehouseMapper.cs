using FabricExplorerBackend.Application.Lakehouse;
using FabricExplorerBackend.Application.Lakehouse.Contracts;
using FabricExplorerBackend.Infrastructures.Fabric.Dtos;
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
