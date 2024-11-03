using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace I2LocLoader
{
    [BepInPlugin(pluginGUID, pluginName, pluginVersion)]
    public class Plugin : BaseUnityPlugin
    {
        public const string pluginGUID = "com.metalted.zeepkist.i2locloader";
        public const string pluginName = "I2LocLoader";
        public const string pluginVersion = "1.0";
        public static Plugin Instance;

        public ConfigEntry<bool> reloadButton;
        public ConfigEntry<bool> exportEnglishCSV;

        private void Awake()
        {
            Harmony harmony = new Harmony(pluginGUID);
            harmony.PatchAll();

            Instance = this;

            Logger.LogInfo($"Plugin {pluginGUID} is loaded!");

            reloadButton = Config.Bind("Settings", "Reload Language Files", true, "[Button] Reload Language Files");
            exportEnglishCSV = Config.Bind("Settings", "Export English CSV", true, "[Button] Export English CSV");

            reloadButton.SettingChanged += OnReloadButton;
            exportEnglishCSV.SettingChanged += OnExportEnglishCSVButton;
        }        

        public void Initialize()
        {
            //Check if the i18n folder exist in the plugins folder and make it otherwise.
            if (!Directory.Exists(Path.Combine(Paths.PluginPath, "i18n")))
            {
                Directory.CreateDirectory(Path.Combine(Paths.PluginPath, "i18n"));
            }

            ReloadLanguageFiles();     
        }

        public void OnExportEnglishCSVButton(object sender, System.EventArgs e)
        {
            string english = I2LocLoaderClass.GetEnglishTermsCSV();
            File.WriteAllText(Path.Combine(Paths.PluginPath, "EnglishTerms.csv"), english);
            Debug.Log("Succesfully exported english terms to csv.");
        }

        public void OnReloadButton(object sender, System.EventArgs e)
        {
            ReloadLanguageFiles();
        }

        public void ReloadLanguageFiles()
        {
            //Get all csv files in the i18n folder.
            string[] csvFiles = Directory.GetFiles(Path.Combine(Paths.PluginPath, "i18n"), "*.csv", SearchOption.TopDirectoryOnly);

            foreach(string csvFile in csvFiles)
            {
                I2LocLoaderClass.LoadLanguageCSV(csvFile);
            }

            Debug.Log("Loaded languages: " + string.Join(',', I2LocLoaderClass.GetCurrentLanguages()));

            SettingsUI settingsUI = GameObject.FindObjectOfType<SettingsUI>();

            if(settingsUI != null)
            {
                SetLanguagesInSettingsUI(settingsUI);
            }
        }

        public void SetLanguagesInSettingsUI(SettingsUI settingsUI)
        {
            List<string> currentLanguages = I2LocLoaderClass.GetCurrentLanguages();

            // Find the index of "Arabic" in the list
            int arabicIndex = currentLanguages.IndexOf("Arabic");
            if (arabicIndex > 0) // Proceed only if Arabic is found and not the first language
            {
                // Find indices of "German" and "French"
                int germanIndex = currentLanguages.IndexOf("German");
                int frenchIndex = currentLanguages.IndexOf("French");

                // Check if "German" and "French" appear before "Arabic" in the correct order
                if ((germanIndex >= 0 && germanIndex < arabicIndex) ||
                    (frenchIndex >= 0 && frenchIndex < arabicIndex))
                {
                    // Remove "German" and "French" if they appear before "Arabic"
                    // Removing in reverse order to avoid index shifting issues
                    if (frenchIndex >= 0 && frenchIndex < arabicIndex)
                    {
                        currentLanguages.RemoveAt(frenchIndex);
                        arabicIndex--; // Adjust arabicIndex after removing French
                        if (germanIndex > frenchIndex) germanIndex--; // Adjust if German comes after French
                    }
                    if (germanIndex >= 0 && germanIndex < arabicIndex)
                    {
                        currentLanguages.RemoveAt(germanIndex);
                        arabicIndex--; // Adjust arabicIndex after removing German
                    }
                }
            }

            // Finally, remove "Arabic"
            currentLanguages.Remove("Arabic");

            // Update the available languages in the Settings UI instance
            settingsUI.AvailableLanguages = currentLanguages;
        }
    }

    [HarmonyPatch(typeof(I2.Loc.LocalizationManager), "RegisterSceneSources")]
    public class PlayerManagerAwakePatch
    {
        public static void Postfix()
        {
            Plugin.Instance.Initialize();
        }
    }

    [HarmonyPatch(typeof(SettingsUI), "Awake")]
    public class SettingsUIAwake
    {
        public static void Prefix(SettingsUI __instance)
        {
            Plugin.Instance.SetLanguagesInSettingsUI(__instance);
        }
    }
}
