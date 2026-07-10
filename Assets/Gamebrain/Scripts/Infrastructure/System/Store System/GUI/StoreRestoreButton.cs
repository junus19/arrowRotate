using UnityEngine;
using UnityEngine.UI;
using GameBrain.Utils;
using Casual = GameBrain.Casual;

namespace GameBrain.Store
{
    /// <summary>
    /// "Restore Purchases" button. Raises <see cref="ShopRestoreRequestedEvent"/> on click and re-enables
    /// itself when <see cref="ShopPurchasesRestoredEvent"/> arrives. Required on iOS; harmless elsewhere.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class StoreRestoreButton : MonoBehaviour
    {
        [SerializeField] private Button _button;

        private EventBinding<ShopPurchasesRestoredEvent> _restoredBinding;

        private void Reset() => _button = GetComponent<Button>();

        private void OnEnable()
        {
            if (_button != null) _button.onClick.AddListener(OnClicked);
            _restoredBinding = new EventBinding<ShopPurchasesRestoredEvent>(OnRestored);
            EventBus<ShopPurchasesRestoredEvent>.Register(_restoredBinding);
        }

        private void OnDisable()
        {
            if (_button != null) _button.onClick.RemoveListener(OnClicked);
            EventBus<ShopPurchasesRestoredEvent>.Deregister(_restoredBinding);
        }

        private void OnClicked()
        {
            EventBus<Casual.FxRequestEvent>.Raise(new Casual.FxRequestEvent(Casual.EffectType.Button));
            if (_button != null) _button.interactable = false;   // avoid double taps while restoring
            EventBus<ShopRestoreRequestedEvent>.Raise(new ShopRestoreRequestedEvent());
        }

        private void OnRestored(ShopPurchasesRestoredEvent evt)
        {
            if (_button != null) _button.interactable = true;
        }
    }
}
