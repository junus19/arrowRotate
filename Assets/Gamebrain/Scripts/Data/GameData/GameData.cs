using UnityEngine;

namespace GameBrain.Casual
{
    [System.Serializable]
    public class GameDataModel
    {
        [Header("Level")]
        public int Level;

        public int RealLevel;
        public int SelectedRandomLevel = -1;
        public int LastCompletedRandomLevel = -1;

        public int hexInBank = 0;
        public int currentObjectIndex = 0;
        public int collectedHexInObject = 0;

        public bool IsOnboardingComplete = false;

        public int Coin;

        public int StartCoin;
        //public bool minOne = false;
    }

    [CreateAssetMenu(menuName = "Scriptable Objects/Game System/Game Data SO")]
    public class GameData : BaseGameData<GameDataModel>
    {
        public int InstantStartLevelWithoutMainMenu = 3; // Main menu will be introduced after this level!
        public int RandomLevelLoopStartIndex;
        public int interstatialShowLevel;
        public int reviewRequestLevel;

        public int GetLevelIndex()
        {
            return Data.Level;
        }

        public int GetAnalyticLevelIndex()
        {
            return Data.Level + 1;
        }

        public int GetRealLevelIndex()
        {
            return Data.RealLevel;
        }

        public int GetVisualLevelIndex()
        {
            return Data.Level + 1;
        }
        
        public void LevelComplete()
        {
            Data.Level++;
            Data.LastCompletedRandomLevel = Data.SelectedRandomLevel;
            Data.SelectedRandomLevel = -1;
            SaveData();
        }

        public void RealLevelComplete()
        {
            Data.Level++;
            Data.RealLevel++;
            Data.LastCompletedRandomLevel = Data.SelectedRandomLevel;
            Data.SelectedRandomLevel = -1;
            SaveData();
        }

        public void SetSelectedRandomLevel(int randomLevel)
        {
            Data.SelectedRandomLevel = randomLevel;
            SaveData();
        }

        public bool IsInstantStartLevel()
        {
            return Data.Level <= InstantStartLevelWithoutMainMenu;
        }

        public bool CanShowInter()
        {
            return Data.Level >= interstatialShowLevel;
        }

        public int GetCoinAmount()
        {
            return Data.Coin;
        }

        public int AddCoin(int amount)
        {
            Data.Coin += amount;
            SaveData();
            return Data.Coin;
        }

        public int DebitCoin(int amount)
        {
            Data.Coin -= amount;

            if (Data.Coin < 0)
                Data.Coin = 0;

            SaveData();

            return Data.Coin;
        }
    }
}
