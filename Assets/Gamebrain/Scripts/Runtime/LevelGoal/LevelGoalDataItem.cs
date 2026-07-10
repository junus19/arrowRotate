using UnityEngine;

namespace GameBrain.Casual
{
    [System.Serializable]
    public class LevelGoalDataItem
    {
        [SerializeField] private LevelGoalType _goalType;
        public LevelGoalType GoalType => _goalType;

        [SerializeField] private int _amount;
        public int Amount => _amount;
    }
}
