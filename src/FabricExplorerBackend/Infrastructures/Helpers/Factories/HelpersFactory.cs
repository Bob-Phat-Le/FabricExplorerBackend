using FabricExplorerBackend.Infrastructures.Helpers.HelperConcretes;
using FabricExplorerBackend.Infrastructures.Helpers.HelperConcretes.LandingZone;
using FabricExplorerBackend.Infrastructures.Options;
using Microsoft.Extensions.Options;

namespace FabricExplorerBackend.Infrastructures.Helpers.Factories
{
    public class HelpersFactory(IServiceProvider serviceProvider) : IHelpersFactory
    {
        public T GetHelper<T>(string helperName) where T : IBaseHelper
        {
            IBaseHelper helper = helperName switch
            {
                HelperNames.LandingZoneHelper => serviceProvider.GetRequiredService<ILandingZoneHelper>(),
                _ => throw new NotImplementedException()
            };

            return helper is T result
                ? result
                : throw new InvalidOperationException($"Helper '{helperName}' does not implement {typeof(T).Name}");
        }
    }
}
