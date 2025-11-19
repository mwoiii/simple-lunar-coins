using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using Newtonsoft.Json;
using R2API.Utils;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace SimpleLunarCoins {
    public class Hooks {
        private static GameObject coinPrefab = Assets.mainAssetBundle.LoadAssetAsync<GameObject>("LunarCoinEmitter").asset as GameObject;
        public static void Init() {
            // Changing coin drop chance
            On.RoR2.PlayerCharacterMasterController.Awake += InitialCoinChance;


            // Changing chance multiplier & preventing coin droplet, instead spawning coin effect
            // thank you ephemeral coins
            BindingFlags allFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly;
            var initDelegate = typeof(PlayerCharacterMasterController).GetNestedTypes(allFlags)[0].GetMethodCached(name: "<Init>b__85_0");
            MonoMod.RuntimeDetour.HookGen.HookEndpointManager.Modify(initDelegate, (Action<ILContext>)CoinDropHook);

            // Loading soundbanks for coin flip noise
            On.RoR2.Run.Awake += InitSoundbanks;

            // Setting coins at start of run
            On.RoR2.Run.Start += StartingCoins;

            // Lunar coin distribution for droplet (distribution for effect type is built into CoinDropHook)
            On.RoR2.LunarCoinDef.GrantPickup += RegularCoinDistribute;


            // ProperSave compatibility
            if (ProperSaveCompatibility.enabled) {
                ProperSaveCompatibility.AddEvent(SaveCoins);
            }
        }

        private static void InitialCoinChance(On.RoR2.PlayerCharacterMasterController.orig_Awake orig, PlayerCharacterMasterController self) {
            orig(self);
            self.SetFieldValue("lunarCoinChanceMultiplier", SimpleLunarCoins.coinChance.Value);
        }

        private static void CoinDropHook(ILContext il) {
            var c = new ILCursor(il);

            // Custom coin & auto-collect
            var matched = c.TryGotoNext(
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
                x => x.MatchCallOrCallvirt<PickupDropletController>("CreatePickupDroplet")
                );

            if (matched) {

                c.Index += 11;
                c.Emit(OpCodes.Ldarg_1);
                c.Index -= 1;
                var label = c.DefineLabel();
                c.MarkLabel(label);
                c.Index -= 11;
                c.EmitDelegate<Func<bool>>(() => {
                    return SimpleLunarCoins.noCoinDroplet.Value;
                });
                c.Emit(OpCodes.Brtrue, label);
                c.Index += 12;
                c.EmitDelegate<Action<DamageReport>>((damageReport) => {
                    if (SimpleLunarCoins.noCoinDroplet.Value) {
                        if (SimpleLunarCoins.teamCoins.Value) {
                            foreach (PlayerCharacterMasterController instance in PlayerCharacterMasterController.instances) {
                                if ((bool)instance.GetFieldValue<NetworkUser>("resolvedNetworkUserInstance")) {
                                    instance.GetFieldValue<NetworkUser>("resolvedNetworkUserInstance").AwardLunarCoins(1);
                                }
                            }
                        } else {
                            if ((bool)damageReport.attackerMaster.playerCharacterMasterController) {
                                damageReport.attackerMaster.playerCharacterMasterController.GetFieldValue<NetworkUser>("resolvedNetworkUserInstance").AwardLunarCoins(1);
                            }
                        }

                        EffectManager.SpawnEffect(coinPrefab, new EffectData {
                            origin = damageReport.victimBody.corePosition,
                            genericFloat = 20f,
                            scale = damageReport.victimBody.radius
                        }, transmit: true);
                    }
                });
            } else { Log.Warning("Custom coin drop ILHook failed, likely due to a conflict. This feature will not work as intended."); }

            // Setting coin chance multiplier
            matched = c.TryGotoNext(
                x => x.MatchLdloc(1),
                x => x.MatchDup(),
                x => x.MatchLdfld<PlayerCharacterMasterController>("lunarCoinChanceMultiplier"),
                x => x.MatchLdcR4(0.5f),
                x => x.MatchMul(),
                x => x.MatchStfld<PlayerCharacterMasterController>("lunarCoinChanceMultiplier")
                );

            if (matched) {
                c.Index += 4;
                c.Emit(OpCodes.Pop);
                c.EmitDelegate<Func<float>>(() =>  // Delegate lets it change dynamically (RoO)
                {
                    return SimpleLunarCoins.coinMultiplier.Value;
                });
            } else { Log.Warning("Coin chance multiplier ILHook failed, likely due to a conflict. This feature will not work as intended."); }

        }

        /*
        private static void StartingCoins(On.RoR2.Run.orig_OnUserAdded orig, Run self, NetworkUser user)
        {

            if (SimpleLunarCoins.resetCoins.Value)
            {
                user.InvokeMethod("RpcDeductLunarCoins", user.lunarCoins);
                user.InvokeMethod("RpcAwardLunarCoins", (uint)SimpleLunarCoins.startingCoins.Value);
            }
            orig(self, user);
        }
        */


        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        private static void StartingCoins(On.RoR2.Run.orig_Start orig, Run self) {
            if (!NetworkServer.active) {
                orig(self);
                return;
            }

            bool isLoading = false;
            if (ProperSaveCompatibility.enabled) {
                if (ProperSaveCompatibility.IsLoading) { isLoading = true; }
            }

            if (!isLoading && SimpleLunarCoins.resetCoins.Value) {

                foreach (var user in NetworkUser.readOnlyInstancesList) {
                    user.DeductLunarCoins(user.lunarCoins);
                    user.AwardLunarCoins((uint)SimpleLunarCoins.startingCoins.Value);
                }
            } else if (isLoading) {
                string jsonString = ProperSaveCompatibility.GetModdedData("SimpleLunarCoinsObj");

                var playerCoins = JsonConvert.DeserializeObject<Dictionary<string, uint>>(jsonString);

                foreach (var user in NetworkUser.readOnlyInstancesList) {
                    if (playerCoins.ContainsKey(user.GetNetworkPlayerName().GetResolvedName())) {
                        user.DeductLunarCoins(user.lunarCoins);
                        user.AwardLunarCoins((uint)playerCoins[user.GetNetworkPlayerName().GetResolvedName()]);
                    }
                }
            }
            orig(self);
        }


        private static void SaveCoins(Dictionary<string, object> dict) {
            Dictionary<string, uint> playerCoins = [];
            foreach (var user in NetworkUser.instancesList) {
                playerCoins.Add(user.GetNetworkPlayerName().GetResolvedName(), user.lunarCoins);
            }
            string jsonString = JsonConvert.SerializeObject(playerCoins);
            dict.Add("SimpleLunarCoinsObj", jsonString);
        }

        private static void RegularCoinDistribute(On.RoR2.LunarCoinDef.orig_GrantPickup orig, LunarCoinDef self, ref PickupDef.GrantContext context) {
            if (SimpleLunarCoins.teamCoins.Value) {
                NetworkUser networkUser = Util.LookUpBodyNetworkUser(context.body);
                foreach (PlayerCharacterMasterController instance in PlayerCharacterMasterController.instances) {
                    if ((bool)instance.GetFieldValue<NetworkUser>("resolvedNetworkUserInstance") && instance.GetFieldValue<NetworkUser>("resolvedNetworkUserInstance") != networkUser) {
                        instance.GetFieldValue<NetworkUser>("resolvedNetworkUserInstance").AwardLunarCoins(1);
                    }
                }
            }
            orig(self, ref context);
        }

        private static void InitSoundbanks(On.RoR2.Run.orig_Awake orig, Run self) {
            orig(self);
            SoundBanks.Init();
        }
    }
}