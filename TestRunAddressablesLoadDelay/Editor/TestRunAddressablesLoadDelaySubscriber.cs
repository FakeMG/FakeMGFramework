using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;

namespace FakeMG.TestRunAddressablesLoadDelay.Editor
{
    [InitializeOnLoad]
    public sealed class TestRunAddressablesLoadDelaySubscriber : ICallbacks
    {
        private readonly TestRunAddressablesLoadDelay _loadDelay;

        // The test runner clears its callbacks on domain reload, and this constructor re-registers on every reload,
        // so the registration lives exactly as long as the domain and needs no explicit unregistration.
        static TestRunAddressablesLoadDelaySubscriber()
        {
            TestRunnerApi.RegisterTestCallback(new TestRunAddressablesLoadDelaySubscriber(new TestRunAddressablesLoadDelay()));
        }

        private TestRunAddressablesLoadDelaySubscriber(TestRunAddressablesLoadDelay loadDelay)
        {
            _loadDelay = loadDelay;
        }

        #region Public Methods

        public void RunStarted(ITestAdaptor testsToRun)
        {
            _loadDelay.OverrideAuthoredDelayForTestRun();
        }

        public void RunFinished(ITestResultAdaptor result)
        {
            _loadDelay.RestoreAuthoredDelay();
        }

        public void TestStarted(ITestAdaptor test)
        {
        }

        public void TestFinished(ITestResultAdaptor result)
        {
        }

        #endregion
    }
}
