using BepInEx;
using MonoMod.Cil;
using Mono.Cecil.Cil;
using R2API;
using R2API.Utils;
using RoR2;
using UnityEngine;
using System;
using System.Reflection;
using BepInEx.Configuration;

namespace SimpleLunarCoins
{

    [BepInDependency("com.rune580.riskofoptions", BepInDependency.DependencyFlags.SoftDependency)]

    [BepInDependency(LanguageAPI.PluginGUID)]

    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]

    public class SimpleLunarCoins : BaseUnityPlugin
    {
        public const string PluginGUID = PluginAuthor + "." + PluginName;
        public const string PluginAuthor = "Miyowi";
        public const string PluginName = "SimpleLunarCoins";
        public const string PluginVersion = "1.1.4";

        public static ConfigEntry<float> coinChance { get; set; }
        public static ConfigEntry<float> coinMultiplier { get; set; }
        public static ConfigEntry<int> startingCoins { get; set; }
        public static ConfigEntry<bool> teamCoins { get; set; }
        public static ConfigEntry<bool> noCoinDroplet { get; set; }
        public static ConfigEntry<bool> resetCoins { get; set; }

        public static PluginInfo pluginInfo;

        public void Awake()
        {
            pluginInfo = Info;

            Assets.PopulateAssets();

            coinChance = Config.Bind("Lunar Coin Adjustments", "Initial Coin Chance", 2f, "Chance for first lunar coin to be dropped.");
            coinMultiplier = Config.Bind("Lunar Coin Adjustments", "Coin Chance Multiplier", 1f, "Value that chance is multiplied by after a coin is dropped.");
            startingCoins = Config.Bind("Lunar Coin Adjustments", "Starting Coins", 5, "Coins that each player has at the start of a run, if 'Reset Coins Each Run' is enabled.");
            teamCoins = Config.Bind("Lunar Coin Adjustments", "Distribute Coins", true, "All allies receive a lunar coin when one is dropped.");
            noCoinDroplet = Config.Bind("Lunar Coin Adjustments", "No Coin Droplets", true, "Enemies emit a lunar coin effect instead of the regular droplet that is manually picked up.");
            resetCoins = Config.Bind("Lunar Coin Adjustments", "Reset Coins Each Run", true, "Lunar coins are reset at the start of a run to the value determined by 'Starting Coins'.");

            Log.Init(Logger);

            Hooks.Init();

            ContentPackProvider.Initialize();

            if (RiskOfOptionsCompatibility.enabled)
            {
                RiskOfOptionsCompatibility.OptionsInit();
            }
            //On.RoR2.Networking.NetworkManagerSystemSteam.OnClientConnect += (s, u, t) => { };
        }

        public void Start()
        {
            SoundBanks.Init();
        }

    }
}
