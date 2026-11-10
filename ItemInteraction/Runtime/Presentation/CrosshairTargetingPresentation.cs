
namespace FakeMG.ItemInteraction
{
    public sealed class CrosshairTargetingPresentation : IItemUsePresentation
    {
        private readonly InteractionHudView _hudView;

        public CrosshairTargetingPresentation(InteractionHudView hudView)
        {
            _hudView = hudView;
        }

        public void BeginUse(IInteractionItem item, ItemActionSO actionSO)
        {
            if (actionSO.TargetingProfileSO is CrosshairTargetingProfileSO)
            {
                _hudView.SetCrosshairVisible(true);
            }
        }

        public void EndUse(IInteractionItem item, ItemActionSO actionSO)
        {
            if (actionSO.TargetingProfileSO is CrosshairTargetingProfileSO)
            {
                _hudView.SetCrosshairVisible(false);
            }
        }
    }
}
