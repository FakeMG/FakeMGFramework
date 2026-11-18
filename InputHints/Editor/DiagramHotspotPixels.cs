namespace FakeMG.InputHints.Editor
{
    /// <summary>A control's box on the diagram image, in pixels from its top-left corner, as an image editor shows them.</summary>
    internal readonly struct DiagramHotspotPixels
    {
        public string ControlPath { get; }
        public float CenterXPixels { get; }
        public float CenterYPixels { get; }
        public float WidthPixels { get; }
        public float HeightPixels { get; }

        public DiagramHotspotPixels(string controlPath, float centerXPixels, float centerYPixels, float widthPixels, float heightPixels)
        {
            ControlPath = controlPath;
            CenterXPixels = centerXPixels;
            CenterYPixels = centerYPixels;
            WidthPixels = widthPixels;
            HeightPixels = heightPixels;
        }
    }
}
