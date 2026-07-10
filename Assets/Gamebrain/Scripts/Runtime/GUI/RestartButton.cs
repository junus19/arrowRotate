using UnityEngine;
using UnityEngine.UI;
using GameBrain.Utils;

namespace GameBrain.Casual
{
    public class RestartButton : MonoBehaviour
    {
        [SerializeField] Button Button;

        private void Awake()
        {
            Button.onClick.AddListener(OnButtonClicked);
        }

        private void OnButtonClicked()
        {
            EventBus<RestartRequestedEvent>.Raise(new RestartRequestedEvent());
        }
    }
}
