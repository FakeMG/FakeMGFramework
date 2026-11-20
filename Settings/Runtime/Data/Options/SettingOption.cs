namespace FakeMG.Settings
{
    /// <summary>One choice of an option setting. The id is saved; the label is only shown.</summary>
    public readonly struct SettingOption
    {
        public string Id { get; }
        public string Label { get; }

        public SettingOption(string id, string label)
        {
            Id = id;
            Label = label;
        }
    }
}
