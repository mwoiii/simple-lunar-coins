using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using RiskOfOptions.OptionConfigs;
using RiskOfOptions.Options;
using RiskOfOptions;
using ProperSave;
using UnityEngine;

namespace SimpleLunarCoins
{
    public static class RiskOfOptionsCompatibility
    {
        private static bool? _enabled;

        public static bool enabled
        {
            get
            {
                if (_enabled == null)
                {
                    _enabled = BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("com.rune580.riskofoptions");
                }
                return (bool)_enabled;
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        public static void OptionsInit()
        {
            ModSettingsManager.AddOption(new SliderOption(SimpleLunarCoins.coinChance, new SliderConfig() { min = 0, max = 100 }));
            ModSettingsManager.AddOption(new StepSliderOption(SimpleLunarCoins.coinMultiplier, new StepSliderConfig() { min = 0, max = 1, increment = 0.01f }));
            ModSettingsManager.AddOption(new IntSliderOption(SimpleLunarCoins.startingCoins, new IntSliderConfig() { min = 0, max = 10000 }));
            ModSettingsManager.AddOption(new CheckBoxOption(SimpleLunarCoins.teamCoins));
            ModSettingsManager.AddOption(new CheckBoxOption(SimpleLunarCoins.noCoinDroplet));
            ModSettingsManager.AddOption(new CheckBoxOption(SimpleLunarCoins.resetCoins));

            ModSettingsManager.SetModDescription("Provides simple customisation of the lunar coins behaviour, without interfering with the balance of lunar items themselves.");

            Sprite icon = Assets.mainAssetBundle.LoadAsset<Sprite>("icon.png");
            ModSettingsManager.SetModIcon(icon);
        }
    }

    public static class ProperSaveCompatibility
    {
        private static bool? _enabled;

        public static bool enabled
        {
            get
            {
                if (_enabled == null)
                {
                    _enabled = BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("com.KingEnderBrine.ProperSave");
                }
                return (bool)_enabled;
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        public static void AddEvent(Action<Dictionary<string, object>> action)
        {
            ProperSave.SaveFile.OnGatherSaveData += action;
        }

        public static bool IsLoading
        {
            [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
            get { return ProperSave.Loading.IsLoading; }
        }

        // forgive me for my foolish ways this whole compatibility thing has gotten me in a kerfuffle it's 3:30am and the birds are tweeting I'm going insane
        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        public static string GetModdedData(string name)
        {
            return ProperSave.Loading.CurrentSave.GetModdedData<string>(name);
        }
    }

    public class SimpleLunarCoins_ProperSaveObj
    {
        public Dictionary<ulong, uint> playerCoins;
        public SimpleLunarCoins_ProperSaveObj(Dictionary<ulong, uint> playerCoins)
        {
            this.playerCoins = playerCoins;
        }
    }

}
