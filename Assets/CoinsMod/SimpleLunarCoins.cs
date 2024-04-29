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
using RoR2.ContentManagement;
using System.Collections;
using Path = System.IO.Path;

namespace SimpleLunarCoins
{

    [BepInDependency(LanguageAPI.PluginGUID)]

    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]

    public class SimpleLunarCoins : BaseUnityPlugin
    {
        public const string PluginGUID = PluginAuthor + "." + PluginName;
        public const string PluginAuthor = "Miyowi";
        public const string PluginName = "SimpleLunarCoins";
        public const string PluginVersion = "1.0.1";

        public static ConfigEntry<float> coinChance { get; set; }
        public static ConfigEntry<float> coinMultiplier { get; set; }
        public static ConfigEntry<uint> startingCoins { get; set; }

        public static PluginInfo pluginInfo;

        public void Awake()
        {
            pluginInfo = Info;

            Assets.PopulateAssets();

            coinChance = Config.Bind("Lunar Coin Adjustments", "Initial Coin Chance", 5f, "% Chance for first lunar coin to be dropped");
            coinMultiplier = Config.Bind("Lunar Coin Adjustments", "Coin Chance Multiplier", 1f, "Value that chance is multiplied by after a coin is dropped");
            startingCoins = Config.Bind("Lunar Coin Adjustments", "Starting Coins", (uint)5, "Coins that each player has at the start of a run");

            Log.Init(Logger);

            Hooks.Init();

            ContentPackProvider.Initialize();
            //On.RoR2.Networking.NetworkManagerSystemSteam.OnClientConnect += (s, u, t) => { };
        }

        public void Start()
        {
            SoundBanks.Init();
        }

    }
}
