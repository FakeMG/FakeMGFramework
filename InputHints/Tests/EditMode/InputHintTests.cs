using NUnit.Framework;

namespace FakeMG.InputHints.Tests.EditMode
{
    public sealed class InputHintTests
    {
        [Test]
        public void SettingTheSameVisibilityTwice_ReportsOneChange()
        {
            var hint = new InputHint("Load");
            int changeCount = 0;
            hint.OnChanged += () => changeCount++;

            hint.SetShown(false);
            hint.SetShown(false);

            Assert.That(changeCount, Is.EqualTo(1));
        }

        [Test]
        public void SettingTheSameLabel_ReportsNoChange()
        {
            var hint = new InputHint("Load");
            int changeCount = 0;
            hint.OnChanged += () => changeCount++;

            hint.SetLabel("Load");

            Assert.That(changeCount, Is.Zero);
        }
    }
}
