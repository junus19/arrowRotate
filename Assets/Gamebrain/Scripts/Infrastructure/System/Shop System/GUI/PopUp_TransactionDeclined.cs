using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GameBrain.Casual
{
    public class PopUp_TransactionDeclined : UIPopup
    {
        [SerializeField] private TMP_Text _resultText;
        [SerializeField] private Image _iconImage;
        [SerializeField] private Button _okButton;

        public void SetResultText(string result) => _resultText.text = result;

        public void SetIconImage(Sprite result) => _iconImage.sprite = result;
    }
}
