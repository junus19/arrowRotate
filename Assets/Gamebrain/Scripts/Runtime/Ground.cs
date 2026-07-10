using UnityEngine;
using DG.Tweening;
using GameBrain.Utils;

namespace GameBrain.Casual
{    
    public class Ground : MonoBehaviour
    {
        [SerializeField] MeshRenderer meshRenderer;
        [SerializeField] Color defaultColor;
        [SerializeField] Color boosterActiveColor;

        private EventBinding<BoosterActionStartedEvent> boosterActionStartedEventBinding;
        private EventBinding<BoosterActionEndedEvent> boosterActionEndedEventBinding;

        void Awake()
        {
            boosterActionEndedEventBinding = new EventBinding<BoosterActionEndedEvent>(ChangeToDefaultColor);
            EventBus<BoosterActionEndedEvent>.Register(boosterActionEndedEventBinding);

            boosterActionStartedEventBinding = new EventBinding<BoosterActionStartedEvent>(ChangeToBoosterColor);
            EventBus<BoosterActionStartedEvent>.Register(boosterActionStartedEventBinding);
        }

        void OnDestroy()
        {
            EventBus<BoosterActionEndedEvent>.Deregister(boosterActionEndedEventBinding);
            EventBus<BoosterActionStartedEvent>.Deregister(boosterActionStartedEventBinding);
        }
        
        public void ChangeToDefaultColor(BoosterActionEndedEvent eventInfo)
        {
            if(eventInfo.BoosterType == BoosterType.Refresh)
                return;
            meshRenderer.material.DOKill();
            meshRenderer.material.DOColor(defaultColor, .75f);
        }

        public void ChangeToBoosterColor(BoosterActionStartedEvent eventInfo)
        {
            if(eventInfo.BoosterType == BoosterType.Refresh)
                return;
            meshRenderer.material.DOKill();
            meshRenderer.material.DOColor(boosterActiveColor, .75f);
        }
    }
}
