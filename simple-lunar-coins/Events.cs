using Mono.Cecil.Cil;
using MonoMod.Cil;
using Newtonsoft.Json;
using R2API.Utils;
using RoR2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine.Networking;

namespace SimpleLunarCoins {
    public class Events {

        public static void Init() {
            On.RoR2.PlayerCharacterMasterController.Awake += InitialCoinChance;

            BindingFlags allFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly;
            var initDelegate = typeof(PlayerCharacterMasterController)
                .GetNestedTypes(allFlags)[0]
                .GetMethods(allFlags)
                .FirstOrDefault(method => method.Name.Contains("<Init>b__")); // very susceptible to changing between updates - current is "<Init>b__85_0";
            if (initDelegate != null) {
                MonoMod.RuntimeDetour.HookGen.HookEndpointManager.Modify(initDelegate, CoinDropHook);
            } else {
                Log.Error("Failed to find method for CoinDropHook! Mod is mostly broken!");
            }

            On.RoR2.Run.Start += StartingCoins;

            On.RoR2.LunarCoinDef.GrantPickup += RegularCoinDistribute;

            if (ProperSaveCompat.enabled) {
                ProperSaveCompat.AddEvent(SaveCoins);
            }
        }

        private static void InitialCoinChance(On.RoR2.PlayerCharacterMasterController.orig_Awake orig, PlayerCharacterMasterController self) {
            orig(self);
            self.lunarCoinChanceMultiplier = Options.coinChance.Value;
        }

        private static void CoinDropHook(ILContext il) {
            var c = new ILCursor(il);

            // Custom coin & auto-collect
            //var matched = c.TryGotoNext(
            //    x => x.MatchLdsfld(typeof(RoR2Content.MiscPickups).FullName, nameof(RoR2Content.MiscPickups.LunarCoin)),
            //    x => x.MatchLdfld<MiscPickupDef>("miscPickupIndex"),
            //    x => x.MatchCallOrCallvirt(typeof(PickupCatalog).FullName, nameof(PickupCatalog.FindPickupIndex)),
            //    x => x.MatchLdarg(1),
            //    x => x.MatchLdfld<DamageReport>("victim"),
            //    x => x.MatchCallOrCallvirt<Component>("get_transform"),
            //    x => x.MatchCallOrCallvirt<Transform>("get_position"),
            //    x => x.MatchCallOrCallvirt<Vector3>("get_up"),
            //    x => x.MatchLdcR4(10),
            //    x => x.MatchCallOrCallvirt<Vector3>("op_Multiply"),
            //    x => x.MatchCallOrCallvirt<PickupDropletController>("CreatePickupDroplet")
            //    );

            if (c.TryGotoNext(MoveType.Before, x => x.MatchLdsfld(typeof(RoR2Content.MiscPickups).FullName, nameof(RoR2Content.MiscPickups.LunarCoin)))) {
                ILLabel skipDropletLabel = c.DefineLabel();
                c.EmitDelegate<Func<bool>>(() => {
                    return Options.noCoinDroplet.Value;
                });
                c.Emit(OpCodes.Brtrue, skipDropletLabel);
                c.MarkLabel(skipDropletLabel);

                if (c.TryGotoNext(MoveType.After, x => x.MatchCallOrCallvirt<PickupDropletController>("CreatePickupDroplet"))) {
                    c.MarkLabel(skipDropletLabel);
                    c.Emit(OpCodes.Ldarg_1);
                    c.EmitDelegate<Action<DamageReport>>((damageReport) => {
                        if (Options.noCoinDroplet.Value) {
                            if (Options.teamCoins.Value) {
                                foreach (PlayerCharacterMasterController instance in PlayerCharacterMasterController.instances) {
                                    if (instance && instance.resolvedNetworkUserInstance) {
                                        instance.resolvedNetworkUserInstance.AwardLunarCoins(1);
                                    }
                                }
                            } else {
                                if (damageReport.attackerMaster && damageReport.attackerMaster.playerCharacterMasterController) {
                                    damageReport.attackerMaster.playerCharacterMasterController.resolvedNetworkUserInstance.AwardLunarCoins(1);
                                }
                            }
                            if (damageReport.victimBody) {
                                EffectManager.SpawnEffect(Assets.coinEmitterPrefab, new EffectData {
                                    origin = damageReport.victimBody.corePosition,
                                    scale = damageReport.victimBody.radius
                                }, transmit: true);
                            }
                        }
                    });
                } else {
                    Log.Error("Part 2 of CoinDropHook failed! No coin droplet and distribute coins will not work!");
                }
            } else {
                Log.Error("Part 1 of CoinDropHook failed! No coin droplet and distribute coins will not work!");
            }

            // Setting coin chance multiplier
            if (c.TryGotoNext(x => x.MatchLdfld<PlayerCharacterMasterController>("lunarCoinChanceMultiplier")) &&
                c.TryGotoNext(MoveType.After, x => x.MatchLdcR4(out _))) {
                c.Emit(OpCodes.Pop);
                c.EmitDelegate<Func<float>>(() => {
                    return Options.coinMultiplier.Value;
                });
            } else { Log.Warning("Part 3 of CoinDropHook failed! Coin chance multiplier override will not work!"); }
        }

        private static void StartingCoins(On.RoR2.Run.orig_Start orig, Run self) {
            if (!NetworkServer.active) {
                orig(self);
                return;
            }

            bool isLoading = false;
            if (ProperSaveCompat.enabled && ProperSaveCompat.IsLoading) {
                isLoading = true;
            }

            if (!isLoading && Options.resetCoins.Value) {

                foreach (var user in NetworkUser.readOnlyInstancesList) {
                    user.DeductLunarCoins(user.lunarCoins);
                    user.AwardLunarCoins((uint)Options.startingCoins.Value);
                }
            } else if (isLoading) {
                string jsonString = ProperSaveCompat.GetModdedData("SimpleLunarCoinsObj");

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
            if (Options.teamCoins.Value) {
                NetworkUser networkUser = Util.LookUpBodyNetworkUser(context.body);
                foreach (PlayerCharacterMasterController instance in PlayerCharacterMasterController.instances) {
                    if ((bool)instance.GetFieldValue<NetworkUser>("resolvedNetworkUserInstance") && instance.GetFieldValue<NetworkUser>("resolvedNetworkUserInstance") != networkUser) {
                        instance.GetFieldValue<NetworkUser>("resolvedNetworkUserInstance").AwardLunarCoins(1);
                    }
                }
            }
            orig(self, ref context);
        }
    }
}