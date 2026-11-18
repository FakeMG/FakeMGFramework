namespace FakeMG.KeyBinding
{
    public readonly struct BoundKey
    {
        public string ActionMapName { get; }
        public string ControlPath { get; }

        public BoundKey(string actionMapName, string controlPath)
        {
            ActionMapName = actionMapName;
            ControlPath = controlPath;
        }
    }
}
