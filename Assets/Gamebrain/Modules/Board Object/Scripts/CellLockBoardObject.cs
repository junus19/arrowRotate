using TMPro;
using UnityEngine;
using GameBrain.Utils;
using GameBrain.Casual;

namespace GameBrain.Casual
{
    public class CellLockBoardObject : BoardObject
    {
        [SerializeField] int unlockScore;
        [SerializeField] Transform lockVisualAndTextParent;
        [SerializeField] CellLockVisual lockVisual;
        [SerializeField] TextMeshPro unlockScoreTxt;
        [SerializeField] GameObject fillLockObject;
        [SerializeField] GameObject glassFx;
        [SerializeField] bool hasFill;

        private EventBinding<ScoreUpdateEvent> scoreUpdateEventBinding;

        public override void LoadBoardObject(ICell cell, int objectValue)
        {
             currentCell = cell;
             currentCell.LoadBoardObject(this);
             unlockScore = objectValue;
             unlockScoreTxt.text = unlockScore.ToString();
             fillLockObject.SetActive(false);
            // Vector3 boardObjectPos = currentCell.transform.position;
            //  int tokenCount = currentCell.GetTotalTokenCount();
            // if (tokenCount > 0)
            // {
            //     boardObjectPos.y = currentCell.CurrentFillContainer.GetTopFillGroup().FillInfos[0].fill.NumberContainerPosition.y;
            // }
            
            // transform.position = boardObjectPos;
            //
            //

            ApplyCellStatusChanges();
            scoreUpdateEventBinding = new EventBinding<ScoreUpdateEvent>(OnScoreUpdated);
            EventBus<ScoreUpdateEvent>.Register(scoreUpdateEventBinding);
        }

        public override void UnloadAndDestroyBoardObject()
        {
            // //OnBoardObjectUnloaded.Invoke();
            // EventBus<ScoreUpdateEvent>.Deregister(scoreUpdateEventBinding);
            //
            currentCell.UnLoadBoardObject();
             Destroy(this.gameObject);
        }

        private void OnScoreUpdated(ScoreUpdateEvent eventInfo)
        {
            int leftScore = unlockScore - eventInfo.score;
            //unlockScoreTxt.text = leftScore.ToString();

            if (leftScore <= 0)
            {
                lockVisual.DestroyAnim();
                UnloadAndDestroyBoardObject();
            }

        }

        public override void ApplyCellStatusChanges()
        {
             base.ApplyCellStatusChanges();
            int tokenCount = currentCell.GetTotalTokenCount();
            if(tokenCount <= 0)
            return;

            fillLockObject.SetActive(true);
            //
             Vector3 boardObjectPos = currentCell.transform.position;

            if (tokenCount > 0)
            {
                hasFill = true;
                boardObjectPos.z = transform.position.z - tokenCount * 0.22f;
            }
            //
             transform.position = boardObjectPos;
        }

        private void OnDestroy()
        {
            EventBus<ScoreUpdateEvent>.Deregister(scoreUpdateEventBinding);
            if(hasFill)
            {
                glassFx.transform.SetParent(null);
                glassFx.SetActive(true);
            }
        }
    }
}
