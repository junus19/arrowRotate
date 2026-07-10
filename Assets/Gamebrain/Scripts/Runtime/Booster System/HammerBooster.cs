using System;
using GameBrain.Casual;
using DG.Tweening;
using UnityEngine;
using GameBrain.Utils;

namespace GameBrain.Casual
{
    public class HammerBooster : BaseBooster
    {
        private Action hitAction;

        public Transform hammerParent;
        public Transform hammerAnimParent;

        public GameObject hammerHitEffectObject;
        private ICell targetCell;

        protected override void StartBoosterAction(BoosterRequestedEvent eventInfo)
        {
            base.StartBoosterAction(eventInfo);
            hammerHitEffectObject.SetActive(false);
            hammerAnimParent.transform.localScale = Vector3.one;
        }

        public void SetHammerForHit()
        {
            Vector3 cellPosition = targetCell.transform.position;
            Vector3 hitPos = new Vector3(cellPosition.x, cellPosition.y + 1, cellPosition.z - 2.1f);
            hammerParent.transform.position = hitPos;

            ExecuteBoosterAction();
        }

        protected override void UpdateLogic()
        {
            base.UpdateLogic();

            if (Input.GetMouseButtonDown(0))
            {
                Ray ray = CameraManager.Instance.GameplayCamera.ScreenPointToRay(Input.mousePosition);
            
                if (Physics.Raycast(ray.origin, ray.direction, out RaycastHit hit, Mathf.Infinity, LayerMask))
                {
                    if (hit.transform.parent.gameObject.TryGetComponent(out ICell cell))
                    {
                        if (cell.HasDamageableBoardObject() || cell.GetTotalTokenCount() > 0)
                        {
                            targetCell = cell;
                            SetHammerForHit();
                        }
                        else
                            EndBoosterAction(false);
                    }
                }
                else
                {
                    EndBoosterAction(false);
                }
            }
        }

        protected override void ExecuteBoosterAction()
        {
            base.ExecuteBoosterAction();

            hammerParent.gameObject.SetActive(true);
            DOVirtual.DelayedCall(1.0f, () => HammerAfterHitAction());
        }

        private void HammerAfterHitAction()
        {
            hammerHitEffectObject.SetActive(true);
            hammerAnimParent.DOScale(Vector3.zero, 0.35f).SetDelay(0.2f).OnComplete(()=> hammerParent.gameObject.SetActive(false));
            EndBoosterAction(true);
        }

        protected override void EndBoosterAction(bool isUsed)
        {
            base.EndBoosterAction(isUsed);

            if(isUsed)
            {
                if (boosterManager != null)
                    UseBooster();
                
                if (targetCell.HasDamageableBoardObject())
                    ((DamageableBoardObject)targetCell.BoardObject).TakeDamage(100);
                else if (targetCell.GetTotalTokenCount()>0)
                    targetCell.ClearHexes();
                
                EventBus<FxRequestEvent>.Raise(new FxRequestEvent(EffectType.Hammer));
            }
        }
    }
}
