using UnityEngine;
using DG.Tweening;
using System.Collections;
using System.Collections.Generic;

namespace GameBrain.Casual
{
    public class LevelObjectivesContainerUI : MonoBehaviour
    {
        public CanvasGroup mainCanvasGroup;
        [SerializeField] Transform boardObjectivesMainParent;
        [SerializeField] List<Transform> boardObjectiveParents;
        [SerializeField] ScoreLevelObjectiveUI scoreLevelObjectiveUI;
        [SerializeField] Transform scoreLevelObjectiveParent;
        public ScoreLevelObjectiveUI ScoreLevelObjectiveUI => scoreLevelObjectiveUI;
        [SerializeField] CanvasGroup bgCanvasGroup;
        [Header("Introduce")] [SerializeField] BoardObjectLevelObjectiveUI boardObjectLevelObjectiveUIPrefab;
        [SerializeField] Transform horizontalLayoutTransform;
        [SerializeField] CanvasGroup introduceBgCanvasGroup;
        [SerializeField] Transform bgMovePoint;
        [SerializeField] List<BoardObjectLevelObjectiveUI> boardObjectLevelObjectiveUIs = new();
        public Sprite scoreSprite;
        public Sprite woodSprite;
        public Sprite iceSprite;
        public Sprite claySprite;

        public void Init()
        {
            foreach (var item in boardObjectLevelObjectiveUIs)
            {
                Destroy(item.gameObject);
            }

            boardObjectLevelObjectiveUIs.Clear();
            foreach (var boardOboardObjectiveParent in boardObjectiveParents)
            {
                boardOboardObjectiveParent.gameObject.SetActive(false);
            }

            introduceBgCanvasGroup.alpha = 1.0f;
            introduceBgCanvasGroup.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
            introduceBgCanvasGroup.transform.localScale = Vector3.zero;
            horizontalLayoutTransform.transform.localScale = Vector3.zero;
            bgCanvasGroup.alpha = 0;
            scoreLevelObjectiveUI.GetComponent<CanvasGroup>().alpha = 0;
            boardObjectivesMainParent.gameObject.GetComponent<CanvasGroup>().enabled = true;
            boardObjectivesMainParent.gameObject.SetActive(false);
        }

        public void SetReadyForLevel()
        {
            hasScore = false;
        }

        public void SetScoreObjective(Sprite icon, LevelGoal goal)
        {
            SetObjective(icon, goal);
        }

        public void SetObjective(Sprite icon, LevelGoal goal)
        {
            Sprite iconnn = null;
            LevelGoalType levelGoalType = LevelGoalType.None;
            if (goal.GoalType != LevelGoalType.Score) boardObjectivesMainParent.gameObject.SetActive(true);
            if (goal.GoalType == LevelGoalType.Score)
                iconnn = scoreSprite;
            else if (goal.GoalType == LevelGoalType.Wood)
                iconnn = woodSprite;
            else if (goal.GoalType == LevelGoalType.Ice)
                iconnn = iceSprite;
            else if (goal.GoalType == LevelGoalType.Clay) iconnn = claySprite;
            if (goal.GoalType == LevelGoalType.Score)
                levelGoalType = LevelGoalType.Score;
            else if (goal.GoalType == LevelGoalType.Wood)
                levelGoalType = LevelGoalType.Wood;
            else if (goal.GoalType == LevelGoalType.Ice)
                levelGoalType = LevelGoalType.Ice;
            else if (goal.GoalType == LevelGoalType.Clay) levelGoalType = LevelGoalType.Clay;
            var objectiveUI = Instantiate(boardObjectLevelObjectiveUIPrefab, horizontalLayoutTransform);
            objectiveUI.Init(levelGoalType, iconnn, goal.TotalGoal, false);
            boardObjectLevelObjectiveUIs.Add(objectiveUI);
        }

        public bool hasScore = false;

        public void SetObjectiveScore(Sprite icon, LevelGoal goal)
        {
            hasScore = true;
            Sprite iconnn = icon;
            LevelGoalType levelGoalType = LevelGoalType.None;
            iconnn = scoreSprite;
            levelGoalType = LevelGoalType.Score;
            scoreLevelObjectiveUI.Init(levelGoalType, iconnn, goal.TotalGoal, false);
        }

