using FakeMG.Settings;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace FakeMG.Game
{
    public class UIInjector : IStartable
    {
        private readonly IObjectResolver _container;

        [Inject]
        public UIInjector(IObjectResolver container)
        {
            _container = container;
        }

        public void Start()
        {
            var binders = Object.FindObjectsByType<DropdownSettingUIBinder>(FindObjectsSortMode.None);
            foreach (var binder in binders)
            {
                _container.Inject(binder);
            }

            var sliderBinders = Object.FindObjectsByType<SliderSettingUIBinder>(FindObjectsSortMode.None);
            foreach (var binder in sliderBinders)
            {
                _container.Inject(binder);
            }
        }
    }
}