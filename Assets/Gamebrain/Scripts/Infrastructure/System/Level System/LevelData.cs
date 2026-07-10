using UnityEngine;
using System.Collections.Generic;
using Object = UnityEngine.Object;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace GameBrain.Casual
{
    [CreateAssetMenu(menuName = "GameBrain/Level Data", fileName = "New Level Data", order = 0)]
    public class LevelData : ScriptableObject
    {
        [SerializeField] protected Object _scene;
        [SerializeField, SceneDropdown, HideInInspector] protected string _sceneName;
        [SerializeField] protected LevelDifficulty _difficulty = LevelDifficulty.Easy;

        [Header("Config"), Space(5)]
        [SerializeField, HideInInspector] protected List<LevelObjective> _objectives = new List<LevelObjective>();
        [SerializeField, HideInInspector] protected List<LevelGoalDataItem> _goals = new List<LevelGoalDataItem>();

        public Object Scene => _scene;
        public string SceneName => _sceneName;
        public LevelDifficulty Difficulty => _difficulty;
        public List<LevelObjective> Objectives => _objectives;
        public List<LevelGoalDataItem> Goals => _goals;

        private void OnValidate()
        {
            _sceneName = _scene != null ? _scene.name : string.Empty;
        }
    }

    public enum LevelDifficulty
    {
        Beginner,
        Easy,
        Medium,
        Hard,
        VeryHard,
        Extreme,
        Insane
    }
}
