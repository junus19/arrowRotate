using UnityEngine;
using UnityEngine.UI;

namespace GameBrain.Casual
{
    public class SettingsItemButton : MonoBehaviour
    {
        [SerializeField] Button button;
        [SerializeField] Image buttonIcon;
        [SerializeField] Sprite activeIcon;
        [SerializeField] Sprite deactiveIcon;

        [SerializeField] Color disabledColor;

        public void Init()
        {
        }

        private void SetStatus(bool status)
        {
            if (status)
            {
                if (activeIcon != null)
                    buttonIcon.sprite = activeIcon;
                else
                    buttonIcon.color = Color.white;
            }
            else
            {
                if (deactiveIcon != null)
                    buttonIcon.sprite = deactiveIcon;
                else
                    buttonIcon.color = disabledColor;
            }
        }
    }
}
