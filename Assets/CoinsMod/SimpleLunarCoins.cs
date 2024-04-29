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
        public const string PluginVersion = "1.0.0";

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

            On.RoR2.PlayerCharacterMasterController.Awake += InitialCoinChance;

            BindingFlags allFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly;
            var initDelegate = typeof(PlayerCharacterMasterController).GetNestedTypes(allFlags)[0].GetMethodCached(name: "<Init>b__72_0");
            MonoMod.RuntimeDetour.HookGen.HookEndpointManager.Modify(initDelegate, (Action<ILContext>)CoinDropHook);

            On.RoR2.Run.OnUserAdded += StartingCoins;

            ContentPackProvider.Initialize();

            //On.RoR2.Networking.NetworkManagerSystemSteam.OnClientConnect += (s, u, t) => { };

        }

        public void Start()
        {
            SoundBanks.Init();
        }

        public static class Assets
        {
            public static AssetBundle mainAssetBundle = null;
            //the filename of your assetbundle
            internal static string assetBundleName = "mwassetbundle";

            internal static string assemblyDir
            {
                get
                {
                    return Path.GetDirectoryName(SimpleLunarCoins.pluginInfo.Location);
                }
            }

            public static void PopulateAssets()
            {
                mainAssetBundle = AssetBundle.LoadFromFile(Path.Combine(assemblyDir, assetBundleName));
                ContentPackProvider.serializedContentPack = mainAssetBundle.LoadAsset<SerializableContentPack>(ContentPackProvider.contentPackName);
            }
        }

        public class ContentPackProvider : IContentPackProvider
        {
            public static SerializableContentPack serializedContentPack;
            public static ContentPack contentPack;
            //Should be the same names as your SerializableContentPack in the asset bundle
            public static string contentPackName = "CoinPack";

            public string identifier
            {
                get
                {
                    return "SimpleLunarCoins";
                }
            }

            internal static void Initialize()
            {
                contentPack = serializedContentPack.CreateContentPack();
                ContentManager.collectContentPackProviders += AddCustomContent;
            }

            private static void AddCustomContent(ContentManager.AddContentPackProviderDelegate addContentPackProvider)
            {
                addContentPackProvider(new ContentPackProvider());
            }

            public IEnumerator LoadStaticContentAsync(LoadStaticContentAsyncArgs args)
            {
                args.ReportProgress(1f);
                yield break;
            }

            public IEnumerator GenerateContentPackAsync(GetContentPackAsyncArgs args)
            {
                ContentPack.Copy(contentPack, args.output);
                args.ReportProgress(1f);
                yield break;
            }

            public IEnumerator FinalizeAsync(FinalizeAsyncArgs args)
            {
                args.ReportProgress(1f);
                yield break;
            }
        }

        internal static class SoundBanks
        {
            private static bool initialized = false;
            public static string SoundBankDirectory
            {
                get
                {
                    return Path.Combine(Assets.assemblyDir);
                }
            }

            public static void Init()
            {
                if (initialized) return;
                initialized = true;
                AKRESULT akResult = AkSoundEngine.AddBasePath(SoundBankDirectory);
                if (akResult == AKRESULT.AK_Success)
                {
                    Log.Info($"Added bank base path : {SoundBankDirectory}");
                }
                else
                {
                    Log.Error(
                        $"Error adding base path : {SoundBankDirectory} " +
                        $"Error code : {akResult}");
                }

                AkSoundEngine.LoadBank("cointoss.bnk", out _);
                if (akResult == AKRESULT.AK_Success)
                {
                    Log.Info($"Added bank : {"cointoss.bnk"}");
                }
                else
                {
                    Log.Error(
                        $"Error loading bank : {"cointoss.bnk"} " +
                        $"Error code : {akResult}");
                }
            }
        }


        private static void InitialCoinChance(On.RoR2.PlayerCharacterMasterController.orig_Awake orig, PlayerCharacterMasterController self)
        {
            orig(self);
            self.SetFieldValue("lunarCoinChanceMultiplier", coinChance.Value);
        }

        private static void CoinDropHook(ILContext il)
        {
            var c = new ILCursor(il);

            var b = c.TryGotoNext(
                x => x.MatchLdsfld(typeof(RoR2Content.MiscPickups).FullName, nameof(RoR2Content.MiscPickups.LunarCoin)),
                x => x.MatchLdfld<MiscPickupDef>("miscPickupIndex"),
                x => x.MatchCallOrCallvirt(typeof(PickupCatalog).FullName, nameof(PickupCatalog.FindPickupIndex)),
                x => x.MatchLdarg(1),
                x => x.MatchLdfld<DamageReport>("victim"),
                x => x.MatchCallOrCallvirt<Component>("get_transform"),
                x => x.MatchCallOrCallvirt<Transform>("get_position"),
                x => x.MatchCallOrCallvirt<Vector3>("get_up"),
                x => x.MatchLdcR4(10),
                x => x.MatchCallOrCallvirt<Vector3>("op_Multiply"),
                x => x.MatchCallOrCallvirt<PickupDropletController>("CreatePickupDroplet"),
                x => x.MatchLdloc(1),
                x => x.MatchDup(),
                x => x.MatchLdfld<PlayerCharacterMasterController>("lunarCoinChanceMultiplier"),
                x => x.MatchLdcR4(0.5f),
                x => x.MatchMul()
                );

            if (b)
            {

                var label = c.DefineLabel();

                c.Index += 11;
                c.MarkLabel(label);
                c.Index -= 11;
                c.Emit(OpCodes.Br, label);
                c.Index += 14;
                c.Next.Operand = coinMultiplier.Value;
                c.Index += 2;
                c.Emit(OpCodes.Ldarg_1);

                c.EmitDelegate<Action<DamageReport>>((damageReport) =>
                {
                    foreach (PlayerCharacterMasterController instance in PlayerCharacterMasterController.instances)
                    {
#pragma warning disable Publicizer001
                        if ((bool)instance.GetFieldValue<NetworkUser>("resolvedNetworkUserInstance")) { instance.GetFieldValue<NetworkUser>("resolvedNetworkUserInstance").AwardLunarCoins(1); }
#pragma warning restore Publicizer001
                    }

                    AssetBundleRequest loadAsset = Assets.mainAssetBundle.LoadAssetAsync<GameObject>("LunarCoinEmitter");
                    GameObject myLoadedPrefab = loadAsset.asset as GameObject;
                    AkSoundEngine.PostEvent(4043138392, damageReport.victim.gameObject);

                    var coinEffectPrefab = myLoadedPrefab;
                    EffectManager.SpawnEffect(coinEffectPrefab, new EffectData
                    {
                        origin = damageReport.victimBody.corePosition,
                        genericFloat = 20f,
                        scale = damageReport.victimBody.radius
                    }, transmit: true);

                });
            }
            else { Log.Info("IL failed?"); }
        }

        private static void StartingCoins(On.RoR2.Run.orig_OnUserAdded orig, Run self, NetworkUser user)
        {
            user.InvokeMethod("RpcDeductLunarCoins", user.lunarCoins);
            user.InvokeMethod("RpcAwardLunarCoins", startingCoins.Value);
            orig(self, user);
        }

    }
}
