using System;
using UnityEngine;
using TMPro;

namespace GameBrain.Store
{
    /// <summary>
    /// Large multi-reward card (starter pack, special bundle). Extends the base item view with a list of
    /// reward chips (built from the item's rewards), an optional bonus label ("70% EXTRA") and an
    /// optional countdown.
    /// </summary>
    public class StoreBundleView : StoreItemView
    {
        [Header("Bundle")]
        [SerializeField] private Transform _rewardEntriesContainer;
        [SerializeField] private StoreRewardEntryView _rewardEntryPrefab;
        [SerializeField] private TMP_Text _bonusLabelText;
        [SerializeField] private CountdownView _countdown;

        public override void Bind(ShopItemDefinition definition, StoreRewardIconSet iconSet)
        {
            base.Bind(definition, iconSet);
            BuildRewardEntries();
            BindBonus();
            BindCountdown();
        }

        private void BuildRewardEntries()
        {
            if (_rewardEntriesContainer == null || _rewardEntryPrefab == null || _iconSet == null) return;

            for (int i = _rewardEntriesContainer.childCount - 1; i >= 0; i--)
                Destroy(_rewardEntriesContainer.GetChild(i).gameObject);

            var rewards = _definition.Rewards;
            for (int i = 0; i < rewards.Count; i++)
            {
                if (!_iconSet.TryResolve(rewards[i], out Sprite icon, out int amount)) continue;
                StoreRewardEntryView entry = Instantiate(_rewardEntryPrefab, _rewardEntriesContainer);
                entry.Set(icon, amount);
            }
        }

        private void BindBonus()
        {
            if (_bonusLabelText == null) return;
            string bonus = _definition.BonusLabel;
            _bonusLabelText.text = bonus;
            _bonusLabelText.gameObject.SetActive(!string.IsNullOrEmpty(bonus));
        }

        private void BindCountdown()
        {
            if (_countdown == null) return;
            int seconds = _definition.OfferDurationSeconds;
            if (seconds > 0) _countdown.StartCountdown(TimeSpan.FromSeconds(seconds));
        }
    }
}
