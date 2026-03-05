using VContainer;
using VContainer.Unity;

namespace FakeMG.Settings
{
    public class GameLifetimeScope : LifetimeScope
    {
        protected override void Configure(IContainerBuilder builder)
        {
            builder.RegisterComponentInHierarchy<SettingDataManager>();
            builder.RegisterComponentInHierarchy<SliderSettingUIBinder>();
            builder.RegisterComponentInHierarchy<DropdownSettingUIBinder>();
        }
    }
}