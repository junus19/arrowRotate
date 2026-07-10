using System;
using System.Collections.Generic;

namespace GameBrain.Store
{
    /// <summary>
    /// Framework-free correctness checks for the shop core. Builds a fully in-memory shop, runs every
    /// purchase scenario and asserts the wallet/history side effects. Returns a list of results so it can
    /// be driven from a MonoBehaviour (see <see cref="ShopDemoBootstrap"/>) or wrapped in NUnit later.
    /// This is the proof that the system behaves correctly when instantiated.
    /// </summary>
    public static class ShopVerification
    {
        public readonly struct Result
        {
            public readonly string Name;
            public readonly bool Passed;
            public readonly string Detail;

            public Result(string name, bool passed, string detail)
            {
                Name = name;
                Passed = passed;
                Detail = detail;
            }

            public override string ToString() =>
                $"{(Passed ? "PASS" : "FAIL")}  {Name}{(string.IsNullOrEmpty(Detail) ? "" : $"  ({Detail})")}";
        }

        // A throwaway reward + granter used to verify generic dispatch without touching game save data.
        private sealed class CountingReward : IShopReward { }

        private sealed class CountingGranter : IRewardGranter
        {
            public int Granted;
            public Type RewardType => typeof(CountingReward);
            public void Grant(IShopReward reward) => Granted++;
        }

