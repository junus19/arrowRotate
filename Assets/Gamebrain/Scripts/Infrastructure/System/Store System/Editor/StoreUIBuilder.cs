using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;
using Casual = GameBrain.Casual;

namespace GameBrain.Store.EditorTools
{
    /// <summary>
    /// One-click generator for the Store UI: builds the example catalog/items/icon-set ScriptableObjects,
    /// the fully-wired view/popup/panel prefabs, and an optional demo scene. Everything is created through
    /// the real Unity API so all serialized references resolve correctly. Visuals are plain coloured
    /// placeholders — drop in your own sprites afterwards.
    ///
    /// Menu: GameBrain → Store → 1. Build Store UI Assets, then 2. Build Demo Scene.
    /// Generated content lives under "Store System/Generated" — delete that folder to remove it all.
    /// </summary>
    public static class StoreUIBuilder
    {
        private const string Root = "Assets/Gamebrain/Scripts/Infrastructure/System/Store System/Generated";
        private const string PrefabsFolder = Root + "/Prefabs";
        private const string DataFolder = Root + "/Data";
        private const string ItemsFolder = DataFolder + "/Items";
        private const string ScenePath = Root + "/StoreDemo.unity";

        private const string CatalogPath = DataFolder + "/StoreCatalog.asset";
        private const string IconSetPath = DataFolder + "/StoreRewardIcons.asset";
        private const string PanelPrefabPath = PrefabsFolder + "/StorePanel.prefab";

        // ---- palette (approx. of the reference art) ----
        private static readonly Color Purple = new Color(0.25f, 0.18f, 0.38f);
        private static readonly Color Blue = new Color(0.30f, 0.55f, 0.85f);
        private static readonly Color Yellow = new Color(0.95f, 0.75f, 0.25f);
        private static readonly Color Pink = new Color(0.95f, 0.45f, 0.65f);
        private static readonly Color Green = new Color(0.30f, 0.78f, 0.45f);
        private static readonly Color CardWhite = new Color(0.96f, 0.96f, 0.98f);
        private static readonly Color Dark = new Color(0.18f, 0.14f, 0.28f);

        [MenuItem("GameBrain/Store/1. Build Store UI Assets")]
        public static void BuildAssets()
        {
            EnsureFolder(PrefabsFolder);
            EnsureFolder(ItemsFolder);

            // --- data ---
            StoreRewardIconSet iconSet = CreateOrLoad<StoreRewardIconSet>(IconSetPath);

            List<ShopItemDefinition> items = new List<ShopItemDefinition>
            {
                CreateItem("starter_pack", "STARTER PACK", CurrencyType.RealMoney, 0, "EUR 10,99",
                    StoreItemLayout.Bundle, StoreBadgeType.None, "70% EXTRA", 84180, true, -1,
                    new List<IShopReward>
                    {
                        new CurrencyReward(CurrencyType.Coin, 1000),
                        new BoosterReward(Casual.BoosterType.Hammer, 2),
                        new BoosterReward(Casual.BoosterType.Swap, 8),
                        new BoosterReward(Casual.BoosterType.Refresh, 3)
                    }),

                // second pack so the carousel has more than one page
                CreateItem("value_pack", "VALUE PACK", CurrencyType.RealMoney, 0, "EUR 19,99",
                    StoreItemLayout.Bundle, StoreBadgeType.None, "50% EXTRA", 169200, true, -1,
                    new List<IShopReward>
                    {
                        new CurrencyReward(CurrencyType.Coin, 2500),
                        new BoosterReward(Casual.BoosterType.Hammer, 5),
                        new BoosterReward(Casual.BoosterType.Swap, 5),
                        new BoosterReward(Casual.BoosterType.Refresh, 5)
                    }),

                CreateItem("no_ads", "NO ADS", CurrencyType.RealMoney, 0, "EUR 9,49",
                    StoreItemLayout.Generic, StoreBadgeType.None, "", 0, false, 1,
                    new List<IShopReward>()),

                CreateCoin("coins_150", 150, "EUR 2,29", StoreBadgeType.None),
                CreateCoin("coins_700", 700, "EUR 8,99", StoreBadgeType.None),
                CreateCoin("coins_1800", 1800, "EUR 16,99", StoreBadgeType.None),
                CreateCoin("coins_4000", 4000, "EUR 33,99", StoreBadgeType.None),
                CreateCoin("coins_7000", 7000, "EUR 54,99", StoreBadgeType.BestValue),
                CreateCoin("coins_15000", 15000, "EUR 114,99", StoreBadgeType.Popular),

                CreateItem("offer_1plus1", "1+1 OFFER", CurrencyType.RealMoney, 0, "EUR 7,99",
                    StoreItemLayout.Hidden, StoreBadgeType.None, "BUY 1, GET 1 FREE!", 255600, true, -1,
                    new List<IShopReward>
                    {
                        new BoosterReward(Casual.BoosterType.Swap, 3),
                        new BoosterReward(Casual.BoosterType.Hammer, 3),
                        new BoosterReward(Casual.BoosterType.Refresh, 3),
                        new CurrencyReward(CurrencyType.Coin, 100)
                    })
            };

            ShopCatalogDefinition catalog = CreateCatalog(items);

            // --- prefabs (dependency order) ---
            StoreRewardEntryView chip = BuildRewardChip();
            PageIndicatorItem dot = BuildPageIndicatorDot();
            StoreItemView coinTile = BuildCoinTile();
            StoreItemView genericRow = BuildGenericRow();
            StoreBundleView bundleCard = BuildBundleCard(chip);
            StoreMessagePopup messagePopup = BuildMessagePopup();
            BuildOfferPopup(chip);                                   // standalone, for offer flow
            BuildStorePanel(catalog, iconSet, coinTile, bundleCard, genericRow, messagePopup, dot);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[StoreUIBuilder] Done. Assets + prefabs created under " + Root +
                      ". Assign your sprites on the StoreRewardIcons asset and the view prefabs.");
        }

