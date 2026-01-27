using System;
using System.Collections.Generic;
using HarmonyLib;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Server;

namespace HuntingDamageOverTime
{
    public class HuntingDamageOverTimeModSystem : ModSystem
    {
        // Configurable constants
        public const float BLEEDING_DAMAGE_PERCENT = 50f;  // Percentage of weapon damage dealt as total bleed
        public const float BLEEDING_DURATION_SECONDS = 15f;

        private static ICoreServerAPI? serverApi;
        private static Dictionary<long, BleedingState> bleedingEntities = new();
        private Harmony? harmony;

        public override void StartServerSide(ICoreServerAPI api)
        {
            base.StartServerSide(api);
            serverApi = api;

            // Apply Harmony patches
            harmony = new Harmony("com.huntingdamageovertime");
            harmony.PatchAll();

            // Register tick handler
            api.Event.RegisterGameTickListener(OnServerTick, 1000); // Every second
            api.Event.OnEntityDeath += OnEntityDeath;

            Mod.Logger.Notification("Hunting Damage Over Time mod loaded (server-side)");
        }

        public override void Dispose()
        {
            harmony?.UnpatchAll("com.huntingdamageovertime");
            bleedingEntities.Clear();
            serverApi = null;
            base.Dispose();
        }

        private void OnServerTick(float deltaTime)
        {
            if (serverApi == null) return;

            var toRemove = new List<long>();

            foreach (var kvp in bleedingEntities)
            {
                var state = kvp.Value;
                state.RemainingTime -= deltaTime;

                if (state.RemainingTime <= 0)
                {
                    toRemove.Add(kvp.Key);
                    continue;
                }

                // Get the entity
                if (!serverApi.World.LoadedEntities.TryGetValue(kvp.Key, out var entity))
                {
                    toRemove.Add(kvp.Key);
                    continue;
                }

                if (!entity.Alive)
                {
                    toRemove.Add(kvp.Key);
                    continue;
                }

                // Apply bleed damage
                float damageToApply = Math.Max(0.0001f, state.DamagePerSecond);

                DamageSource bleedDamage = new DamageSource
                {
                    Source = EnumDamageSource.Internal,
                    Type = EnumDamageType.Injury,
                    IgnoreInvFrames = true
                };

                entity.ReceiveDamage(bleedDamage, damageToApply);

                serverApi.Logger.Debug($"[HuntingDamageOverTime] {entity.Code?.Path ?? "unknown"} bleeding: {damageToApply} damage, {state.RemainingTime:F1}s remaining");
            }

            foreach (var id in toRemove)
            {
                bleedingEntities.Remove(id);
            }
        }

        private void OnEntityDeath(Entity entity, DamageSource? damageSource)
        {
            bleedingEntities.Remove(entity.EntityId);
        }

        public static void ApplyBleeding(Entity entity, float damage)
        {
            if (serverApi == null) return;
            if (!IsHarvestableAnimal(entity)) return;

            float damagePerSecond = damage * (BLEEDING_DAMAGE_PERCENT / 100f / BLEEDING_DURATION_SECONDS);

            bleedingEntities[entity.EntityId] = new BleedingState
            {
                DamagePerSecond = damagePerSecond,
                RemainingTime = BLEEDING_DURATION_SECONDS
            };

            serverApi.Logger.Notification($"[HuntingDamageOverTime] {entity.Code?.Path ?? "unknown"} hit by projectile for {damage} damage, bleeding {damagePerSecond}/s for {BLEEDING_DURATION_SECONDS}s");
        }

        private static bool IsHarvestableAnimal(Entity entity)
        {
            if (!entity.IsCreature) return false;
            if (entity is EntityPlayer) return false;
            return entity.HasBehavior("harvestable");
        }

        private class BleedingState
        {
            public float DamagePerSecond;
            public float RemainingTime;
        }
    }

    [HarmonyPatch(typeof(Entity), nameof(Entity.ReceiveDamage))]
    public static class EntityReceiveDamagePatch
    {
        public static void Postfix(Entity __instance, DamageSource damageSource, float damage, bool __result)
        {
            // Only process if damage was actually applied
            if (!__result) return;

            // Only process on server side
            if (__instance.World.Side != EnumAppSide.Server) return;

            // Check if damage is from a projectile (arrow or spear)
            if (damageSource?.SourceEntity != null)
            {
                string entityCode = damageSource.SourceEntity.Code?.Path ?? "";
                if (entityCode.Contains("arrow") || entityCode.Contains("spear"))
                {
                    HuntingDamageOverTimeModSystem.ApplyBleeding(__instance, damage);
                }
            }
        }
    }
}
