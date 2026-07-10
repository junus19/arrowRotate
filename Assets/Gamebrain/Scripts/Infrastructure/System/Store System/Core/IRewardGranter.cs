using System;

namespace GameBrain.Store
{
    /// <summary>
    /// Grants one concrete <see cref="IShopReward"/> type. The shop dispatches each reward to the
    /// granter whose <see cref="RewardType"/> equals the reward's runtime type.
    /// </summary>
    public interface IRewardGranter
    {
        /// <summary>The concrete reward type this granter handles (e.g. typeof(CurrencyReward)).</summary>
        Type RewardType { get; }

        void Grant(IShopReward reward);
    }
}
