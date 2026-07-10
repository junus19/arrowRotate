using UnityEngine;
using UnityEngine.UI;
using GameBrain.Utils;

namespace GameBrain.Casual
{
    public class HomeButton : MonoBehaviour
    {
        [SerializeField] Button Button;

        private void Awake()
        {
            Button.onClick.AddListener(OnButtonClicked);
        }

        private void OnButtonClicked()
        {
            EventBus<MainMenuRequestedEvent>.Raise(new MainMenuRequestedEvent());
        }
    }
}
