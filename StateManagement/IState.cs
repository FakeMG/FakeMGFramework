namespace FakeMG.FakeMGFramework.StateManagement
{
    public interface IState
    {
        public void Tick();
        public void OnEnter();
        public void OnExit();
    }
}