        public void ShowObjectives()
        {
            StartCoroutine(IntroduceObjectivesAndGoTopPosition());
        }

        IEnumerator IntroduceObjectivesAndGoTopPosition()
        {
            yield return new WaitForSeconds(0.30f);
            introduceBgCanvasGroup.transform.DOScale(1.0f, 0.1f);
            horizontalLayoutTransform.DOScale(1.0f, 0.1f);
            yield return new WaitForSeconds(0.1f);
            introduceBgCanvasGroup.transform.DOScale(Vector3.one * 1.3f, 0.2f).SetLoops(2, LoopType.Yoyo);
            horizontalLayoutTransform.DOScale(Vector3.one * 1.3f, 0.2f).SetLoops(2, LoopType.Yoyo);
            yield return new WaitForSeconds(1.5f);
            introduceBgCanvasGroup.DOFade(0, 0.8f);
            introduceBgCanvasGroup.transform.DOMove(bgMovePoint.position, 0.8f);

            //yield return new WaitForSeconds(0.5f);
            scoreLevelObjectiveParent.gameObject.SetActive(hasScore);
            Move(null, boardObjectLevelObjectiveUIs, hasScore);
        }

        private void Move(ScoreLevelObjective scoreLevelObjective, List<BoardObjectLevelObjectiveUI> _boardObjectLevelObjectiveUIs, bool _hasScore)
        {
            int boardObjectiveParentIndex = 0;
            for (int i = 0; i < _boardObjectLevelObjectiveUIs.Count; i++)
            {
                if (_hasScore && i == 0)
                {
                    SetAnchorPropertiesAndParent(_boardObjectLevelObjectiveUIs[i].RectTransform, scoreLevelObjectiveUI.IconTransform);
                    _boardObjectLevelObjectiveUIs[i].RectTransform.DOAnchorPos(Vector3.zero, 1.0f)
                        .OnComplete(() => CompleteMovementOfScoreObject(_boardObjectLevelObjectiveUIs[0]));
                    _boardObjectLevelObjectiveUIs[i].UnSubscribeEvents();
                }
                else
                {
                    boardObjectiveParents[boardObjectiveParentIndex].gameObject.SetActive(true);
                    //boardObjectivesMainParent.gameObject.SetActive(true);
                    SetAnchorPropertiesAndParent(_boardObjectLevelObjectiveUIs[i].RectTransform, boardObjectiveParents[boardObjectiveParentIndex]);
                    var cGroup = _boardObjectLevelObjectiveUIs[i].GetComponent<CanvasGroup>();
                    _boardObjectLevelObjectiveUIs[i].RectTransform.DOAnchorPos(Vector3.zero, 1.0f).OnComplete(() => cGroup.enabled = false);
                    //OnComplete(() => CompleteMovementOfBoardObject(boardObjectLevelObjectiveUIs[i]));
                    boardObjectiveParentIndex++;
                }
            }

            bgCanvasGroup.DOFade(1.0f, 0.3f).SetDelay(0.80f);
            boardObjectivesMainParent.gameObject.GetComponent<CanvasGroup>().enabled = false;

            //bgCanvasGroup.transform.DOScale(Vector3.one * 1.2f, 0.2f).SetDelay(0.80f).SetLoops(2, LoopType.Yoyo);
        }

        private void SetAnchorPropertiesAndParent(RectTransform targetRectTransform, Transform newParent)
        {
            Vector3 pos = targetRectTransform.transform.position;

            targetRectTransform.transform.SetParent(null);
            targetRectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            targetRectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            targetRectTransform.pivot = new Vector2(0.5f, 0.5f);
            targetRectTransform.transform.SetParent(newParent);
            targetRectTransform.transform.position = pos;
            //Vector3 aP = targetRectTransform.anchoredPosition;
            //aP.z = 0;
            //targetRectTransform.anchoredPosition = aP;
        }

        private void CompleteMovementOfScoreObject(BoardObjectLevelObjectiveUI objectMoved)
        {
            objectMoved.gameObject.SetActive(false);
            scoreLevelObjectiveUI.CanvasGroup.alpha = 1.0f;
            scoreLevelObjectiveUI.SetVisual();
        }

        private void CompleteMovementOfBoardObject(BoardObjectLevelObjectiveUI objectMoved)
        {
        }
    }
}
