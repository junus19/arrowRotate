using System.Collections.Generic;
using UnityEngine;
using TMPro;
namespace GameBrain.Casual
{
    public class StageUI : MonoBehaviour
    {
        [SerializeField] Transform stageUIItemsParent;
        [SerializeField] StageUIItem stageUIItemPrefab;
        [SerializeField] List<StageUIItem> stageUIItems;    

        [SerializeField] StageUIItem activeStageUIItem;

        [SerializeField] TextMeshProUGUI txt;

        public void Init(int tutorialCount)
        {
            stageUIItems = new();
            for(int i=0; i < tutorialCount; i++)
            {
                var item = Instantiate(stageUIItemPrefab, stageUIItemsParent);
                item.Deactivate();
                stageUIItems.Add(item);
            }

            txt.gameObject.SetActive(true);
        }

        public void Activate(int tutotialIndex)
        {
            if(activeStageUIItem != null)
            {
                activeStageUIItem.Complete();
                // activeStageUIItem.Deactivate();
            }

            activeStageUIItem = stageUIItems[tutotialIndex];
            activeStageUIItem.Activate();
        }   
    }
}
