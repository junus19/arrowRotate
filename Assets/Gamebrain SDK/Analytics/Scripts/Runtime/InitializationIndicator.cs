using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GameBrain.SDK
{
    public class InitializationIndicator : MonoBehaviour
    {
        [SerializeField] private Slider _slider;
        [SerializeField] private TMP_Text _progressText;

        public void SetProgress(int percent)
        {
            _slider.value = (float)percent / 100f;
            _progressText.text = "Loading..."+" "+percent.ToString() + "%";
        }
    }
}
