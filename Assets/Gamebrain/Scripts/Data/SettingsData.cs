using UnityEngine;

namespace GameBrain.Casual
{
    [System.Serializable]
    public class SettingsDataModel
    {
        // #if UNITY_IOS
        public bool IsHapticEnabled = true;
        // #else
        // public bool IsHapticEnabled = false;
        // #endif

        public bool IsAudioFxEnabled = true;
        public bool IsMusicEnabled = true;
    }

    [CreateAssetMenu(menuName = "GameBrain/SaveData/Settings Data SO")]
    public class SettingsData : BaseGameData<SettingsDataModel>
    {
        public bool GetHapticStatus()
        {
            return Data.IsHapticEnabled;
        }

        public void SetHapticStatus()
        {
            Data.IsHapticEnabled = !Data.IsHapticEnabled;
            SaveData();
        }

        public void SetAudioFxStatus()
        {
            Data.IsAudioFxEnabled = !Data.IsAudioFxEnabled;
            SaveData();
        }

        public bool GetAudioFxStatus()
        {
            return Data.IsAudioFxEnabled;
        }

        public void SetMusicStatus()
        {
            Data.IsMusicEnabled = !Data.IsMusicEnabled;
            SaveData();
        }

        public bool GetMusicStatus()
        {
            return Data.IsMusicEnabled;
        }
    }
}
