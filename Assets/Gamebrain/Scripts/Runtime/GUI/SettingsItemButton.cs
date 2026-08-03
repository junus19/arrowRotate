using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

namespace GameBrain.Casual
{
    public class SettingsItemButton : MonoBehaviour
    {
        [SerializeField] Button button;
        [SerializeField] Image buttonIcon;
        [SerializeField] Sprite activeIcon;
        [SerializeField] Sprite deactiveIcon;
        [SerializeField] GameObject deactiveObject;

        [SerializeField] Color disabledColor;

        public void Init(UnityAction action)
        {
            button.onClick.AddListener(action);
        }

        private void SetStatus(bool status)
        {
            if (status)
            {
                if (activeIcon != null)
                    buttonIcon.sprite = activeIcon;
                else
                    buttonIcon.color = Color.white;

                deactiveObject.SetActive(true);
            }
            else
            {
                if (deactiveIcon != null)
                    buttonIcon.sprite = deactiveIcon;
                else
                    buttonIcon.color = disabledColor;

                deactiveObject.SetActive(false);

            }
        }
    }
}
