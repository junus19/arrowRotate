using TMPro;
using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;

namespace GameBrain.Casual
{
    [DisallowMultipleComponent]
    public class UIPopup : MonoBehaviour
    {
        [SerializeField] protected Button _closeButton;
        [SerializeField] protected TMP_Text _titleText;

        protected virtual void OnEnable()
        {
            _closeButton.onClick.AddListener(OnCloseButtonClicked);
        }

        protected virtual void OnDisable()
        {
            _closeButton.onClick.RemoveListener(OnCloseButtonClicked);
        }

        private void OnCloseButtonClicked()
        {
            Close();
        }

        [ContextMenu("Open")]
        public virtual void Open()
        {
            transform.DOKill();
            transform.localScale = Vector3.zero;
            transform.DOScale(1f, .5f).SetEase(Ease.OutBack);
            gameObject.SetActive(true);
        }

        [ContextMenu("Close")]
        public virtual void Close()
        {
            transform.DOKill();
            transform.DOScale(0f, .5f).SetEase(Ease.InBack).OnComplete(()=> gameObject.SetActive(false));
        }

        public void SetTitle(string title) => _titleText.text = title;
        
        protected virtual void OnDestroy()
        {
            transform.DOKill();
        }
    }
}
