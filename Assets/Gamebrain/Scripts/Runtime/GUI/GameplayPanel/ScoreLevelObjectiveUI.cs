using TMPro;
using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;

namespace GameBrain.Casual
{
    public class ScoreLevelObjectiveUI : BaseLevelObjectiveUI
    {
        [SerializeField] private Image _icon;
        [SerializeField] private Image _scoreFill;
        [SerializeField] private TMP_Text _scoreText;

        public GameObject ScoreMask;

        public Transform IconTransform => _icon.transform;
        public Vector3 IconPosition => _icon.transform.position;

        public override void Init(LevelGoalType _levelGoalType, Sprite iconSprite, int intialObjectivePoint, bool isDummy)
        {
            levelGoalType = _levelGoalType;
            _icon.sprite = iconSprite;

            IconTransform.DOKill();
            IconTransform.localScale = Vector3.one;

            UpdateObjective(0, intialObjectivePoint, false, default, default);
            UnSubscribeEvents();
            SubscribeEvents();
        }

        public override void UpdateObjective(int currentScore, int totalScore, bool showFlyingIndicator, Vector3 objectPosition, Color color)
        {
            Vector2 pos;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(GetComponent<RectTransform>(), Camera.main.WorldToScreenPoint(objectPosition), null, out pos);

            var dummyObj = Instantiate(dummy, transform);
            dummyObj.GetComponent<RectTransform>().anchoredPosition = pos;
            Color trailColor = color;
            trailColor.a = 0.5f;
            color.a = 0.95f;
            showFlyingIndicator = false;
            if (showFlyingIndicator)
                LaunchParticle(dummyObj, flyTarget, currentScore, totalScore, color, trailColor);
            else

                UpdateGoalTexts(currentScore, totalScore);
        }

        protected override void OnFlyEnd(int currentScore, int totalScore, GameObject flyingIndicator)
        {
            base.OnFlyEnd(currentScore, totalScore, flyingIndicator);

            IconTransform.DOKill();
            IconTransform.localScale = Vector3.one;
            IconTransform.DOScale(Vector3.one * 1.15f, 0.1f).SetLoops(2, LoopType.Yoyo).SetEase(Ease.Linear);
            _scoreText.text = Mathf.Min(currentScore, totalScore) + "/" + totalScore;

            if (currentScore >= totalScore)
            {
                _scoreFill.fillAmount = 1.0f;
            }
            else
            {
                float fill = currentScore / (float)totalScore;
                _scoreFill.fillAmount = fill;
            }
        }

        protected override void UpdateWithoutFly(int currentScore, int totalScore)
        {
            base.UpdateWithoutFly(currentScore, totalScore);
            OnFlyEnd(currentScore, totalScore, null);    
        }
        
        public override void SetVisual()
        {
            base.SetVisual();

            ScoreMask.transform.DOScaleX(0, 0.3f);
        }

        protected override void UpdateGoalTexts(int currentScore, int totalScore)
        {
            base.UpdateGoalTexts(currentScore, totalScore);

            _scoreText.text = Mathf.Min(currentScore, totalScore) + "/" + totalScore;

            if (currentScore >= totalScore)
            {
                _scoreFill.fillAmount = 1.0f;
            }
            else
            {
                float fill = currentScore / (float)totalScore;
                _scoreFill.fillAmount = fill;
            }
        }
    }
}
