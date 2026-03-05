using System.Collections.Generic;
using UnityEngine;

namespace FakeMG.Settings
{
    [CreateAssetMenu(menuName = "Settings/Common/On Off Option Setting")]
    public class OnOffOptionSettingSO : OptionSettingSO
    {
        private static readonly string[] ON_OFF_OPTIONS =
        {
            "Off",
            "On"
        };

        public override List<string> GetOptions()
        {
            List<string> onOffOptions = new List<string>(ON_OFF_OPTIONS.Length);

            for (int index = 0; index < ON_OFF_OPTIONS.Length; index++)
            {
                onOffOptions.Add(ON_OFF_OPTIONS[index]);
            }

            return onOffOptions;
        }
    }
}
