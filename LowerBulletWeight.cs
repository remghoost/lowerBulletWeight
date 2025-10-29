using System;
using System.IO;
using System.Linq;
using Duckov.Modding;
using HarmonyLib;
using ItemStatsSystem;
using UnityEngine;
using SodaCraft.Localizations;
using ReplaceThisWithYourModNameSpace;

namespace LowerBulletWeight
{
    [Serializable]
    public class LBWConfig
    {
        public float BulletWeightMultiplier = 0.5f;
        public string configToken = "lower_bullet_weight_v1";
    }

    public class ModBehaviour : Duckov.Modding.ModBehaviour
    {
        public static string MOD_NAME = "LowerBulletWeight";

        private Harmony _harmony;
        public static LBWConfig Config = new LBWConfig();

        private static string PersistentConfigPath => Path.Combine(Application.streamingAssetsPath, "LowerBulletWeightConfig.txt");

        private void OnEnable()
        {
            try
            {
                // Initialize ModConfig communication
                ModConfigAPI.Initialize();

                // Subscribe to ModManager events
                ModManager.OnModActivated += OnModActivated;

                // If ModConfig is already active, set up immediately
                if (ModConfigAPI.IsAvailable())
                {
                    Debug.Log("[LowerBulletWeight] ModConfig available at startup!");
                    SetupModConfig();
                    LoadConfigFromModConfig();
                }

                // Apply Harmony patches
                _harmony = new Harmony("LowerBulletWeight");
                _harmony.PatchAll();
                Debug.Log("[LowerBulletWeight] Mod enabled and Harmony patches applied!");
            }
            catch (Exception ex)
            {
                Debug.LogError("[LowerBulletWeight] Failed to initialize: " + ex);
            }
        }

        private void OnDisable()
        {
            try
            {
                _harmony?.UnpatchSelf();
                ModManager.OnModActivated -= OnModActivated;
                ModConfigAPI.SafeRemoveOnOptionsChangedDelegate(OnModConfigOptionsChanged);
                Debug.Log("[LowerBulletWeight] Mod disabled and Harmony patches removed.");
            }
            catch (Exception ex)
            {
                Debug.LogError("[LowerBulletWeight] Error disabling mod: " + ex);
            }
        }

        private void OnModActivated(ModInfo info, Duckov.Modding.ModBehaviour behaviour)
        {
            if (info.name == ModConfigAPI.ModConfigName)
            {
                Debug.Log("[LowerBulletWeight] ModConfig just activated!");
                SetupModConfig();
                LoadConfigFromModConfig();
            }
        }

        private void SetupModConfig()
        {
            if (!ModConfigAPI.IsAvailable())
            {
                Debug.LogWarning("[LowerBulletWeight] ModConfig not available — skipping setup.");
                return;
            }

            Debug.Log("[LowerBulletWeight] Setting up ModConfig options...");

            // Watch for changes
            ModConfigAPI.SafeAddOnOptionsChangedDelegate(OnModConfigOptionsChanged);

            // Localized label
            SystemLanguage[] chineseLanguages = {
                SystemLanguage.Chinese,
                SystemLanguage.ChineseSimplified,
                SystemLanguage.ChineseTraditional
            };
            bool isChinese = chineseLanguages.Contains(LocalizationManager.CurrentLanguage);

            // Add slider (range 0.1x to 2x)
            ModConfigAPI.SafeAddInputWithSlider(
                MOD_NAME,
                "BulletWeightMultiplier",
                isChinese ? "子弹重量倍率" : "Bullet Weight Multiplier",
                typeof(float),
                Config.BulletWeightMultiplier,
                new Vector2(0.1f, 2f)
            );

            Debug.Log("[LowerBulletWeight] ModConfig setup completed.");
        }

        private void OnModConfigOptionsChanged(string key)
        {
            if (!key.StartsWith(MOD_NAME + "_"))
                return;

            LoadConfigFromModConfig();
            SaveConfig(Config);

            Debug.Log($"[LowerBulletWeight] ModConfig updated — new multiplier: {Config.BulletWeightMultiplier:F2}");
        }

        private void LoadConfigFromModConfig()
        {
            Config.BulletWeightMultiplier = ModConfigAPI.SafeLoad<float>(
                MOD_NAME,
                "BulletWeightMultiplier",
                Config.BulletWeightMultiplier
            );
        }

        private static void SaveConfig(LBWConfig cfg)
        {
            try
            {
                string json = JsonUtility.ToJson(cfg, true);
                File.WriteAllText(PersistentConfigPath, json);
                Debug.Log("[LowerBulletWeight] Config saved to file.");
            }
            catch (Exception e)
            {
                Debug.LogError("[LowerBulletWeight] Failed to save config: " + e);
            }
        }
    }

    [HarmonyPatch(typeof(Item), nameof(Item.RecalculateTotalWeight))]
    public static class BulletWeightPatch
    {
        [HarmonyPostfix]
        public static void Postfix(Item __instance, ref float __result)
        {
            try
            {
                if (__instance == null)
                    return;

                string itemName = __instance.name ?? string.Empty;

                if (itemName.StartsWith("Bullet_", StringComparison.OrdinalIgnoreCase))
                {
                    __result *= ModBehaviour.Config.BulletWeightMultiplier;
                    //Debug.Log($"[LowerBulletWeight] Adjusted {itemName} weight → {__result:F3}");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError("[LowerBulletWeight] Exception during patch: " + ex);
            }
        }
    }
    [HarmonyPatch(typeof(Item), "TotalWeight", MethodType.Getter)]
    public static class BulletWeightGetterPatch
    {
        [HarmonyPostfix]
        public static void Postfix(Item __instance, ref float __result)
        {
            string itemName = __instance.name ?? "";
            if (itemName.StartsWith("Bullet_", StringComparison.OrdinalIgnoreCase))
                __result *= ModBehaviour.Config.BulletWeightMultiplier;
        }
    }

}
