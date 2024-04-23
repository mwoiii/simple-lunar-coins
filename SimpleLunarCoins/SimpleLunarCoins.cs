using BepInEx;
using MonoMod.Cil;
using Mono.Cecil.Cil;
using R2API;
using R2API.Utils;
using RoR2;
using UnityEngine;
using UnityEngine.AddressableAssets;
using System;
using System.Reflection;
using UnityEngine.Networking;
using TMPro;
using BepInEx.Configuration;

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

        public void Awake()
        {

            coinChance = Config.Bind("Lunar Coin Adjustments", "Initial Coin Chance", 5f, "% Chance for first lunar coin to be dropped");
            coinMultiplier = Config.Bind("Lunar Coin Adjustments", "Coin Chance Multiplier", 1f, "Value that chance is multiplied by after a coin is dropped");
            startingCoins = Config.Bind("Lunar Coin Adjustments", "Starting Coins", (uint)5, "Coins that each player has at the start of a run");


            Log.Init(Logger);

            On.RoR2.PlayerCharacterMasterController.Awake += InitialCoinChance;

            BindingFlags allFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly;
            var initDelegate = typeof(PlayerCharacterMasterController).GetNestedTypes(allFlags)[0].GetMethodCached(name: "<Init>b__72_0");
            MonoMod.RuntimeDetour.HookGen.HookEndpointManager.Modify(initDelegate, (Action<ILContext>)CoinDropHook);

            On.RoR2.Run.OnUserAdded += StartingCoins;
        }

        private static void InitialCoinChance(On.RoR2.PlayerCharacterMasterController.orig_Awake orig, PlayerCharacterMasterController self)
        {
            orig(self);
#pragma warning disable Publicizer001
            self.lunarCoinChanceMultiplier = coinChance.Value;
#pragma warning restore Publicizer001
        }

        private static void CoinDropHook(ILContext il)
        {
            var c = new ILCursor(il);

            var b = c.GotoNext(
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

            var label = c.DefineLabel();

            c.Index += 11;
            c.MarkLabel(label);
            c.Index -= 11;
            c.Emit(OpCodes.Br, label);
            c.Index += 14;
            c.Next.Operand = coinMultiplier.Value;
            c.Index += 2;

            c.EmitDelegate<Action>(() =>
            {
                foreach (PlayerCharacterMasterController instance in PlayerCharacterMasterController.instances)
                {
#pragma warning disable Publicizer001
                    if ((bool)instance.resolvedNetworkUserInstance) { instance.resolvedNetworkUserInstance.AwardLunarCoins(1); }
#pragma warning restore Publicizer001
                }

                /*
                var coinEffectPrefab = DeathRewards.coinEffectPrefab;
                Color32 color = new Color32(0, 0, 255, 255);
                EffectManager.SpawnEffect(coinEffectPrefab, new EffectData
                {
                    origin = PlayerCharacterMasterController.instances[0].master.GetBody().corePosition,
                    genericFloat = 20f,
                    scale = PlayerCharacterMasterController.instances[0].master.GetBody().radius,
                    color = color
                }, transmit: true);
                */
            });
        }

        private static void StartingCoins(On.RoR2.Run.orig_OnUserAdded orig, Run self, NetworkUser user)
        {
            user.DeductLunarCoins(user.lunarCoins);
            user.AwardLunarCoins(startingCoins.Value);
            orig(self, user);
        }
        
    }
}
