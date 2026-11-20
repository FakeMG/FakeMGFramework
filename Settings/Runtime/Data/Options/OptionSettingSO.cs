using System;
using System.Collections.Generic;

namespace FakeMG.Settings
{
    /// <summary>A setting that stores the id of one of its options.</summary>
    public abstract class OptionSettingSO : SettingDefinitionGenericSO<string>
    {
        public const int NO_OPTION_INDEX = -1;

        public abstract IReadOnlyList<SettingOption> GetOptions();

        /// <returns>
        /// The first option for an unknown id, so a value saved by an older build still resolves to a valid choice.
        /// <see cref="NO_OPTION_INDEX"/> only when there are no options.
        /// </returns>
        public int IndexOfOptionOrDefault(string optionId)
        {
            IReadOnlyList<SettingOption> options = GetOptions();
            for (int index = 0; index < options.Count; index++)
            {
                if (string.Equals(options[index].Id, optionId, StringComparison.Ordinal))
                {
                    return index;
                }
            }

            return options.Count > 0 ? 0 : NO_OPTION_INDEX;
        }
    }
}
