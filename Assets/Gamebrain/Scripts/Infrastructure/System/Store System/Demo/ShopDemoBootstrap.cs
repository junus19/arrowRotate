using UnityEngine;

namespace GameBrain.Store
{
    /// <summary>
    /// Drop this on an empty GameObject and press Play (or use the context-menu item) to run the shop's
    /// self-verification and print PASS/FAIL to the Console. Pure in-memory — touches no save data.
    /// Doubles as the seed for a manual demo scene: replace the in-memory wallet with a
    /// <see cref="CurrencyManagerWallet"/> and a real <see cref="ShopCatalogDefinition"/> to go live.
    /// </summary>
    public sealed class ShopDemoBootstrap : MonoBehaviour
    {
        [SerializeField] private bool _runOnStart = true;

        private void Start()
        {
            if (_runOnStart) Run();
        }

        [ContextMenu("Run Shop Verification")]
        public void Run()
        {
            System.Collections.Generic.List<ShopVerification.Result> results = ShopVerification.RunAll();

            int passed = 0;
            for (int i = 0; i < results.Count; i++)
            {
                ShopVerification.Result result = results[i];
                if (result.Passed)
                {
                    passed++;
                    Debug.Log(result.ToString());
                }
                else
                {
                    Debug.LogError(result.ToString());
                }
            }

            string summary = $"[Shop] Verification: {passed}/{results.Count} scenarios passed.";
            if (passed == results.Count) Debug.Log(summary);
            else Debug.LogError(summary);
        }
    }
}
