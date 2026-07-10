using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GameBrain.Casual
{
    public class BoosterActiveUI : MonoBehaviour
    {
        [SerializeField] GameObject parentObject;
        [SerializeField] Image icon;
        [SerializeField] TextMeshProUGUI boosterHeader;
        [SerializeField] TextMeshProUGUI boosterInfo;

        public void Show(BoosterItemData boosterData)
        {
            parentObject.SetActive(true);
            icon.sprite = boosterData.Icon;
            boosterHeader.text = boosterData.Header;
            boosterInfo.text = boosterData.Info;
        }

        public void Hide()
        {
            parentObject.SetActive(false);
        }
    }
}
