using System.Collections.Generic;
using FakeMG.Framework;
using UnityEngine;

namespace FakeMG.Settings
{
    [CreateAssetMenu(menuName = FakeMGEditorMenus.SETTINGS_COMMON + "/On Off Option Setting")]
    public class OnOffOptionSettingSO : OptionSettingSO
    {
        private static readonly string[] ON_OFF_OPTIONS =
        {
            "Off",
            "On"
        };

        public override List<string> GetOptions()
        {
            List<string> onOffOptions = new(ON_OFF_OPTIONS.Length);

            for (int index = 0; index < ON_OFF_OPTIONS.Length; index++)
            {
                onOffOptions.Add(ON_OFF_OPTIONS[index]);
            }

            return onOffOptions;
        }
    }
}
