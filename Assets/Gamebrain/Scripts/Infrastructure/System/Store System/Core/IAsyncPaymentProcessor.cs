using System;

namespace GameBrain.Store
{
    /// <summary>
    /// A payment processor whose result is not known synchronously (e.g. platform IAP). The shop calls
    /// <see cref="BeginPay"/> and fulfils the purchase when the callback reports success. Such a processor
    /// is also registered as an <see cref="IPaymentProcessor"/> (for currency routing); its synchronous
    /// <see cref="IPaymentProcessor.Pay"/> is not used.
    /// </summary>
    public interface IAsyncPaymentProcessor
    {
        /// <summary>Start charging for the item. The result arrives on onComplete (Unity main thread).</summary>
        void BeginPay(IShopItem item, Action<bool> onComplete);
    }
}
