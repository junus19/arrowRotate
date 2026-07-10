using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace GameBrain.Casual
{
    public class SwitchUIItem : MonoBehaviour
    {
        [SerializeField] Button switchButton;
        public Button SwitchButton => switchButton;

        [SerializeField] Image handleBg;
        [SerializeField] Image switchBg;

        [SerializeField] Sprite handleOnSprite;
        [SerializeField] Sprite handleOffSprite;

        [SerializeField] Sprite switchBgOnSprite;
        [SerializeField] Sprite switchBgOffSprite;

        [SerializeField] Transform handleOnPoint;
        [SerializeField] Transform handleOffPoint;

        protected virtual void Awake()
        {
            switchButton.onClick.AddListener(OnSwitchHandleButtonClicked);
        }

        public void Init(bool status)
        {
            ChangeStatus(status, false);
        }

        protected virtual void OnSwitchHandleButtonClicked()
        {
        }

        public void ChangeStatus(bool status, bool withAnim = false)
        {
            handleBg.sprite = status ? handleOnSprite : handleOffSprite;
            switchBg.sprite = status ? switchBgOnSprite : switchBgOffSprite;
            Vector3 handleTarget = status ? handleOnPoint.localPosition : handleOffPoint.localPosition;

            handleBg.transform.DOKill();
            if (withAnim)
                handleBg.transform.DOLocalMove(handleTarget, 0.3f).SetEase(Ease.OutCubic);
            else
                handleBg.transform.localPosition = handleTarget;

            // Debug.Log("handle target : " + handleTarget);
        }
    }
}
