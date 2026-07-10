using System;
using UnityEngine;
using GameBrain.Casual;

namespace GameBrain.Store
{
    /// <summary>Grants a quantity of a gameplay booster.</summary>
    [Serializable]
    public sealed class BoosterReward : IShopReward
    {
        [SerializeField] private BoosterType _boosterType;
        [SerializeField] private int _amount;

        public BoosterType BoosterType => _boosterType;
        public int Amount => _amount;

        public BoosterReward() { }

        public BoosterReward(BoosterType boosterType, int amount)
        {
            _boosterType = boosterType;
            _amount = amount;
        }
    }

    /// <summary>Adds a <see cref="BoosterReward"/> to the player's booster save data.</summary>
    public sealed class BoosterRewardGranter : IRewardGranter
    {
        private readonly BoosterGameData _boosterGameData;

        public Type RewardType => typeof(BoosterReward);

        public BoosterRewardGranter(BoosterGameData boosterGameData) => _boosterGameData = boosterGameData;

        public void Grant(IShopReward reward)
        {
            BoosterReward boosterReward = (BoosterReward)reward;
            _boosterGameData.AddBooster(boosterReward.BoosterType, boosterReward.Amount);
        }
    }
}
