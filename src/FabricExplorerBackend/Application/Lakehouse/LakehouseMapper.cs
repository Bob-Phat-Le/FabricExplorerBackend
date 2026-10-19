using FabricExplorerBackend.Application.Lakehouse;
using FabricExplorerBackend.Application.Lakehouse.Contracts;
using FabricExplorerBackend.Infrastructures.Fabric.Dtos;
using Microsoft.Fabric.Api.Lakehouse.Models;
using Riok.Mapperly.Abstractions;
using System.Globalization;
using System.Text.Json;

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

        public LoadTableRequest Map(ApplicationLoadTableRequest request)
        {
            TextInfo ti = CultureInfo.CurrentCulture.TextInfo;
            var format = ti.ToTitleCase(request.FormatOptions.GetProperty("format").ToString().ToLower());

            return new LoadTableRequest(request.RelativePath, new PathType(ti.ToTitleCase(request.PathType.ToLower())))
            {
                Recursive = request.Recursive,
                FileExtension = ti.ToTitleCase(request.FileExtension.ToLower()),
                Mode = ti.ToTitleCase(request.Mode.ToLower()) switch
                {
                    "Append" => ModeType.Append,
                    "Overwrite" => ModeType.Overwrite,
                    _ => throw new Exception()
                },
                FormatOptions = format switch
                {
                    "Csv" => JsonSerializer.Deserialize<Csv>(request.FormatOptions),
                    "Parquet" => JsonSerializer.Deserialize<Parquet>(request.FormatOptions),
                    _ => throw new Exception()
                }
            };
        }
    }
}
