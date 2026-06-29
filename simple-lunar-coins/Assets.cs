using RoR2;
using RoR2BepInExPack.GameAssetPaths.Version_1_39_0;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.AddressableAssets;
using Path = System.IO.Path;


namespace SimpleLunarCoins {
    public static class Assets {
        private static AssetBundle assetBundle;

        public static GameObject coinEmitterPrefab;

        public static Sprite icon;

        internal static string assetBundleName = "mwmwsimplelunarcoinsbundle";

        internal static string assemblyDir {
            get {
                return Path.GetDirectoryName(SimpleLunarCoins.pluginInfo.Location);
            }
        }

        private static void TryBuildAsset(string assetName, System.Action buildAction) {
            try {
                buildAction();
            } catch (System.Exception e) {
                Log.Warning($"Failed to complete building asset {assetName}!\n\n{e}");
            }
        }

        private static void GetAssetBundle() {
            using (Stream assetStream = Assembly.GetExecutingAssembly().GetManifestResourceStream($"SimpleLunarCoins.mwmwsimplelunarcoinsbundle")) {
                if (assetStream != null) {
                    assetBundle = AssetBundle.LoadFromStream(assetStream);
                }
            }
        }

        public static void Init() {
            GetAssetBundle();

            CreateCoinEmitter();

            icon = assetBundle.LoadAsset<Sprite>("icon");
        }

        private static void CreateCoinEmitter() {
            coinEmitterPrefab = assetBundle.LoadAsset<GameObject>("LunarCoinEmitter");
            TryBuildAsset("Lunar Coin Emitter", () => {
                ParticleSystemRenderer psr = coinEmitterPrefab.transform.Find("LunarCoin").GetComponent<ParticleSystemRenderer>();
                psr.mesh = Addressables.LoadAssetAsync<Mesh>(RoR2_Base_Common_VFX.mdlLunarCoinWithHole_fbx).WaitForCompletion();
                psr.sharedMaterial = Addressables.LoadAssetAsync<Material>(RoR2_Base_Common_VFX.matLunarCoinPlaceholder_mat).WaitForCompletion();
            });
            EffectDef coinEmitterEffectDef = new EffectDef(coinEmitterPrefab);
            ContentPacks.effectDefs.Add(coinEmitterEffectDef);
        }
    }
}