        public static List<Result> RunAll()
        {
            List<Result> results = new List<Result>();

            // 1) Successful coin purchase debits price and grants reward.
            {
                InMemoryWallet wallet = new InMemoryWallet(coin: 100);
                IShopItem item = new ShopItem("coin_to_gem", CurrencyType.Coin, 50, -1, true,
                    new List<IShopReward> { new CurrencyReward(CurrencyType.Gem, 5) });
                ShopService shop = Build(wallet, item);
                PurchaseOutcome outcome = shop.Purchase("coin_to_gem");
                bool ok = outcome.Success
                          && wallet.GetBalance(CurrencyType.Coin) == 50
                          && wallet.GetBalance(CurrencyType.Gem) == 5;
                results.Add(new Result("Successful purchase debits price and grants reward", ok,
                    $"coin={wallet.GetBalance(CurrencyType.Coin)} gem={wallet.GetBalance(CurrencyType.Gem)}"));
                shop.Dispose();
            }

            // 2) Insufficient funds blocks and changes nothing.
            {
                InMemoryWallet wallet = new InMemoryWallet(coin: 10);
                IShopItem item = new ShopItem("expensive", CurrencyType.Coin, 50, -1, true,
                    new List<IShopReward> { new CurrencyReward(CurrencyType.Gem, 5) });
                ShopService shop = Build(wallet, item);
                PurchaseOutcome outcome = shop.Purchase("expensive");
                bool ok = !outcome.Success
                          && outcome.Reason == PurchaseFailureReason.InsufficientFunds
                          && wallet.GetBalance(CurrencyType.Coin) == 10
                          && wallet.GetBalance(CurrencyType.Gem) == 0;
                results.Add(new Result("Insufficient funds blocks; no debit, no grant", ok,
                    $"reason={outcome.Reason} coin={wallet.GetBalance(CurrencyType.Coin)}"));
                shop.Dispose();
            }

            // 3) Unknown item id.
            {
                InMemoryWallet wallet = new InMemoryWallet(coin: 100);
                IShopItem item = new ShopItem("known", CurrencyType.Coin, 10, -1, true, null);
                ShopService shop = Build(wallet, item);
                PurchaseOutcome outcome = shop.Purchase("missing_id");
                bool ok = !outcome.Success && outcome.Reason == PurchaseFailureReason.ItemNotFound;
                results.Add(new Result("Unknown id -> ItemNotFound", ok, $"reason={outcome.Reason}"));
                shop.Dispose();
            }

            // 4) Max purchases cap enforced.
            {
                InMemoryWallet wallet = new InMemoryWallet(coin: 1000);
                IShopItem item = new ShopItem("limited", CurrencyType.Coin, 10, 2, true, null);
                ShopService shop = Build(wallet, item);
                PurchaseOutcome a = shop.Purchase("limited");
                PurchaseOutcome b = shop.Purchase("limited");
                PurchaseOutcome c = shop.Purchase("limited");
                bool ok = a.Success && b.Success
                          && !c.Success && c.Reason == PurchaseFailureReason.MaxPurchasesReached;
                results.Add(new Result("MaxPurchases cap enforced after N buys", ok, $"third={c.Reason}"));
                shop.Dispose();
            }

            // 5) Non-consumable cannot be re-bought.
            {
                InMemoryWallet wallet = new InMemoryWallet(coin: 1000);
                IShopItem item = new ShopItem("remove_ads", CurrencyType.Coin, 10, -1, false, null);
                ShopService shop = Build(wallet, item);
                PurchaseOutcome first = shop.Purchase("remove_ads");
                PurchaseOutcome second = shop.Purchase("remove_ads");
                bool ok = first.Success
                          && !second.Success && second.Reason == PurchaseFailureReason.AlreadyOwned;
                results.Add(new Result("Non-consumable blocks repurchase (AlreadyOwned)", ok, $"second={second.Reason}"));
                shop.Dispose();
            }

            // 6) Reward dispatch reaches the matching granter (OCP / extensibility).
            {
                InMemoryWallet wallet = new InMemoryWallet(coin: 100);
                CountingGranter counter = new CountingGranter();
                IShopItem item = new ShopItem("custom_reward", CurrencyType.Coin, 10, -1, true,
                    new List<IShopReward> { new CountingReward(), new CountingReward() });
                ShopService shop = new ShopService(
                    new ShopCatalog(new List<IShopItem> { item }),
                    new List<IPaymentProcessor> { new WalletPaymentProcessor(CurrencyType.Coin, wallet) },
                    new List<IRewardGranter> { new CurrencyRewardGranter(wallet), counter },
                    new List<IPurchaseValidator> { new OwnershipValidator(), new MaxPurchaseValidator() });
                PurchaseOutcome outcome = shop.Purchase("custom_reward");
                bool ok = outcome.Success && counter.Granted == 2;
                results.Add(new Result("Reward dispatch reaches matching granter (OCP)", ok, $"granted={counter.Granted}"));
                shop.Dispose();
            }

            // 7) Undeliverable reward -> fail BEFORE charging (atomicity guard).
            {
                InMemoryWallet wallet = new InMemoryWallet(coin: 100);
                IShopItem item = new ShopItem("orphan", CurrencyType.Coin, 30, -1, true,
                    new List<IShopReward> { new CountingReward() }); // no granter registered for it
                ShopService shop = new ShopService(
                    new ShopCatalog(new List<IShopItem> { item }),
                    new List<IPaymentProcessor> { new WalletPaymentProcessor(CurrencyType.Coin, wallet) },
                    new List<IRewardGranter> { new CurrencyRewardGranter(wallet) });
                PurchaseOutcome outcome = shop.Purchase("orphan");
                bool ok = !outcome.Success
                          && outcome.Reason == PurchaseFailureReason.NoRewardHandler
                          && wallet.GetBalance(CurrencyType.Coin) == 100; // never charged
                results.Add(new Result("Undeliverable reward fails before charging (atomicity)", ok,
                    $"reason={outcome.Reason} coin={wallet.GetBalance(CurrencyType.Coin)}"));
                shop.Dispose();
            }

            // 8) Custom requirement validator gates the purchase (extensible rules).
            {
                InMemoryWallet wallet = new InMemoryWallet(coin: 100);
                IShopItem item = new ShopItem("level_locked", CurrencyType.Coin, 10, -1, true, null);
                bool unlocked = false;
                ShopService shop = new ShopService(
                    new ShopCatalog(new List<IShopItem> { item }),
                    new List<IPaymentProcessor> { new WalletPaymentProcessor(CurrencyType.Coin, wallet) },
                    new List<IRewardGranter> { new CurrencyRewardGranter(wallet) },
                    new List<IPurchaseValidator>
                    {
                        new DelegateRequirementValidator(_ => unlocked, PurchaseFailureReason.RequirementNotMet)
                    });
                PurchaseOutcome locked = shop.Purchase("level_locked");
                unlocked = true;
                PurchaseOutcome afterUnlock = shop.Purchase("level_locked");
                bool ok = !locked.Success && locked.Reason == PurchaseFailureReason.RequirementNotMet
                          && afterUnlock.Success;
                results.Add(new Result("Custom requirement validator gates purchase", ok,
                    $"locked={locked.Reason} unlocked={afterUnlock.Success}"));
                shop.Dispose();
            }

            // 9) A different currency (Gem) flows through the same generic path.
            {
                InMemoryWallet wallet = new InMemoryWallet(coin: 0, gem: 20);
                IShopItem item = new ShopItem("gem_pack", CurrencyType.Gem, 15, -1, true,
                    new List<IShopReward> { new CurrencyReward(CurrencyType.Coin, 100) });
                ShopService shop = Build(wallet, item);
                PurchaseOutcome outcome = shop.Purchase("gem_pack");
                bool ok = outcome.Success
                          && wallet.GetBalance(CurrencyType.Gem) == 5
                          && wallet.GetBalance(CurrencyType.Coin) == 100;
                results.Add(new Result("Gem-priced item works via the same generic flow", ok,
                    $"gem={wallet.GetBalance(CurrencyType.Gem)} coin={wallet.GetBalance(CurrencyType.Coin)}"));
                shop.Dispose();
            }

            return results;
        }

        private static ShopService Build(IWallet wallet, params IShopItem[] items)
        {
            return new ShopService(
                new ShopCatalog(new List<IShopItem>(items)),
                new List<IPaymentProcessor>
                {
                    new WalletPaymentProcessor(CurrencyType.Coin, wallet),
                    new WalletPaymentProcessor(CurrencyType.Gem, wallet)
                },
                new List<IRewardGranter> { new CurrencyRewardGranter(wallet) },
                new List<IPurchaseValidator> { new OwnershipValidator(), new MaxPurchaseValidator() });
        }
    }
}
