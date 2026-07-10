using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;
using GameBrain.Utils;
using AssetKits.ParticleImage;

namespace GameBrain.Casual
{
    public abstract class BaseLevelObjectiveUI : MonoBehaviour
    {
        [SerializeField] protected LevelGoalType levelGoalType;
        public  LevelGoalType LevelGoalType => levelGoalType;

        [SerializeField] protected bool dontFly;
        [SerializeField] protected ParticleImage flyingIndicatorParticle;

        [SerializeField] protected Image flyingObjectiveIndicator;
        [SerializeField] protected Sprite flyingObjectiveSprite;

        [SerializeField] RectTransform rectTransform;
        public RectTransform RectTransform => rectTransform;

        [SerializeField] protected CanvasGroup canvasGroup;
        public CanvasGroup CanvasGroup => canvasGroup;

        private EventBinding<LevelGoalUpdatedEvent> levelGoalUpdatedEventBinding;
        private EventBinding<LevelGoalAchievedEvent> levelGoalAchievedEventBinding;


        public abstract void Init(LevelGoalType _levelGoalType, Sprite icon, int intialObjectivePoint, bool isDummy);
        public abstract void UpdateObjective(int current, int total, bool showFlyingIndicator, Vector3 objectPosition, Color color);

        private Sequence flyingSequence;
        public Transform dummy;

        public RectTransform flyTarget;
        [Header("Temp Colors")]
        public Color WoodColor;
        public Color IceColor;
        public Color ScoreColor;
        public Color ClayColor;


        public Texture2D a;

        protected void SubscribeEvents()
        {
            levelGoalUpdatedEventBinding = new EventBinding<LevelGoalUpdatedEvent>(LevelGoalUpdated);
            EventBus<LevelGoalUpdatedEvent>.Register(levelGoalUpdatedEventBinding);

            levelGoalAchievedEventBinding = new EventBinding<LevelGoalAchievedEvent>(LevelGoalAchievedEvent);
            EventBus<LevelGoalAchievedEvent>.Register(levelGoalAchievedEventBinding);
        }

        public void UnSubscribeEvents()
        {
            EventBus<LevelGoalUpdatedEvent>.Deregister(levelGoalUpdatedEventBinding);
            EventBus<LevelGoalAchievedEvent>.Deregister(levelGoalAchievedEventBinding);
        }

        protected virtual Color GetColor()
        {
            switch (levelGoalType)
            {
                case LevelGoalType.None:
                    return ScoreColor;
                case LevelGoalType.Score:
                    return ScoreColor;
                case LevelGoalType.Wood:
                    return WoodColor;
                case LevelGoalType.Ice:
                    return IceColor;
                case LevelGoalType.Clay:
                    return ClayColor;

                default:
                    return ScoreColor;
            };
        }
        private void LevelGoalUpdated(LevelGoalUpdatedEvent eventInfo)
        {
            if (eventInfo.LevelGoalType == levelGoalType)
            {
                if(dontFly)
                    UpdateWithoutFly(eventInfo.CurrentGoal, eventInfo.TotalGoal);
                else
                    UpdateObjective(eventInfo.CurrentGoal, eventInfo.TotalGoal, true, eventInfo.Position, eventInfo.Color);
            }
        }

        protected virtual void LevelGoalAchievedEvent(LevelGoalAchievedEvent eventInfo)
        {

        }

        public virtual void SetVisual()
        {

        }

        protected virtual void UpdateGoalTexts(int current, int total)
        {

        }

        protected virtual void LaunchParticle(Transform start, Transform end, int current, int total, Color  particleColor, Color trailColor)
        {
            var particle = Instantiate(flyingIndicatorParticle, transform);
            particle.emitterConstraintTransform = start;
            particle.attractorTarget = end;
            particle.colorOverLifetime = particleColor;
            particle.trailColorOverTrail = trailColor;
            particle.texture = flyingObjectiveSprite.texture;
            particle.onFirstParticleFinish.AddListener(() => OnFlyEnd(current, total, null));

        }
        protected void LaunchFlyingIndicator(Vector3 launchPos, Vector3 targetPos, int current, int total)
        {
            var flyingIndicator = Instantiate(flyingObjectiveIndicator, transform);
            flyingIndicator.sprite = flyingObjectiveSprite;

            Vector2 pos;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(GetComponent<RectTransform>(), Camera.main.WorldToScreenPoint(launchPos), null, out pos);

            flyingIndicator.rectTransform.anchoredPosition = pos;
            flyingIndicator.transform.localScale = Vector3.zero;

            flyingSequence = DOTween.Sequence();
            flyingSequence.Append(flyingIndicator.rectTransform.DOScale(Vector3.one * 1.2f, 0.15f));
            flyingSequence.PrependInterval(0.2f);
            flyingSequence.Append(transform.DOScale(Vector3.one, 0.15f).SetDelay(0.15f));
            flyingSequence.Join(flyingIndicator.rectTransform.DOMove(targetPos, 0.6f));
            flyingSequence.Join(flyingIndicator.rectTransform.DOScale(Vector3.zero, 0.20f).SetDelay(0.4f).OnComplete(() => OnFlyEnd(current, total, flyingIndicator.gameObject)));

        }

        protected virtual void OnFlyEnd(int current, int total, GameObject flyingIndicator)
        {
            if(flyingIndicator != null)
                Destroy(flyingIndicator);
        }

        protected virtual void UpdateWithoutFly(int currentScore, int totalScore)
        {
            
        }

        private void OnDestroy()
        {
            UnSubscribeEvents();
        }
    }
}
