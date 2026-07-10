using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using GameBrain.Utils;
using UnityEngine.EventSystems;

namespace GameBrain.Casual
{
    public class RevivePopUp : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        [SerializeField] private Button _reviveButton;
        [SerializeField] private Button _closeButton;
        [SerializeField] private CanvasGroup _canvasGroup;

        public void SetActive(bool active) => gameObject.SetActive(active);
        
        private void OnEnable()
        {
            _reviveButton.onClick.AddListener(OnReviveButtonClicked);
            _closeButton.onClick.AddListener(OnCloseButtonClicked);
        }

        private void OnDisable()
        {
            _reviveButton.onClick.RemoveListener(OnReviveButtonClicked);
            _closeButton.onClick.RemoveListener(OnCloseButtonClicked);
        }

        private void OnReviveButtonClicked()
        {
            EventBus<ReviveRequestedEvent>.Raise(new ReviveRequestedEvent());
        }
        
        private void OnCloseButtonClicked()
        {
            EventBus<FxRequestEvent>.Raise(new FxRequestEvent(EffectType.Button));
            EventBus<ReviveDeclinedEvent>.Raise(new ReviveDeclinedEvent());
            SetActive(false);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            _canvasGroup.DOFade(0f, .2f);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            _canvasGroup.DOFade(1f, .2f);
        }
    }
}
