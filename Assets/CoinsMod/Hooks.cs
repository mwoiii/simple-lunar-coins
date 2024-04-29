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
	public class Hooks
	{
        public static void Init()
        {
            On.RoR2.PlayerCharacterMasterController.Awake += InitialCoinChance;

            BindingFlags allFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly;
            var initDelegate = typeof(PlayerCharacterMasterController).GetNestedTypes(allFlags)[0].GetMethodCached(name: "<Init>b__72_0");
            MonoMod.RuntimeDetour.HookGen.HookEndpointManager.Modify(initDelegate, (Action<ILContext>)CoinDropHook);

            On.RoR2.Run.OnUserAdded += StartingCoins;
        }

        private static void InitialCoinChance(On.RoR2.PlayerCharacterMasterController.orig_Awake orig, PlayerCharacterMasterController self)
        {
            orig(self);
            self.SetFieldValue("lunarCoinChanceMultiplier", SimpleLunarCoins.coinChance.Value);
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
                c.Next.Operand = SimpleLunarCoins.coinMultiplier.Value;
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

                    var coinEffectPrefab = myLoadedPrefab;
                    EffectManager.SpawnEffect(coinEffectPrefab, new EffectData
                    {
                        origin = damageReport.victimBody.corePosition,
                        genericFloat = 20f,
                        scale = damageReport.victimBody.radius
                    }, transmit: true);

                });
            }
            else { Log.Info("ILHook failed"); }
        }

        private static void StartingCoins(On.RoR2.Run.orig_OnUserAdded orig, Run self, NetworkUser user)
        {
            user.InvokeMethod("RpcDeductLunarCoins", user.lunarCoins);
            user.InvokeMethod("RpcAwardLunarCoins", SimpleLunarCoins.startingCoins.Value);
            orig(self, user);
        }
    }
}