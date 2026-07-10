using UnityEngine;
using GameBrain.Utils;

namespace GameBrain.Casual
{
    public enum LevelGoalType
    {
        None,
        Score,
        Wood,
        Ice,
        Clay
    }

    [System.Serializable]
    public class LevelGoal
    {
        [SerializeField] protected bool isAchieved;
        public bool IsAchieved => isAchieved;

        [SerializeField] LevelGoalType goalType;
        public LevelGoalType GoalType => goalType;

        [SerializeField] int totalGoal;
        public int TotalGoal => totalGoal;

        [SerializeField] protected int remainingGoal;
        public int RemainingGoal => remainingGoal;

        [SerializeField] protected int currentGoal;
        public int CurrentGoal => currentGoal;

        public LevelGoal(LevelGoalType _goalType, int _goal)
        {
            goalType = _goalType;
            totalGoal = _goal;
        }

        public LevelGoal(LevelGoalDataItem levelGoalDataItem)
        {
            goalType = levelGoalDataItem.GoalType;
            totalGoal = levelGoalDataItem.Amount;
        }

        public virtual void UpdateCurrentGoal(int amount, Vector3 pos, Color color)
        {

        }

        protected void OnLevelGoalUpdated(Vector3 pos, Color color)
        {
            EventBus<LevelGoalUpdatedEvent>.Raise(new LevelGoalUpdatedEvent(goalType, currentGoal, totalGoal, remainingGoal, pos, color));

        }

        protected void MarkAchieved()
        {
            isAchieved = true;
            EventBus<LevelGoalAchievedEvent>.Raise(new LevelGoalAchievedEvent(goalType));
        }
    }

    [System.Serializable]
    public class LevelGoalIncrease : LevelGoal
    {

        public LevelGoalIncrease(LevelGoalType _goalType, int _goal) : base(_goalType, _goal)
        {

        }

        public override void UpdateCurrentGoal(int amount, Vector3 pos, Color color)
        {
            if (isAchieved)
                return;

            currentGoal += amount;
            remainingGoal = TotalGoal - currentGoal;

            OnLevelGoalUpdated(pos, color);
            if (currentGoal >= TotalGoal)
                MarkAchieved();
        }
    }

    [System.Serializable]
    public class LevelGoalDecrease : LevelGoal
    {
        public LevelGoalDecrease(LevelGoalType _goalType, int _goal) : base(_goalType, _goal)
        {
            currentGoal = TotalGoal;
        }

        public override void UpdateCurrentGoal(int amount, Vector3 pos, Color color)
        {
            if (isAchieved)
                return;

            currentGoal -= amount;
            remainingGoal = currentGoal;

            OnLevelGoalUpdated(pos, color);
            if (currentGoal <= 0)
                MarkAchieved();
        }
    }

    [System.Serializable]
    public class LevelGoalDirectSet : LevelGoal
    {
        public LevelGoalDirectSet(LevelGoalType _goalType, int _goal) : base(_goalType, _goal)
        {

        }

        public override void UpdateCurrentGoal(int amount, Vector3 pos, Color color)
        {
            if (isAchieved)
                return;

            currentGoal = amount;
            remainingGoal = TotalGoal - currentGoal;

            OnLevelGoalUpdated(pos, color);
            if (currentGoal >= TotalGoal)
                MarkAchieved();
        }
    }


    public class LevelGoalUpdatedEvent : IEvent
    {
        public LevelGoalType LevelGoalType;

        public int CurrentGoal;
        public int TotalGoal;
        public int RemainingGoal;
        public Vector3 Position;
        public Color Color;

        public LevelGoalUpdatedEvent(LevelGoalType levelGoalType, int currentGoal, int totalGoal, int remainingGoal, Vector3 pos, Color color)
        {
            LevelGoalType = levelGoalType;
            CurrentGoal = currentGoal;
            TotalGoal = totalGoal;
            RemainingGoal = remainingGoal;
            Position = pos;
            Color = color;
        }
    }

    public class LevelGoalAchievedEvent : IEvent
    {
        public LevelGoalType LevelGoalType;

        public LevelGoalAchievedEvent(LevelGoalType levelGoalType)
        {
            LevelGoalType = levelGoalType;
        }
    }
}
