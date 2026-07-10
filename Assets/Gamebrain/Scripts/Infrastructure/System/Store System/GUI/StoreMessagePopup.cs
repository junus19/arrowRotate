using TMPro;
using UnityEngine;
using Casual = GameBrain.Casual;

namespace GameBrain.Store
{
    /// <summary>
    /// Surfaces a purchase failure. The machine-readable <see cref="PurchaseFailureReason"/> is mapped to
    /// display text here (UI layer), keeping the shop core string-free. Extends the framework's UIPopup,
    /// so it reuses the open/close scale animation and the base close button.
    /// </summary>
    public sealed class StoreMessagePopup : Casual.UIPopup
    {
        [SerializeField] private TMP_Text _messageText;

        public void ShowFailure(PurchaseFailureReason reason)
        {
            SetTitle("Purchase Failed");
            if (_messageText != null) _messageText.text = GetMessage(reason);
            Open();
        }

        public static string GetMessage(PurchaseFailureReason reason)
        {
            switch (reason)
            {
                case PurchaseFailureReason.InsufficientFunds: return "Not enough currency.";
                case PurchaseFailureReason.MaxPurchasesReached: return "You've reached the purchase limit.";
                case PurchaseFailureReason.AlreadyOwned: return "You already own this item.";
                case PurchaseFailureReason.RequirementNotMet: return "Requirements not met.";
                case PurchaseFailureReason.ItemNotFound: return "This item is unavailable.";
                case PurchaseFailureReason.PaymentFailed: return "Payment failed or was cancelled.";
                default: return "Purchase could not be completed.";
            }
        }
    }
}
