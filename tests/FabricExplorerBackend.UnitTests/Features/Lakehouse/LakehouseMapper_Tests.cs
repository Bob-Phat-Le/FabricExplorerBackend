using FabricExplorerBackend.Application.Lakehouse.Contracts;
using FabricExplorerBackend.Features.Fabric.Lakehouse;
using FabricExplorerBackend.Infrastructures.Fabric.Dtos;
using Microsoft.Fabric.Api.Lakehouse.Models;
using System.Text.Json;

namespace FabricExplorerBackend.UnitTests.Features.Lakehouse
{
    public class LakehouseMapper_Tests
    {
        private readonly LakehouseMapper _mapper = new();

        [Fact]
        public void Map_FabricTableResponse_MapsBasicFields()
        {
            var source = new FabricTableResponse
            {
                Name = "Customers",
                Type = "Managed",
                Format = "delta"
            };

            var result = _mapper.Map(source);

            Assert.NotNull(result);
            Assert.Equal("Customers", result.Name);
            Assert.Equal("Managed", result.Type);
            Assert.Equal("delta", result.Format);
        }

        [Fact]
        public void Map_ApplicationLoadTableRequest_CsvAppend_MapsCorrectly()
        {
            var json = JsonDocument.Parse("{\"format\":\"csv\",\"header\":true}").RootElement;
            var request = new ApplicationLoadTableRequest
            {
                RelativePath = "data/customers.csv",
                PathType = "File",
                Mode = "Append",
                FileExtension = "csv",
                Recursive = false,
                FormatOptions = json
            };

            var result = _mapper.Map(request);

            Assert.NotNull(result);
            Assert.Equal("data/customers.csv", result.RelativePath);
            Assert.Equal(ModeType.Append, result.Mode);
            Assert.Equal("Csv", result.FileExtension);
            Assert.IsType<Csv>(result.FormatOptions);
        }

        [Fact]
        public void Map_ApplicationLoadTableRequest_ParquetOverwrite_MapsCorrectly()
        {
            var json = JsonDocument.Parse("{\"format\":\"parquet\"}").RootElement;
            var request = new ApplicationLoadTableRequest
            {
                RelativePath = "data/parquet_folder",
                PathType = "Folder",
                Mode = "Overwrite",
                FileExtension = "parquet",
                Recursive = true,
                FormatOptions = json
            };

            var result = _mapper.Map(request);

            Assert.NotNull(result);
            Assert.Equal("data/parquet_folder", result.RelativePath);
            Assert.Equal(ModeType.Overwrite, result.Mode);
            Assert.Equal("Parquet", result.FileExtension);
            Assert.True(result.Recursive);
            Assert.IsType<Parquet>(result.FormatOptions);
        }

        [Fact]
        public void Map_ApplicationLoadTableRequest_InvalidMode_ThrowsException()
        {
            var json = JsonDocument.Parse("{\"format\":\"csv\"}").RootElement;
            var request = new ApplicationLoadTableRequest
            {
                RelativePath = "data/file.csv",
                PathType = "File",
                Mode = "InvalidMode",
                FileExtension = "csv",
                FormatOptions = json
            };

            Assert.ThrowsAny<Exception>(() => _mapper.Map(request));
        }

        [Fact]
        public void Map_ApplicationLoadTableRequest_InvalidFormat_ThrowsException()
        {
            var json = JsonDocument.Parse("{\"format\":\"json\"}").RootElement;
            var request = new ApplicationLoadTableRequest
            {
                RelativePath = "data/file.json",
                PathType = "File",
                Mode = "Append",
                FileExtension = "json",
                FormatOptions = json
            };

            Assert.ThrowsAny<Exception>(() => _mapper.Map(request));
        }
    }
}
