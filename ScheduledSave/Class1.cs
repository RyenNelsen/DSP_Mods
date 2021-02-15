using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using NGPT;
using UnityEngine;
using UnityEngine.UI;
using System.Text.RegularExpressions;
using System.IO;

namespace ScheduledSave
{
    public class EvaluateFilename
    {
        private static string finalFilename;
        private static int maxFilenumber = 0;

        public static string eval(string filename)
        {
            finalFilename = filename;

            // search for highest number
            string[] saveFiles = Directory.GetFiles(GameConfig.gameSaveFolder);
            string matchFilenamePattern = @"/(.+/)*(.+)\.(.+)$";
            string dynamicPattern = Regex.Replace(finalFilename, @"\(count\)", @"(\d{4})");

            foreach (string file in saveFiles)
            {
                // isolate just the file names
                Match isolatedSaveFilename = Regex.Match(file, matchFilenamePattern);
                // grab the numbers from the matched save file
                Match saveFileNumber = Regex.Match(isolatedSaveFilename.Groups[2].Value, dynamicPattern);

                if (saveFileNumber.Success)
                {
                    int testcase = int.Parse(saveFileNumber.Groups[1].Value);

                    // find the highest save file number
                    if (testcase > maxFilenumber)
                    {
                        maxFilenumber = testcase;
                    }
                }
            }

            // increment maxFilenumber so it does not colide with previous save
            maxFilenumber++;

            // replace <count> with the next number and pad it
            finalFilename = Regex.Replace(finalFilename, @"\(count\)", maxFilenumber.ToString("D4"));

            // just in case the user puts something not recognized in
            finalFilename = Regex.Replace(finalFilename, @"[<>/\\]", "");

            // TODO: Add some protections

            return finalFilename;
        }
    }

    [BepInPlugin(pluginGuid, pluginName, pluginVersion)]
    [BepInProcess("DSPGAME.exe")]
    public class ScheduledSave : BaseUnityPlugin
    {
        public const string pluginGuid = "io.ryen.scheduledsave";
        public const string pluginName = "Scheduled Save";
        public const string pluginVersion = "1.0.0.0";

        new internal static ManualLogSource Logger;

        private ConfigEntry<int> configScheduledSaveInterval;
        private ConfigEntry<string> configScheduledSaveFilename;

        public static long scheduledSaveInterval;
        public static string scheduledSaveFilename;

        public void Awake()
        {
            ScheduledSave.Logger = base.Logger;

            // setup configuration options
            configScheduledSaveInterval = Config.Bind("Save Settings",
                "SaveInterval",
                30,
                "The interval that you would like a save to be triggered by this plugin.");
            configScheduledSaveFilename = Config.Bind("Save Settings",
                "Filename",
                "ScheduledSave(count)",
                "The filename of the save. Optional paramters:\n" +
                "(count)\tthe file name will sequentially increment");

            // verify the config data was good otherwise fallback to defaults
            try
            {
                scheduledSaveInterval = configScheduledSaveInterval.Value * 60;
            } catch
            {
                Logger.LogInfo($"failed to parse float from configuration string ({configScheduledSaveInterval.Value}). Using default value as fallback.");
                scheduledSaveInterval = (long)configScheduledSaveInterval.DefaultValue * 60;
            }

            // parse filename
            scheduledSaveFilename = configScheduledSaveFilename.Value;

            // initalize harmony and patch
            var harmony = new Harmony(pluginGuid);
            harmony.PatchAll();

            Logger.LogInfo($"Initialized {pluginGuid} v{pluginVersion}");
        }
    }

    [HarmonyPatch]
    class Patch
    {
        private static long lastScheduledSaveTick;
        private static readonly double ticrateToSeconds = (double) 1 / 60;
        private static string saveFileName;

        [HarmonyPostfix]
        [HarmonyPatch(typeof(UIAutoSave), "_OnLateUpdate")]
        public static void _OnLateUpdatePostfix(ref float ___autoSaveTime, ref float ___showTime, ref Tweener ___contentTweener, ref Text ___saveText, ref CanvasGroup ___contentCanvas)
        {
            // check if the player is in game, is on a planet
            if (GameMain.localStar == null
                || GameMain.localStar.loaded
                || GameMain.localPlanet == null
                || (GameMain.localPlanet.loaded && GameMain.localPlanet.factoryLoaded))
            {
                // check if it is time to do a scheduled save by calculating the difference
                // between the last save and how often it should be saved minus animation time
                // and check that we are not currently saving
                if ((float)(GameMain.gameTick - lastScheduledSaveTick) * ticrateToSeconds > ScheduledSave.scheduledSaveInterval - 1.8d && ___showTime <= 0f)
                {
                    ScheduledSave.Logger.LogInfo("ScheduledSave triggered");

                    // trigger saving animation
                    ___showTime = 3.6f;
                    ___contentTweener.Play0To1();
                    ___saveText.text = "Scheduled save...";
                }

                // check if it is time to save
                if (___showTime > 0f)
                {
                    // game dev's way of making the animation fade in and out.
                    ___contentCanvas.alpha = Mathf.Min(___showTime / 0.6f, (3.6f - ___showTime) / 0.6f);
                    ___contentCanvas.gameObject.SetActive(true);

                    // check if the animation is half way done (completely opaque) and we have not saved recently
                    if (___showTime < 1.8f && (float)(GameMain.gameTick - lastScheduledSaveTick) * ticrateToSeconds > ScheduledSave.scheduledSaveInterval - 1.8d)
                    {
                        // set the last time we saved to now
                        lastScheduledSaveTick = GameMain.gameTick;

                        // generate the file name
                        saveFileName = EvaluateFilename.eval(ScheduledSave.scheduledSaveFilename);
                        ScheduledSave.Logger.LogInfo($"Saving to file: {saveFileName}");

                        // try and save the game
                        if (GameSave.SaveCurrentGame(saveFileName))
                        {
                            ___saveText.text = "Scheduled save\nwas successful";
                        }
                        else
                        {
                            ___saveText.text = "Scheduled save\nfailed";
                        }
                    }

                    // keep removing time from the animation for every frame
                    ___showTime -= Time.deltaTime;

                    // keep showTime's value positive
                    if (___showTime < 0f)
                    {
                        ___showTime = 0f;
                    }
                }
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(UIAutoSave), "_OnOpen")]
        public static void _OnOpenPostfix()
        {
            lastScheduledSaveTick = GameMain.gameTick;
        }
    }
}
