namespace FakeMG.Settings
{
    public interface ISettingValueRuntimeStorage
    {
        SettingDefinitionSO SettingSO { get; }

        string FormatValue();

        /// <param name="hasChanged">Whether the restored value differs from the previous one.</param>
        /// <returns>False when the text is not a valid value; the value is then left as it was.</returns>
        bool TryRestoreFromText(string savedText, out bool hasChanged);

        /// <returns>Whether the value changed.</returns>
        bool ResetToDefault();
    }
}
