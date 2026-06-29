using BepInEx;
using BepInEx.Configuration;

namespace SimpleLunarCoins {

    [BepInDependency("com.rune580.riskofoptions", BepInDependency.DependencyFlags.SoftDependency)]

    [BepInDependency("com.KingEnderBrine.ProperSave", BepInDependency.DependencyFlags.SoftDependency)]

    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]

    public class SimpleLunarCoins : BaseUnityPlugin {
        public const string PluginGUID = PluginAuthor + "." + PluginName;
        public const string PluginAuthor = "Miyowi";
        public const string PluginName = "SimpleLunarCoins";
        public const string PluginVersion = "1.3.0";

        public static PluginInfo pluginInfo;

        public static ConfigFile config;

        public void Awake() {
            pluginInfo = Info;
            config = Config;
            Log.Init(Logger);
            Assets.Init();
            Events.Init();
            Options.Init();
            new ContentPacks().Init();
        }
    }
}
