using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using System.Text.RegularExpressions;

namespace CustomColorGrid
{
    public struct RGBA
    {
        public float red;
        public float green;
        public float blue;
        public float alpha;

        public override string ToString()
        {
            return $"RGBA({red}, {green}, {blue}, {alpha})";
        }
    }

    [BepInPlugin(pluginGuid, pluginName, pluginVersion)]
    [BepInProcess("DSPGAME.exe")]
    public class CustomGridColor : BaseUnityPlugin
    {
        public const string pluginGuid = "io.ryen.customcolorgrid";
        public const string pluginName = "Custom Color Grid";
        public const string pluginVersion = "1.0.0.0";

        new internal static ManualLogSource Logger;

        private ConfigEntry<string> buildingGridRGBAConfig;
        private ConfigEntry<string> destructionGridRGBAConfig;
        private ConfigEntry<string> foundationGridRGBAConfig;

        public static RGBA buildingGridRGBA;
        public static RGBA destructionGridRGBA;
        public static RGBA foundationGridRGBA;

        public void Awake()
        {
            CustomGridColor.Logger = base.Logger;

            // initialize config options
            buildingGridRGBAConfig = Config.Bind("Colors",
                "BuildingGridColor",
                "124, 204, 90, 0.404",
                "RGBA values for the building grid.\n" +
                "Valid RGB values are any integer between 0 - 255\n" +
                "Valid alpha channel values is a float between 0 - 1");
            destructionGridRGBAConfig = Config.Bind("Colors",
                "DestructionGridColor",
                "217, 79, 72, 0.404",
                "RGBA values for the destruction grid.\n" +
                "Valid RGB values are any integer between 0 - 255\n" +
                "Valid alpha channel values is a float between 0 - 1");
            foundationGridRGBAConfig = Config.Bind("Colors",
                "FoundationGridColor",
                "68, 144, 226, 0.553",
                "RGBA values for the foundation grid.\n" +
                "Valid RGB values are any integer between 0 - 255\n" +
                "Valid alpha channel values is a float between 0 - 1");

            // parse config strings to usable data
            string rgbaPattern = @"(\d{1,3}),\s*(\d{1,3}),\s*(\d{1,3}),\s*(\d\.?(?:\d+)?)";

            MatchCollection matchBuildingGrid = Regex.Matches(buildingGridRGBAConfig.Value, rgbaPattern);
            MatchCollection matchDestructionGrid = Regex.Matches(destructionGridRGBAConfig.Value, rgbaPattern);
            MatchCollection matchFoundationGrid = Regex.Matches(foundationGridRGBAConfig.Value, rgbaPattern);

            // check if user formatted the setting correctly
            if (matchBuildingGrid[0].Groups.Count != 5)
            {
                Logger.LogInfo("Configuration option \"BuildingGridColor\" was set incorrectly, falling back to default.");
                matchBuildingGrid = Regex.Matches(buildingGridRGBAConfig.DefaultValue.ToString(), rgbaPattern);
            }

            if (matchDestructionGrid[0].Groups.Count != 5)
            {
                Logger.LogInfo("Configuration option \"DestructionGridColor\" was set incorrectly, falling back to default.");
                matchDestructionGrid = Regex.Matches(destructionGridRGBAConfig.DefaultValue.ToString(), rgbaPattern);
            }

            if (matchFoundationGrid[0].Groups.Count != 5)
            {
                Logger.LogInfo("Configuration option \"FoundationGridColor\" was set incorrectly, falling back to default.");
                matchBuildingGrid = Regex.Matches(foundationGridRGBAConfig.DefaultValue.ToString(), rgbaPattern);
            }

            // convert the values to floats and convert them to values Unity expects
            buildingGridRGBA.red = float.Parse(matchBuildingGrid[0].Groups[1].Value) / 255;
            buildingGridRGBA.green = float.Parse(matchBuildingGrid[0].Groups[2].Value) / 255;
            buildingGridRGBA.blue = float.Parse(matchBuildingGrid[0].Groups[3].Value) / 255;
            buildingGridRGBA.alpha = float.Parse(matchBuildingGrid[0].Groups[4].Value);

            destructionGridRGBA.red = float.Parse(matchDestructionGrid[0].Groups[1].Value) / 255;
            destructionGridRGBA.green = float.Parse(matchDestructionGrid[0].Groups[2].Value) / 255;
            destructionGridRGBA.blue = float.Parse(matchDestructionGrid[0].Groups[3].Value) / 255;
            destructionGridRGBA.alpha = float.Parse(matchDestructionGrid[0].Groups[4].Value);

            foundationGridRGBA.red = float.Parse(matchFoundationGrid[0].Groups[1].Value) / 255;
            foundationGridRGBA.green = float.Parse(matchFoundationGrid[0].Groups[2].Value) / 255;
            foundationGridRGBA.blue = float.Parse(matchFoundationGrid[0].Groups[3].Value) / 255;
            foundationGridRGBA.alpha = float.Parse(matchFoundationGrid[0].Groups[4].Value);


            // initalize harmony and patch
            var harmony = new Harmony(pluginGuid);
            harmony.PatchAll();

            Logger.LogInfo($"Initiated {pluginGuid} v{pluginVersion}");
        }
    }

    [HarmonyPatch(typeof(UIBuildingGrid), "Start")]
    class Patch : MonoBehaviour
    {
        public static void Postfix(ref Color ___buildColor, ref Color ___destructColor, ref Color ___reformColor)
        {
            CustomGridColor.Logger.LogInfo($"build grid before change: {___buildColor}");
            CustomGridColor.Logger.LogInfo($"destruction grid before change: {___destructColor}");
            CustomGridColor.Logger.LogInfo($"foundation grid before change: {___reformColor}");

            // override the base build color
            ___buildColor = new Color(CustomGridColor.buildingGridRGBA.red,
                CustomGridColor.buildingGridRGBA.green,
                CustomGridColor.buildingGridRGBA.blue,
                CustomGridColor.buildingGridRGBA.alpha);
            ___destructColor = new Color(CustomGridColor.destructionGridRGBA.red,
                CustomGridColor.destructionGridRGBA.green,
                CustomGridColor.destructionGridRGBA.blue,
                CustomGridColor.destructionGridRGBA.alpha);
            ___reformColor = new Color(CustomGridColor.foundationGridRGBA.red,
                CustomGridColor.foundationGridRGBA.green,
                CustomGridColor.foundationGridRGBA.blue,
                CustomGridColor.foundationGridRGBA.alpha);

            CustomGridColor.Logger.LogInfo($"build grid after change: {___buildColor}");
            CustomGridColor.Logger.LogInfo($"destruction grid after change: {___destructColor}");
            CustomGridColor.Logger.LogInfo($"foundation grid after change: {___reformColor}");
        }
    }
}
