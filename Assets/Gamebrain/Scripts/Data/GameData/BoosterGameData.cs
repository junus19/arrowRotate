using UnityEngine;
using System.Collections.Generic;

namespace GameBrain.Casual
{
    [System.Serializable]
    public class BoosterGameDataItem
    {
        public BoosterType _BoosterType;
        public bool IsActive;
        public int BoosterCount;

        public BoosterGameDataItem(BoosterType boosterType, bool isActive, int boosterCount)
        {
            _BoosterType = boosterType;
            IsActive = isActive;
            BoosterCount = boosterCount;
        }

        public void AddBooster(int amount)
        {
            BoosterCount += amount;
        }

        public void UseBooster(int amount)
        {
            BoosterCount -= amount;
            if (BoosterCount < 0)
                BoosterCount = 0;
        }
    }

    [System.Serializable]
    public class BoosterGameDataModel
    {
        public List<BoosterGameDataItem> BoosterGameDataItems = new List<BoosterGameDataItem>();

        public BoosterGameDataItem GetBoosterGameDataItem(BoosterType boosterType)
        {
            return BoosterGameDataItems.Find(x => x._BoosterType == boosterType);
        }
    }

    [CreateAssetMenu(menuName = "Scriptable Objects/Game System/Booster Data SO")]
    public class BoosterGameData : BaseGameData<BoosterGameDataModel>
    {
        [SerializeField] int boosterStartCount;

        [SerializeField] int hammerUnlockLevel;
        public int HammerUnlockLevel => hammerUnlockLevel;

        [SerializeField] int refreshUnlockLevel;
        public int RefreshUnlockLevel => refreshUnlockLevel;

        [SerializeField] int swapUnlockLevel;
        public int SwapUnlockLevel => swapUnlockLevel;

        [SerializeField] int pickerUnlockLevel;
        public int PickerUnlockLevel => pickerUnlockLevel;

        public bool IsBoosterActive(BoosterType boosterType, int level)
        {
            switch (boosterType)
            {
                case BoosterType.None:
                    return false;
                case BoosterType.Hammer:
                    return level >= hammerUnlockLevel;
                case BoosterType.Refresh:
                    return level >= refreshUnlockLevel;
                case BoosterType.Swap:
                    return level >= swapUnlockLevel;
                case BoosterType.GroupRemove:
                    return level >= pickerUnlockLevel;
                default:
                    return false;
            }
        }

        public int GetBoosterUnlcokVisualLevel(BoosterType boosterType)
        {
            switch (boosterType)
            {
                case BoosterType.None:
                    return 99;
                case BoosterType.Hammer:
                    return hammerUnlockLevel + 1;
                case BoosterType.Refresh:
                    return refreshUnlockLevel + 1;
                case BoosterType.Swap:
                    return swapUnlockLevel + 1;
                case BoosterType.GroupRemove:
                    return pickerUnlockLevel + 1;
                default:
                    return 99;
            }
        }


        public void AddBoosterData(BoosterType boosterType)
        {
            if (Data.GetBoosterGameDataItem(boosterType) == null)
                Data.BoosterGameDataItems.Add(new BoosterGameDataItem(boosterType, false, boosterStartCount));

            SaveData();
        }

        public void AddBooster(BoosterType boosterType, int amount)
        {
            BoosterGameDataItem boosterGameDataItem = Data.GetBoosterGameDataItem(boosterType);
            if (boosterGameDataItem != null)
                boosterGameDataItem.AddBooster(amount);

            SaveData();
        }

        public void UseBooster(BoosterType boosterType, int amount)
        {
            BoosterGameDataItem boosterGameDataItem = Data.GetBoosterGameDataItem(boosterType);
            if (boosterGameDataItem != null)
                boosterGameDataItem.UseBooster(amount);

            SaveData();
        }

        public int GetBoosterCount(BoosterType boosterType)
        {
            BoosterGameDataItem boosterGameDataItem = Data.GetBoosterGameDataItem(boosterType);
            if (boosterGameDataItem != null)
                return boosterGameDataItem.BoosterCount;
            return 0;
        }
    }
}
