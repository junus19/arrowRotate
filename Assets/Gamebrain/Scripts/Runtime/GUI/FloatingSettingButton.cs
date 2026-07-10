using UnityEngine;
using UnityEngine.UI;
using GameBrain.Utils;

namespace GameBrain.Casual
{
    public class FloatingSettingButton : MonoBehaviour
    {
        public bool isInitted = false;
        [SerializeField] SettingType settingType;
        [SerializeField] Button button;
        [SerializeField] GameObject settingOffObject;

        private EventBinding<SettingIsChangedEvent> settingIsChangedEvent;

        private void Awake()
        {
            button.onClick.AddListener(OnButtonClicked);
        }

        public void Init(bool status)
        {
            if (isInitted)
                return;

            isInitted = true;

            UpdateVisual(status);
            settingIsChangedEvent = new EventBinding<SettingIsChangedEvent>(OnSettingChanged);
            EventBus<SettingIsChangedEvent>.Register(settingIsChangedEvent);
        }

        [ContextMenu("aaaa")]
        public void OnButtonClicked()
        {
            Debug.LogWarning("settings change request : " + settingType);

            EventBus<SettingChangeRequestEvent>.Raise(new SettingChangeRequestEvent(settingType));
            EventBus<FxRequestEvent>.Raise(new FxRequestEvent(EffectType.Button));
        }

        private void OnSettingChanged(SettingIsChangedEvent eventInfo)
        {
            name = Random.Range(0, 100000).ToString();
            Debug.LogWarning("settings changed : " + eventInfo.SettingType + " : " + name);
            if (settingType == eventInfo.SettingType)
            {
                UpdateVisual(eventInfo.Status);
            }
        }

        private void UpdateVisual(bool status)
        {
            settingOffObject.SetActive(!status);
        }
    }
}
