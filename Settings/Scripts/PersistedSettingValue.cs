namespace FakeMG.Settings
{
    public readonly struct PersistedSettingValue
    {
        public string SerializedValue { get; }
        public string TypeId { get; }

        public PersistedSettingValue(string serializedValue, string typeId)
        {
            SerializedValue = serializedValue;
            TypeId = typeId;
        }
    }
}
