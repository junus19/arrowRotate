using System;
using UnityEngine;

namespace GameBrain.Store
{
    /// <summary>Grants soft/hard virtual currency (coins, gems).</summary>
    [Serializable]
    public sealed class CurrencyReward : IShopReward
    {
        [SerializeField] private CurrencyType _currency;
        [SerializeField] private int _amount;

        public CurrencyType Currency => _currency;
        public int Amount => _amount;

        public CurrencyReward() { }

        public CurrencyReward(CurrencyType currency, int amount)
        {
            _currency = currency;
            _amount = amount;
        }
    }

    /// <summary>Deposits a <see cref="CurrencyReward"/> into the wallet.</summary>
    public sealed class CurrencyRewardGranter : IRewardGranter
    {
        private readonly IWallet _wallet;

        public Type RewardType => typeof(CurrencyReward);

        public CurrencyRewardGranter(IWallet wallet) => _wallet = wallet;

        public void Grant(IShopReward reward)
        {
            CurrencyReward currencyReward = (CurrencyReward)reward;
            _wallet.Deposit(currencyReward.Currency, currencyReward.Amount);
        }
    }
}
