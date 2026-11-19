using FabricExplorerBackend.Infrastructures.Helpers.HelperConcretes;

namespace FabricExplorerBackend.Infrastructures.Helpers.Factories
{
    public interface IHelpersFactory
    {
        T GetHelper<T>(string helperName) where T : IBaseHelper;
    }
}
