using System;
using System.Collections.Generic;

namespace SimpleLunarCoins {
    public static class ProperSaveCompat {
        private static bool? _enabled;

        public static bool enabled {
            get {
                if (_enabled == null) {
                    _enabled = BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("com.KingEnderBrine.ProperSave");
                }
                return (bool)_enabled;
            }
        }

        public static void AddEvent(Action<Dictionary<string, object>> action) {
            ProperSave.SaveFile.OnGatherSaveData += action;
        }

        public static bool IsLoading {
            get { return ProperSave.Loading.IsLoading; }
        }

        public static string GetModdedData(string name) {
            return ProperSave.Loading.CurrentSave.GetModdedData<string>(name);
        }
    }

    public class SimpleLunarCoins_ProperSaveObj {
        public Dictionary<ulong, uint> playerCoins;

        public SimpleLunarCoins_ProperSaveObj(Dictionary<ulong, uint> playerCoins) {
            this.playerCoins = playerCoins;
        }
    }
}
