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
using static Facepunch.Steamworks.LobbyList.Filter;

namespace SimpleLunarCoins
{
	public class Hooks
	{
        public static void Init()
        {
            // Changing coin drop chance
            On.RoR2.PlayerCharacterMasterController.Awake += InitialCoinChance;

            // Changing chance multiplier & preventing coin droplet, instead spawning coin effect 
            BindingFlags allFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly;
            var initDelegate = typeof(PlayerCharacterMasterController).GetNestedTypes(allFlags)[0].GetMethodCached(name: "<Init>b__72_0");
            MonoMod.RuntimeDetour.HookGen.HookEndpointManager.Modify(initDelegate, (Action<ILContext>)CoinDropHook);

            // Setting coins at start of run
            On.RoR2.Run.OnUserAdded += StartingCoins;

            // Lunar coin distribution for droplet (distribution for effect type is built into CoinDropHook)
            On.RoR2.LunarCoinDef.GrantPickup += RegularCoinDistribute;
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
                c.Index += 14;
                c.Next.Operand = SimpleLunarCoins.coinMultiplier.Value;
                if (SimpleLunarCoins.noCoinDroplet.Value)
                {
                    c.Index -= 3;
                    c.MarkLabel(label);
                    c.Index -= 11;
                    c.Emit(OpCodes.Br, label);
                    c.Index += 16;
                    c.Emit(OpCodes.Ldarg_1);
                    c.EmitDelegate<Action<DamageReport>>((damageReport) =>
                    {
                        if (SimpleLunarCoins.teamCoins.Value)
                        {
                            foreach (PlayerCharacterMasterController instance in PlayerCharacterMasterController.instances)
                            {
                                if ((bool)instance.GetFieldValue<NetworkUser>("resolvedNetworkUserInstance"))
                                {
                                    instance.GetFieldValue<NetworkUser>("resolvedNetworkUserInstance").AwardLunarCoins(1);
                                }
                            }
                        }
                        else
                        {
                            if ((bool)damageReport.attackerMaster.playerCharacterMasterController)
                            {
                                damageReport.attackerMaster.playerCharacterMasterController.GetFieldValue<NetworkUser>("resolvedNetworkUserInstance").AwardLunarCoins(1);
                            }
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
            }
            else { Log.Info("ILHook failed"); }
        }

        private static void StartingCoins(On.RoR2.Run.orig_OnUserAdded orig, Run self, NetworkUser user)
        {

            if (SimpleLunarCoins.resetCoins.Value)
            {
                user.InvokeMethod("RpcDeductLunarCoins", user.lunarCoins);
                user.InvokeMethod("RpcAwardLunarCoins", SimpleLunarCoins.startingCoins.Value);
            }
            orig(self, user);
        }

        private static void RegularCoinDistribute(On.RoR2.LunarCoinDef.orig_GrantPickup orig, LunarCoinDef self, ref PickupDef.GrantContext context)
        {
            if (SimpleLunarCoins.teamCoins.Value)
            {
                NetworkUser networkUser = Util.LookUpBodyNetworkUser(context.body);
                foreach (PlayerCharacterMasterController instance in PlayerCharacterMasterController.instances)
                {
                    if ((bool)instance.GetFieldValue<NetworkUser>("resolvedNetworkUserInstance") && instance.GetFieldValue<NetworkUser>("resolvedNetworkUserInstance") != networkUser)
                    {
                        instance.GetFieldValue<NetworkUser>("resolvedNetworkUserInstance").AwardLunarCoins(1);
                    }
                }
            }
            orig(self, ref context);
        }
    }
}