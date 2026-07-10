using System.Collections.Generic;
using UnityEngine;
using Casual = GameBrain.Casual;

namespace GameBrain.Store
{
    /// <summary>
    /// Maps currencies/boosters to display sprites and extracts (icon, amount) from a reward. This is the
    /// single UI place that knows concrete reward types — extend <see cref="TryResolve"/> when you add a
    /// new <see cref="IShopReward"/>.
    /// </summary>
    [CreateAssetMenu(fileName = "Store Reward Icons", menuName = "GameBrain/Store/Reward Icon Set")]
    public sealed class StoreRewardIconSet : ScriptableObject
    {
        [System.Serializable]
        private struct BoosterIcon
        {
            public Casual.BoosterType Type;
            public Sprite Sprite;
        }

        [Header("Currency")]
        [SerializeField] private Sprite _coinIcon;
        [SerializeField] private Sprite _gemIcon;

        [Header("Boosters")]
        [SerializeField] private List<BoosterIcon> _boosterIcons = new List<BoosterIcon>();

        public Sprite GetCurrencyIcon(CurrencyType currency)
        {
            switch (currency)
            {
                case CurrencyType.Coin: return _coinIcon;
                case CurrencyType.Gem: return _gemIcon;
                default: return null;
            }
        }

        public Sprite GetBoosterIcon(Casual.BoosterType type)
        {
            for (int i = 0; i < _boosterIcons.Count; i++)
            {
                if (_boosterIcons[i].Type == type) return _boosterIcons[i].Sprite;
            }
            return null;
        }

        /// <summary>Resolve a reward into a display icon and amount. Returns false for unknown reward types.</summary>
        public bool TryResolve(IShopReward reward, out Sprite icon, out int amount)
        {
            switch (reward)
            {
                case CurrencyReward currency:
                    icon = GetCurrencyIcon(currency.Currency);
                    amount = currency.Amount;
                    return true;
                case BoosterReward booster:
                    icon = GetBoosterIcon(booster.BoosterType);
                    amount = booster.Amount;
                    return true;
                default:
                    icon = null;
                    amount = 0;
                    return false;
            }
        }
    }
}
