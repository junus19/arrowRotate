using UnityEngine;

namespace GameBrain.Casual
{
    public enum BoosterType
    {
        None,
        Hammer,
        Refresh,
        Swap,
        GroupRemove
    }

    [CreateAssetMenu(fileName = "BoosterItemData", menuName = "GameBrain/BoosterItemData")]
    public class BoosterItemData : ScriptableObject
    {
        [SerializeField] BoosterType boosterType;
        public BoosterType BoosterType => boosterType;

        [SerializeField] Sprite icon;
        public Sprite Icon => icon;

        [SerializeField] Sprite iconDisabled;
        public Sprite IconDisabled => iconDisabled;

        [SerializeField] BaseBooster boosterPrefab;
        public BaseBooster BoosterPrefab => boosterPrefab;

        [SerializeField] string header;
        public string Header => header;

        [SerializeField] string info;
        public string Info => info;
    }
}
