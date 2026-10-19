using FabricExplorerBackend.Features.Fabric.Warehouse;
using FabricExplorerBackend.UnitTests.Common;

namespace FabricExplorerBackend.UnitTests.Features.Warehouse
{
    public class WarehouseMapper_Tests
    {
        private readonly WarehouseMapper _mapper = new();

        [Fact]
        public void Map_SingleWarehouse_MapsProperties()
        {
            var warehouse = FabricTestHelper.CreateWarehouse(Guid.NewGuid(), "DataWarehouse_Prod");

            var response = _mapper.Map(warehouse);

            Assert.NotNull(response);
            Assert.Equal("DataWarehouse_Prod", response.DisplayName);
        }

        [Fact]
        public void Map_WarehouseList_MapsAllItems()
        {
            var list = new[]
            {
                FabricTestHelper.CreateWarehouse(Guid.NewGuid(), "WH_1"),
                FabricTestHelper.CreateWarehouse(Guid.NewGuid(), "WH_2")
            };

            var responses = _mapper.Map(list).ToList();

            Assert.Equal(2, responses.Count);
            Assert.Equal("WH_1", responses[0].DisplayName);
            Assert.Equal("WH_2", responses[1].DisplayName);
        }
    }
}
