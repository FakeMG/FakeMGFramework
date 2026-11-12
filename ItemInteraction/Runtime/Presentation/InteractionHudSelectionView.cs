
namespace FakeMG.ItemInteraction
{
    public sealed class InteractionHudSelectionView : IItemSelectionPresentation
    {
        private readonly InteractionHudView _hudView;

        public InteractionHudSelectionView(InteractionHudView hudView)
        {
            _hudView = hudView;
        }

        public void ShowSelection(IInteractionItem item)
        {
            _hudView.SetMobileUseVisible(item is IActionItem);
        }

        public void HideSelection()
        {
            _hudView.SetMobileUseVisible(true);
        }
    }
}
