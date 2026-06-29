using BepInEx.Configuration;
using RiskOfOptions;
using RiskOfOptions.OptionConfigs;
using RiskOfOptions.Options;

namespace SimpleLunarCoins {
    public static class Options {

        private static bool? _rooEnabled;

        public static bool rooEnabled {
            get {
                if (_rooEnabled == null) {
                    _rooEnabled = BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("com.rune580.riskofoptions");
                }
                return (bool)_rooEnabled;
            }
        }

        public static ConfigEntry<float> coinChance { get; set; }
        public static ConfigEntry<float> coinMultiplier { get; set; }
        public static ConfigEntry<int> startingCoins { get; set; }
        public static ConfigEntry<bool> teamCoins { get; set; }
        public static ConfigEntry<bool> noCoinDroplet { get; set; }
        public static ConfigEntry<bool> resetCoins { get; set; }

        public static void Init() {
            coinChance = SimpleLunarCoins.config.Bind("Lunar Coin Adjustments", "Initial Coin Chance", 0.5f, "Chance for first lunar coin to be dropped.");
            coinMultiplier = SimpleLunarCoins.config.Bind("Lunar Coin Adjustments", "Coin Chance Multiplier", 0.5f, "Value that chance is multiplied by after a coin is dropped.");
            startingCoins = SimpleLunarCoins.config.Bind("Lunar Coin Adjustments", "Starting Coins", 5, "Coins that each player has at the start of a run, if 'Reset Coins Each Run' is enabled.");
            teamCoins = SimpleLunarCoins.config.Bind("Lunar Coin Adjustments", "Distribute Coins", true, "All allies receive a lunar coin when one is dropped.");
            noCoinDroplet = SimpleLunarCoins.config.Bind("Lunar Coin Adjustments", "No Coin Droplets", true, "Enemies emit a lunar coin effect instead of the regular droplet that is manually picked up.");
            resetCoins = SimpleLunarCoins.config.Bind("Lunar Coin Adjustments", "Reset Coins Each Run", false, "Lunar coins are reset at the start of a run to the value determined by 'Starting Coins'.");

            if (rooEnabled) {
                RoOInit();
            }
        }

        public static void RoOInit() {
            ModSettingsManager.AddOption(new StepSliderOption(coinChance, new StepSliderConfig() { min = 0, max = 100, increment = 0.01f }));
            ModSettingsManager.AddOption(new StepSliderOption(coinMultiplier, new StepSliderConfig() { min = 0, max = 1, increment = 0.01f }));
            ModSettingsManager.AddOption(new IntSliderOption(startingCoins, new IntSliderConfig() { min = 0, max = 10000 }));
            ModSettingsManager.AddOption(new CheckBoxOption(teamCoins));
            ModSettingsManager.AddOption(new CheckBoxOption(noCoinDroplet));
            ModSettingsManager.AddOption(new CheckBoxOption(resetCoins));

            ModSettingsManager.SetModDescription("Provides simple customisation of the lunar coins behaviour, without interfering with the balance of lunar items themselves.");

            ModSettingsManager.SetModIcon(Assets.icon);
        }
    }
}
