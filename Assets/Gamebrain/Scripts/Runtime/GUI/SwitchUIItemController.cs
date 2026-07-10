using UnityEngine;

namespace GameBrain.Casual
{
    [RequireComponent(typeof(SwitchUIItem))]
    public abstract class SwitchUIItemController : MonoBehaviour
    {
        [SerializeField] SwitchUIItem switchUIItem;
        protected SwitchUIItem SwitchUIItem => switchUIItem;

        protected virtual void Awake()
        {
        }

        public virtual void Init(bool status)
        {
            if (switchUIItem == null)
                switchUIItem = GetComponent<SwitchUIItem>();

            switchUIItem.SwitchButton.onClick.AddListener(OnSwitchHandleButtonClicked);

            switchUIItem.Init(status);
        }

        protected abstract void OnSwitchHandleButtonClicked();
    }
}
