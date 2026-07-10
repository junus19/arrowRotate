using System;
using System.IO;
using UnityEngine;
// using Newtonsoft.Json;
using Debug = UnityEngine.Debug;

namespace GameBrain.Casual
{
    public class BaseGameData<T> : ScriptableObject
    {
        public string FileName;
        public T Data;

        private void OnEnable()
        {
            if (string.IsNullOrEmpty(FileName))
            {
                FileName = name + ".json";
                Data = (T)Activator.CreateInstance(typeof(T), new object[] { });
                SaveData();
            }
            else
            {
                LoadData();
            }

            Debug.Log("persistentDataPath : " + Application.persistentDataPath);
            Debug.Log($"loaded : {FileName} : {JsonUtility.ToJson(Data)}");
        }

        public void LoadData()
        {
            if (File.Exists(GetFilePath()))
            {
                string jsonText = File.ReadAllText(GetFilePath());
                Data = JsonUtility.FromJson<T>(jsonText);
            }
            else
            {
                Data = (T)Activator.CreateInstance(typeof(T), new object[] { });
            }
        }

        public void SaveData()
        {
            File.WriteAllText(GetFilePath(), JsonUtility.ToJson(Data, true));
        }

        public void ClearData()
        {
            if (File.Exists(GetFilePath()))
            {
                File.Delete(GetFilePath());
            }

            Data = (T)Activator.CreateInstance(typeof(T), new object[] { });
        }

        string GetFilePath()
        {
            return Application.persistentDataPath + "/" + FileName;
        }
    }
}