        [MenuItem("GameBrain/Store/2. Build Demo Scene")]
        public static void BuildDemoScene()
        {
            GameObject panelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PanelPrefabPath);
            ShopCatalogDefinition catalog = AssetDatabase.LoadAssetAtPath<ShopCatalogDefinition>(CatalogPath);
            if (panelPrefab == null || catalog == null)
            {
                Debug.LogError("[StoreUIBuilder] Run '1. Build Store UI Assets' first.");
                return;
            }

            UnityEngine.SceneManagement.Scene scene =
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            GameObject canvasGo = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Canvas canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);

            if (Object.FindObjectOfType<EventSystem>() == null)
                new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

            GameObject panel = (GameObject)PrefabUtility.InstantiatePrefab(panelPrefab, canvasGo.transform);
            StretchFull(panel.GetComponent<RectTransform>());

            GameObject installerGo = new GameObject("StoreInstaller", typeof(StoreInstaller));
            Wire(installerGo.GetComponent<StoreInstaller>(), ("_catalog", catalog));

            EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log("[StoreUIBuilder] Demo scene created at " + ScenePath +
                      ". Press Play — the StoreInstaller builds a live ShopService (in-memory wallet).");
        }

        // ============================ data builders ============================

        private static ShopItemDefinition CreateCoin(string id, int amount, string priceLabel, StoreBadgeType badge)
        {
            return CreateItem(id, amount.ToString(), CurrencyType.RealMoney, 0, priceLabel,
                StoreItemLayout.CoinTile, badge, "", 0, true, -1,
                new List<IShopReward> { new CurrencyReward(CurrencyType.Coin, amount) });
        }

        private static ShopItemDefinition CreateItem(string id, string label, CurrencyType currency, int price,
            string priceLabel, StoreItemLayout layout, StoreBadgeType badge, string bonus, int offerSeconds,
            bool consumable, int maxPurchases, List<IShopReward> rewards)
        {
            string path = ItemsFolder + "/" + id + ".asset";
            ShopItemDefinition def = CreateOrLoad<ShopItemDefinition>(path);

            SerializedObject so = new SerializedObject(def);
            so.FindProperty("_id").stringValue = id;
            so.FindProperty("_label").stringValue = label;
            so.FindProperty("_currency").enumValueIndex = (int)currency;
            so.FindProperty("_price").intValue = price;
            so.FindProperty("_maxPurchases").intValue = maxPurchases;
            so.FindProperty("_isConsumable").boolValue = consumable;
            so.FindProperty("_layout").enumValueIndex = (int)layout;
            so.FindProperty("_priceLabelOverride").stringValue = priceLabel ?? "";
            so.FindProperty("_badge").enumValueIndex = (int)badge;
            so.FindProperty("_bonusLabel").stringValue = bonus ?? "";
            so.FindProperty("_offerDurationSeconds").intValue = offerSeconds;

            SerializedProperty arr = so.FindProperty("_rewards");
            arr.ClearArray();
            for (int i = 0; i < rewards.Count; i++)
            {
                arr.InsertArrayElementAtIndex(i);
                arr.GetArrayElementAtIndex(i).managedReferenceValue = rewards[i];
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(def);
            return def;
        }

        private static ShopCatalogDefinition CreateCatalog(List<ShopItemDefinition> items)
        {
            ShopCatalogDefinition catalog = CreateOrLoad<ShopCatalogDefinition>(CatalogPath);
            SerializedObject so = new SerializedObject(catalog);
            SerializedProperty arr = so.FindProperty("_items");
            arr.ClearArray();
            for (int i = 0; i < items.Count; i++)
            {
                arr.InsertArrayElementAtIndex(i);
                arr.GetArrayElementAtIndex(i).objectReferenceValue = items[i];
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(catalog);
            return catalog;
        }

        // ============================ prefab builders ============================

        private static StoreRewardEntryView BuildRewardChip()
        {
            RectTransform root = NewRect("StoreRewardChip", null);
            SetSize(root, 90, 120);
            AddBg(root, CardWhite);

            Image icon = AddImage(root, "Icon", Blue);
            SetAnchored(icon.rectTransform, new Vector2(0.5f, 0.65f), new Vector2(70, 70));

            TextMeshProUGUI amount = AddLabel(root, "Amount", "x3", 26, Dark, TextAlignmentOptions.Center);
            SetAnchored(amount.rectTransform, new Vector2(0.5f, 0.15f), new Vector2(80, 30));

            StoreRewardEntryView view = root.gameObject.AddComponent<StoreRewardEntryView>();
            Wire(view, ("_iconImage", icon), ("_amountText", amount));

            return SavePrefab<StoreRewardEntryView>(root.gameObject, PrefabsFolder + "/StoreRewardChip.prefab");
        }

        private static StoreItemView BuildCoinTile()
        {
            RectTransform root = NewRect("StoreCoinTile", null);
            SetSize(root, 220, 230);
            AddBg(root, Blue);

            Image icon = AddImage(root, "Icon", Yellow);
            SetAnchored(icon.rectTransform, new Vector2(0.5f, 0.62f), new Vector2(120, 90));

            TextMeshProUGUI label = AddLabel(root, "Amount", "1800", 34, Color.white, TextAlignmentOptions.Center);
            SetAnchored(label.rectTransform, new Vector2(0.5f, 0.40f), new Vector2(200, 44));

            // badge ribbon
            RectTransform badge = NewRect("Badge", root);
            SetAnchored(badge, new Vector2(0.5f, 0.95f), new Vector2(200, 36));
            AddBg(badge, Pink);
            TextMeshProUGUI badgeText = AddLabel(badge, "Text", "BEST VALUE", 20, Color.white, TextAlignmentOptions.Center);
            StretchFull(badgeText.rectTransform);

            // price button
            Button buy = MakeButton(root, "BuyButton", Green, out RectTransform buyRt);
            SetAnchored(buyRt, new Vector2(0.5f, 0.12f), new Vector2(200, 56));
            TextMeshProUGUI price = AddLabel(buyRt, "Price", "EUR 16,99", 24, Color.white, TextAlignmentOptions.Center);
            StretchFull(price.rectTransform);
            Image priceIcon = AddImage(buyRt, "PriceCurrencyIcon", Yellow);
            SetAnchored(priceIcon.rectTransform, new Vector2(0.12f, 0.5f), new Vector2(28, 28));
            priceIcon.gameObject.SetActive(false);

            StoreItemView view = root.gameObject.AddComponent<StoreItemView>();
            Wire(view,
                ("_iconImage", icon), ("_labelText", label), ("_buyButton", buy),
                ("_priceText", price), ("_priceCurrencyIcon", priceIcon),
                ("_badgeRoot", badge.gameObject), ("_badgeText", badgeText));

            return SavePrefab<StoreItemView>(root.gameObject, PrefabsFolder + "/StoreCoinTile.prefab");
        }

        private static StoreItemView BuildGenericRow()
        {
            RectTransform root = NewRect("StoreGenericRow", null);
            SetSize(root, 720, 130);
            AddBg(root, Pink);

            Image icon = AddImage(root, "Icon", CardWhite);
            SetAnchored(icon.rectTransform, new Vector2(0.1f, 0.5f), new Vector2(90, 90));

            TextMeshProUGUI label = AddLabel(root, "Label", "NO ADS", 30, Color.white, TextAlignmentOptions.Left);
            SetAnchored(label.rectTransform, new Vector2(0.45f, 0.5f), new Vector2(340, 80));

            Button buy = MakeButton(root, "BuyButton", Green, out RectTransform buyRt);
            SetAnchored(buyRt, new Vector2(0.86f, 0.5f), new Vector2(180, 70));
            TextMeshProUGUI price = AddLabel(buyRt, "Price", "EUR 9,49", 24, Color.white, TextAlignmentOptions.Center);
            StretchFull(price.rectTransform);

            StoreItemView view = root.gameObject.AddComponent<StoreItemView>();
            Wire(view, ("_iconImage", icon), ("_labelText", label), ("_buyButton", buy), ("_priceText", price));

            return SavePrefab<StoreItemView>(root.gameObject, PrefabsFolder + "/StoreGenericRow.prefab");
        }

        private static StoreBundleView BuildBundleCard(StoreRewardEntryView chipPrefab)
        {
            // Width ~matches the carousel viewport so one card ≈ one page.
            RectTransform root = NewRect("StoreBundleCard", null);
            SetSize(root, 940, 300);
            AddBg(root, Yellow);

            TextMeshProUGUI label = AddLabel(root, "Title", "STARTER PACK", 30, Dark, TextAlignmentOptions.Left);
            SetAnchored(label.rectTransform, new Vector2(0.28f, 0.88f), new Vector2(360, 50));

            // NOTE: no background Image here — the GameObject already has a TMP Graphic, and a
            // GameObject can hold only one Graphic. Skin it with a separate ribbon sprite later.
            TextMeshProUGUI bonus = AddLabel(root, "Bonus", "70% EXTRA", 24, Pink, TextAlignmentOptions.Center);
            SetAnchored(bonus.rectTransform, new Vector2(0.85f, 0.88f), new Vector2(220, 44));

            // countdown
            RectTransform cdRt = NewRect("Countdown", root);
            SetAnchored(cdRt, new Vector2(0.5f, 0.97f), new Vector2(220, 36));
            TextMeshProUGUI cdText = cdRt.gameObject.AddComponent<TextMeshProUGUI>();
            cdText.text = "23h 23m"; cdText.fontSize = 22; cdText.color = Color.white;
            cdText.alignment = TextAlignmentOptions.Center;
            CountdownView countdown = cdRt.gameObject.AddComponent<CountdownView>();
            Wire(countdown, ("_text", cdText));

            // reward chips row
            RectTransform rewards = NewRect("Rewards", root);
            SetAnchored(rewards, new Vector2(0.5f, 0.45f), new Vector2(660, 140));
            HorizontalLayoutGroup hlg = rewards.gameObject.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 12; hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childForceExpandWidth = false; hlg.childForceExpandHeight = false;

            Image icon = AddImage(root, "Icon", CardWhite);
            SetAnchored(icon.rectTransform, new Vector2(0.1f, 0.55f), new Vector2(110, 110));

            Button buy = MakeButton(root, "BuyButton", Green, out RectTransform buyRt);
            SetAnchored(buyRt, new Vector2(0.85f, 0.12f), new Vector2(200, 60));
            TextMeshProUGUI price = AddLabel(buyRt, "Price", "EUR 10,99", 26, Color.white, TextAlignmentOptions.Center);
            StretchFull(price.rectTransform);

            StoreBundleView view = root.gameObject.AddComponent<StoreBundleView>();
            Wire(view,
                ("_iconImage", icon), ("_labelText", label), ("_buyButton", buy), ("_priceText", price),
                ("_rewardEntriesContainer", rewards), ("_rewardEntryPrefab", chipPrefab),
                ("_bonusLabelText", bonus), ("_countdown", countdown));

            return SavePrefab<StoreBundleView>(root.gameObject, PrefabsFolder + "/StoreBundleCard.prefab");
        }

        private static PageIndicatorItem BuildPageIndicatorDot()
        {
            RectTransform root = NewRect("PageIndicatorDot", null);
            SetSize(root, 24, 24);
            AddBg(root, new Color(1f, 1f, 1f, 0.35f)); // inactive dot

            Image selected = AddImage(root, "Selected", Color.white); // active highlight
            StretchFull(selected.rectTransform);

            PageIndicatorItem item = root.gameObject.AddComponent<PageIndicatorItem>();
            Wire(item, ("_selected", selected.gameObject));

            return SavePrefab<PageIndicatorItem>(root.gameObject, PrefabsFolder + "/PageIndicatorDot.prefab");
        }

        private static StoreMessagePopup BuildMessagePopup()
        {
            RectTransform root = NewRect("StoreMessagePopup", null);
            SetSize(root, 700, 420);
            AddBg(root, CardWhite);

            TextMeshProUGUI title = AddLabel(root, "Title", "Purchase Failed", 32, Dark, TextAlignmentOptions.Center);
            SetAnchored(title.rectTransform, new Vector2(0.5f, 0.82f), new Vector2(600, 60));

            TextMeshProUGUI message = AddLabel(root, "Message", "", 26, Dark, TextAlignmentOptions.Center);
            SetAnchored(message.rectTransform, new Vector2(0.5f, 0.52f), new Vector2(600, 120));

            Button close = MakeButton(root, "CloseButton", Green, out RectTransform closeRt);
            SetAnchored(closeRt, new Vector2(0.5f, 0.18f), new Vector2(240, 70));
            TextMeshProUGUI closeText = AddLabel(closeRt, "Label", "OK", 26, Color.white, TextAlignmentOptions.Center);
            StretchFull(closeText.rectTransform);

            StoreMessagePopup popup = root.gameObject.AddComponent<StoreMessagePopup>();
            Wire(popup, ("_closeButton", close), ("_titleText", title), ("_messageText", message));

            return SavePrefab<StoreMessagePopup>(root.gameObject, PrefabsFolder + "/StoreMessagePopup.prefab");
        }

        private static void BuildOfferPopup(StoreRewardEntryView chipPrefab)
        {
            RectTransform root = NewRect("StoreOfferPopup", null);
            SetSize(root, 820, 1200);
            AddBg(root, Blue);

            TextMeshProUGUI title = AddLabel(root, "Title", "1+1 OFFER", 44, Color.white, TextAlignmentOptions.Center);
            SetAnchored(title.rectTransform, new Vector2(0.5f, 0.92f), new Vector2(600, 80));

            RectTransform left = NewRect("LeftColumn", root);
            SetAnchored(left, new Vector2(0.3f, 0.55f), new Vector2(280, 620));
            AddColumnLayout(left);

            RectTransform right = NewRect("RightColumn", root);
            SetAnchored(right, new Vector2(0.7f, 0.55f), new Vector2(280, 620));
            AddColumnLayout(right);

            RectTransform cdRt = NewRect("Countdown", root);
            SetAnchored(cdRt, new Vector2(0.5f, 0.2f), new Vector2(260, 44));
            TextMeshProUGUI cdText = cdRt.gameObject.AddComponent<TextMeshProUGUI>();
            cdText.text = "2D 23H"; cdText.fontSize = 26; cdText.color = Color.white;
            cdText.alignment = TextAlignmentOptions.Center;
            CountdownView countdown = cdRt.gameObject.AddComponent<CountdownView>();
            Wire(countdown, ("_text", cdText));

            Button buy = MakeButton(root, "BuyButton", Green, out RectTransform buyRt);
            SetAnchored(buyRt, new Vector2(0.5f, 0.12f), new Vector2(360, 80));
            TextMeshProUGUI price = AddLabel(buyRt, "Price", "EUR 7,99", 30, Color.white, TextAlignmentOptions.Center);
            StretchFull(price.rectTransform);

            // "No Thanks" doubles as the UIPopup close button
            Button noThanks = MakeButton(root, "NoThanksButton", Dark, out RectTransform noThanksRt);
            SetAnchored(noThanksRt, new Vector2(0.5f, 0.04f), new Vector2(260, 50));
            TextMeshProUGUI noThanksText = AddLabel(noThanksRt, "Label", "No Thanks", 24, Color.white, TextAlignmentOptions.Center);
            StretchFull(noThanksText.rectTransform);

            StoreOfferPopup popup = root.gameObject.AddComponent<StoreOfferPopup>();
            Wire(popup,
                ("_closeButton", noThanks), ("_titleText", title),
                ("_leftColumn", left), ("_rightColumn", right), ("_entryPrefab", chipPrefab),
                ("_priceText", price), ("_buyButton", buy), ("_countdown", countdown));

            SavePrefab<StoreOfferPopup>(root.gameObject, PrefabsFolder + "/StoreOfferPopup.prefab");
        }

        private static void BuildStorePanel(ShopCatalogDefinition catalog, StoreRewardIconSet iconSet,
            StoreItemView coinTile, StoreBundleView bundleCard, StoreItemView genericRow,
            StoreMessagePopup messagePopup, PageIndicatorItem dotPrefab)
        {
            RectTransform root = NewRect("StorePanel", null);
            StretchFull(root);
            AddBg(root, Purple);

            TextMeshProUGUI header = AddLabel(root, "Header", "SHOP", 40, Color.white, TextAlignmentOptions.Center);
            SetAnchored(header.rectTransform, new Vector2(0.5f, 0.95f), new Vector2(400, 70));

            // --- bundle carousel (horizontal, paged) ---
            RectTransform carouselRt = NewRect("BundleCarousel", root);
            carouselRt.anchorMin = new Vector2(0.05f, 0.68f);
            carouselRt.anchorMax = new Vector2(0.95f, 0.92f);
            carouselRt.offsetMin = Vector2.zero; carouselRt.offsetMax = Vector2.zero;
            ScrollRect scroll = carouselRt.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = true; scroll.vertical = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 20f;

            RectTransform viewport = NewRect("Viewport", carouselRt);
            StretchFull(viewport);
            viewport.gameObject.AddComponent<RectMask2D>();

            RectTransform content = NewRect("Content", viewport);
            content.anchorMin = new Vector2(0f, 0f);
            content.anchorMax = new Vector2(0f, 1f);
            content.pivot = new Vector2(0f, 0.5f);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = Vector2.zero;
            // No layout group / size fitter: StoreCarouselView lays out full-width pages at runtime,
            // so paging is pixel-exact (each page == one viewport width, no leftover sliver).
            scroll.viewport = viewport; scroll.content = content;

            // page dots, just below the carousel
            RectTransform dotsRt = NewRect("PageIndicator", root);
            SetAnchored(dotsRt, new Vector2(0.5f, 0.655f), new Vector2(240, 28));
            HorizontalLayoutGroup dotsHlg = dotsRt.gameObject.AddComponent<HorizontalLayoutGroup>();
            dotsHlg.spacing = 12; dotsHlg.childAlignment = TextAnchor.MiddleCenter;
            dotsHlg.childControlWidth = true; dotsHlg.childControlHeight = true;
            dotsHlg.childForceExpandWidth = false; dotsHlg.childForceExpandHeight = false;
            PageIndicator pageIndicator = dotsRt.gameObject.AddComponent<PageIndicator>();
            Wire(pageIndicator, ("_itemPrefab", dotPrefab));

            StoreCarouselView carousel = carouselRt.gameObject.AddComponent<StoreCarouselView>();
            Wire(carousel, ("_scrollRect", scroll), ("_content", content), ("_pageIndicator", pageIndicator));

            // --- generic rows (No Ads) ---
            RectTransform generic = NewRect("GenericContainer", root);
            generic.anchorMin = new Vector2(0.05f, 0.55f);
            generic.anchorMax = new Vector2(0.95f, 0.63f);
            generic.offsetMin = Vector2.zero; generic.offsetMax = Vector2.zero;
            VerticalLayoutGroup genericVlg = generic.gameObject.AddComponent<VerticalLayoutGroup>();
            genericVlg.spacing = 10; genericVlg.childAlignment = TextAnchor.UpperCenter;
            genericVlg.childForceExpandWidth = false; genericVlg.childForceExpandHeight = false;

            // --- COINS label + grid ---
            TextMeshProUGUI coinsLabel = AddLabel(root, "CoinsLabel", "COINS", 30, Color.white, TextAlignmentOptions.Center);
            SetAnchored(coinsLabel.rectTransform, new Vector2(0.5f, 0.52f), new Vector2(300, 44));

            RectTransform grid = NewRect("CoinGrid", root);
            grid.anchorMin = new Vector2(0.05f, 0.08f);
            grid.anchorMax = new Vector2(0.95f, 0.50f);
            grid.offsetMin = Vector2.zero; grid.offsetMax = Vector2.zero;
            GridLayoutGroup glg = grid.gameObject.AddComponent<GridLayoutGroup>();
            glg.cellSize = new Vector2(220, 230); glg.spacing = new Vector2(16, 16);
            glg.constraint = GridLayoutGroup.Constraint.FixedColumnCount; glg.constraintCount = 3;
            glg.childAlignment = TextAnchor.UpperCenter;

            // nested message popup (starts inactive)
            GameObject popupInstance = (GameObject)PrefabUtility.InstantiatePrefab(messagePopup.gameObject, root);
            StretchCentered(popupInstance.GetComponent<RectTransform>(), 700, 420);
            popupInstance.SetActive(false);
            StoreMessagePopup popupComp = popupInstance.GetComponent<StoreMessagePopup>();

            StorePanel panel = root.gameObject.AddComponent<StorePanel>();
            Wire(panel,
                ("_catalog", catalog), ("_iconSet", iconSet),
                ("_bundleCarousel", carousel), ("_genericContainer", generic), ("_coinGridContainer", grid),
                ("_bundlePrefab", bundleCard), ("_genericItemPrefab", genericRow), ("_coinItemPrefab", coinTile),
                ("_messagePopup", popupComp));

            SavePrefab<StorePanel>(root.gameObject, PanelPrefabPath);
        }

        // ============================ helpers ============================

        private static void AddColumnLayout(RectTransform rt)
        {
            AddBg(rt, Pink);
            VerticalLayoutGroup vlg = rt.gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 10; vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childForceExpandWidth = false; vlg.childForceExpandHeight = false;
            vlg.padding = new RectOffset(12, 12, 12, 12);
        }

        private static RectTransform NewRect(string name, Transform parent)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            RectTransform rt = go.GetComponent<RectTransform>();
            if (parent != null) rt.SetParent(parent, false);
            return rt;
        }

        private static Image AddBg(RectTransform rt, Color color)
        {
            Image img = rt.gameObject.AddComponent<Image>();
            img.color = color;
            return img;
        }

        private static Image AddImage(RectTransform parent, string name, Color color)
        {
            RectTransform rt = NewRect(name, parent);
            return AddBg(rt, color);
        }

        private static TextMeshProUGUI AddLabel(Transform parent, string name, string text, float size,
            Color color, TextAlignmentOptions align)
        {
            RectTransform rt = NewRect(name, parent);
            TextMeshProUGUI t = rt.gameObject.AddComponent<TextMeshProUGUI>();
            t.text = text; t.fontSize = size; t.color = color; t.alignment = align;
            return t;
        }

        private static Button MakeButton(Transform parent, string name, Color color, out RectTransform rt)
        {
            rt = NewRect(name, parent);
            Image img = AddBg(rt, color);
            Button btn = rt.gameObject.AddComponent<Button>();
            btn.targetGraphic = img;
            return btn;
        }

        private static void SetSize(RectTransform rt, float w, float h)
        {
            rt.sizeDelta = new Vector2(w, h);
            LayoutElement le = rt.gameObject.AddComponent<LayoutElement>();
            le.preferredWidth = w; le.preferredHeight = h;
        }

        private static void SetAnchored(RectTransform rt, Vector2 anchor, Vector2 size)
        {
            rt.anchorMin = anchor; rt.anchorMax = anchor; rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size; rt.anchoredPosition = Vector2.zero;
        }

        private static void StretchFull(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        }

        private static void StretchCentered(RectTransform rt, float w, float h)
        {
            rt.anchorMin = new Vector2(0.5f, 0.5f); rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f); rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(w, h);
        }

        private static T SavePrefab<T>(GameObject go, string path) where T : Component
        {
            GameObject saved = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            return saved.GetComponent<T>();
        }

        private static void Wire(Object component, params (string name, Object value)[] refs)
        {
            SerializedObject so = new SerializedObject(component);
            foreach ((string name, Object value) in refs)
            {
                SerializedProperty p = so.FindProperty(name);
                if (p == null)
                {
                    Debug.LogError($"[StoreUIBuilder] Field '{name}' not found on {component.GetType().Name}.");
                    continue;
                }
                p.objectReferenceValue = value;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static T CreateOrLoad<T>(string path) where T : ScriptableObject
        {
            T existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing != null) return existing;
            T asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace("\\", "/");
            string leaf = Path.GetFileName(path);
            if (!AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, leaf);
        }
    }
}
