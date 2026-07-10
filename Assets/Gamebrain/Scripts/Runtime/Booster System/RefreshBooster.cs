using DG.Tweening;

namespace GameBrain.Casual
{
    public class RefreshBooster : BaseBooster
    {
        protected override void StartBoosterAction(BoosterRequestedEvent eventInfo)
        {
            base.StartBoosterAction(eventInfo);
            if (eventInfo.BoosterType != boosterItemData.BoosterType)
                return;
            ExecuteBoosterAction();
        }

        protected override void ExecuteBoosterAction()
        {
            base.ExecuteBoosterAction();

            bool result = _gameplayManager.RefreshShapes();
            DOVirtual.DelayedCall(.3f, () => EndBoosterAction(result));
        }

        protected override void EndBoosterAction(bool isUsed)
        {
            base.EndBoosterAction(isUsed);
            if (isUsed && boosterManager != null)
                UseBooster();
        }
    }
}
