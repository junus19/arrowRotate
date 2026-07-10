using TMPro;
using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;

namespace GameBrain.Casual
{
    public class BoardObjectLevelObjectiveUI : BaseLevelObjectiveUI
    {
        [SerializeField] private Image _icon;
        [SerializeField] private TMP_Text objectiveCountText;

        [SerializeField] private GameObject completeObject;
        [SerializeField] private bool isAchieved;
        public override void Init(LevelGoalType _levelGoalType, Sprite iconSprite, int intialObjectivePoint, bool isDummy)
        {
            levelGoalType = _levelGoalType;
            _icon.sprite = iconSprite;
            flyingObjectiveSprite = iconSprite;

            transform.DOKill();
            transform.localScale = Vector3.one;
            UnSubscribeEvents();

            UpdateObjective(intialObjectivePoint, intialObjectivePoint, false, default, default);

            if (!isDummy)
                SubscribeEvents();
        }
        
        public override void UpdateObjective(int current, int total, bool showFlyingIndicator, Vector3 objectPosition, Color color)
        {
            //LaunchFlyingIndicator(objectPosition, _icon.transform.position, current, total);
            Vector2 pos;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(GetComponent<RectTransform>(), Camera.main.WorldToScreenPoint(objectPosition), CameraManager.Instance.GameplayUICamera, out pos);

            var dummyObj = Instantiate(dummy, transform);
            dummyObj.GetComponent<RectTransform>().anchoredPosition = pos;
            if (showFlyingIndicator)
                LaunchParticle(dummyObj, flyTarget, current, total, Color.white, GetColor());
            else
                UpdateGoalTexts(current, total);

        }

        protected override void OnFlyEnd(int current, int total, GameObject flyingIndicator)
        {
            base.OnFlyEnd(current, total, flyingIndicator);

            transform.DOKill();
            transform.localScale = Vector3.one;
            transform.DOScale(Vector3.one * 1.15f, 0.1f).SetLoops(2, LoopType.Yoyo);

            if(isAchieved)
            {
                SetCompleted();
            }
            else
            {
                if (current < 0)
                    current = 0;

                objectiveCountText.text = current.ToString();
            }

        }

        protected override void UpdateGoalTexts(int current, int total)
        {
            base.UpdateGoalTexts(current, total);

            if (current < 0)
                current = 0;

            objectiveCountText.text = current.ToString();

        }

        protected override void LevelGoalAchievedEvent(LevelGoalAchievedEvent eventInfo)
        {
            base.LevelGoalAchievedEvent(eventInfo);

            if(eventInfo.LevelGoalType == levelGoalType)
            {
                isAchieved = true;
            }
        }

        private void SetCompleted()
        {
            objectiveCountText.gameObject.SetActive(false);
            completeObject.SetActive(true);

        }
    }
}
