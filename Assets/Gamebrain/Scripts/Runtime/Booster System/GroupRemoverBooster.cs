// using DG.Tweening;
using UnityEngine;

namespace GameBrain.Casual
{
    public class GroupRemoverBooster : BaseBooster
    {
        protected override void StartBoosterAction(BoosterRequestedEvent eventInfo)
        {
            base.StartBoosterAction(eventInfo);
            
            if (eventInfo.BoosterType != boosterItemData.BoosterType)
                return;

            // bool hasAnyGroup = false;
            // foreach (Cell cell in _gameplayManager.cellList)
            // {
            //     if (cell.type != CellType.normal || cell.CurrentFillContainer == null || cell.CurrentFillContainer.GetFillInfo() == null) continue;
            //
            //     foreach (FillInfo fillInfo in cell.CurrentFillContainer.GetFillInfo())
            //     {
            //         fillInfo.fill.UpdateCollider();
            //         if (fillInfo.fill.groupID != -1)
            //             hasAnyGroup = true;
            //     }
            // }

            // if (!hasAnyGroup)
                EndBoosterAction(false);
        }

        protected override void UpdateLogic()
        {
            base.UpdateLogic();
            // if (Input.GetMouseButtonDown(0))
            // {
            //     Ray ray = GameplayCamera.GetComponent<Camera>().ScreenPointToRay(Input.mousePosition);
            //
            //     int layerMask = LayerMask.GetMask("Fill");
            //     if (Physics.Raycast(ray.origin, ray.direction, out RaycastHit hit, Mathf.Infinity, layerMask))
            //     {
            //         if (hit.transform.gameObject.TryGetComponent(out Fill fill) && fill.groupID != -1)
            //         {
            //             _gameplayManager.RemoveGroup(fill.groupID);
            //             ExecuteBoosterAction();
            //         }
            //     }
            // }
        }
        
        protected override void ExecuteBoosterAction()
        {
            base.ExecuteBoosterAction();
            isActive = false;
            // DOVirtual.DelayedCall(.5f, ()=> EndBoosterAction(true));
        }
        
        protected override void EndBoosterAction(bool isUsed)
        {
            base.EndBoosterAction(isUsed);

            if (!isUsed || boosterManager == null) return;
            UseBooster();
        }
    }
}
