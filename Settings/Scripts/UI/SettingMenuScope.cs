using System.Linq;
using VContainer;
using VContainer.Unity;

namespace FakeMG.Settings
{
    public class SettingMenuScope : LifetimeScope
    {
        protected override void Configure(IContainerBuilder builder)
        {
            autoInjectGameObjects.AddRange(GetComponentsInChildren<SliderSettingUIBinder>(true).Select(b => b.gameObject).ToList());
            autoInjectGameObjects.AddRange(GetComponentsInChildren<DropdownSettingUIBinder>(true).Select(b => b.gameObject).ToList());
        }
    }
}