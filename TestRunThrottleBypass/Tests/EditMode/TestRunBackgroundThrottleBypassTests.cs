using FakeMG.TestRunThrottleBypass.Editor;
using NUnit.Framework;

namespace FakeMG.TestRunThrottleBypass.Tests
{
    public sealed class TestRunBackgroundThrottleBypassTests
    {
        #region Public Methods

        // Regression guard: the bypass resolves internal Unity names by reflection. If an editor or test framework
        // upgrade renames them, unfocused test runs silently fall back to ~10 editor ticks per second (~10x slower).
        [Test]
        public void InternalUnityApisItDependsOnStillResolve()
        {
            var bypass = new TestRunBackgroundThrottleBypass();

            Assert.That(bypass.IsSupported, Is.True);
        }

        #endregion
    }
}
