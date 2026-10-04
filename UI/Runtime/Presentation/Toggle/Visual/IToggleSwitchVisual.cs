namespace FakeMG.Framework.UI.Toggle
{
    public interface IToggleSwitchVisual
    {
        void Apply(bool isOn, float normalizedValue, bool instant);
    }
}