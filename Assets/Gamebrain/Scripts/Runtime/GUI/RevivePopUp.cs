using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using GameBrain.Utils;
using TMPro;
using UnityEngine.EventSystems;

namespace GameBrain.Casual
{
    public class RevivePopUp : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        [SerializeField] private Button _reviveButton;
        [SerializeField] private Button _closeButton;
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private TMP_Text _priceText;
        private GameData _gameData;

        public void SetActive(bool active) => gameObject.SetActive(active);
        
        private void OnEnable()
        {
            _reviveButton.onClick.AddListener(OnReviveButtonClicked);
            _closeButton.onClick.AddListener(OnCloseButtonClicked);
            _priceText.text = _gameData.RevivePrice.ToString();
        }

        private void OnDisable()
        {
            _reviveButton.onClick.RemoveListener(OnReviveButtonClicked);
            _closeButton.onClick.RemoveListener(OnCloseButtonClicked);
        }

        public void OnInject(object[] args)
        {
            _gameData = args[0] as GameData;
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
