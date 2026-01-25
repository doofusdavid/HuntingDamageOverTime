using System;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Server;

namespace HuntingDamageOverTime
{
    public class HuntingDamageOverTimeModSystem : ModSystem
    {
        // Configurable constants
        public const float BLEEDING_DAMAGE_PERCENT = 50f;  // Percentage of weapon damage dealt per second
        public const float BLEEDING_DURATION_SECONDS = 15f;

        public override void Start(ICoreAPI api)
        {
            base.Start(api);

            // Register the bleeding behavior
            api.RegisterEntityBehaviorClass("bleedingdamage", typeof(EntityBehaviorBleedingDamage));

            Mod.Logger.Notification("Hunting Damage Over Time mod loaded");
        }

        public override void StartServerSide(ICoreServerAPI api)
        {
            base.StartServerSide(api);

            // Hook into entity damage events
            api.Event.OnEntitySpawn += OnEntitySpawn;

            // Add the bleeding behavior to all wildlife animals that have already spawned
            foreach (var entity in api.World.LoadedEntities)
            {
                if (IsWildlifeAnimal(entity.Value))
                {
                    AddBleedingBehaviorToEntity(entity.Value);
                }
            }
        }

        private bool IsWildlifeAnimal(Entity entity)
        {
            // Exclude players
            if (entity is EntityPlayer) return false;

            // Exclude hostile mobs
            string entityCode = entity.Code?.Path.ToLower() ?? "";
            string[] hostileTypes = { "drifter", "locust", "zombie", "specter", "nightmare", "temp" };
            if (hostileTypes.Any(hostile => entityCode.Contains(hostile))) return false;

            // Include EntityAgent types (which includes wildlife animals)
            return entity is EntityAgent;
        }

        private void AddBleedingBehaviorToEntity(Entity entity)
        {
            if (!entity.HasBehavior<EntityBehaviorBleedingDamage>())
            {
                entity.AddBehavior(new EntityBehaviorBleedingDamage(entity));
            }
        }

        private void OnEntitySpawn(Entity entity)
        {
            // Add the bleeding behavior to wildlife animals only
            if (IsWildlifeAnimal(entity))
            {
                AddBleedingBehaviorToEntity(entity);
            }
        }
    }

    public class EntityBehaviorBleedingDamage : EntityBehavior
    {
        private float remainingBleedTime = 0f;
        private float timeSinceLastTick = 0f;
        private float bleedDamagePerSecond = 0f;

        public EntityBehaviorBleedingDamage(Entity entity) : base(entity)
        {
        }

        public override string PropertyName() => "bleedingdamage";

        public override void OnEntityReceiveDamage(DamageSource dmgSource, ref float damage)
        {
            base.OnEntityReceiveDamage(dmgSource, ref damage);

            // Only process on server side
            if (entity.World.Side != EnumAppSide.Server)
            {
                return;
            }

            // Check if damage is from a ranged weapon (arrow or spear)
            if (IsProjectileDamage(dmgSource))
            {
                // Calculate bleed damage as percentage of weapon damage
                bleedDamagePerSecond = damage * (HuntingDamageOverTimeModSystem.BLEEDING_DAMAGE_PERCENT / 100f / HuntingDamageOverTimeModSystem.BLEEDING_DURATION_SECONDS);

                // Reset/extend the bleeding duration
                remainingBleedTime = HuntingDamageOverTimeModSystem.BLEEDING_DURATION_SECONDS;

                entity.Api.Logger.Notification($"[HuntingDamageOverTime] {entity.Code?.Path ?? "unknown"} hit by projectile for {damage} damage, bleeding {bleedDamagePerSecond}/s for {remainingBleedTime}s");
            }
        }

        private bool IsProjectileDamage(DamageSource? dmgSource)
        {
            // Check if the damage has a source entity (the projectile)
            // The source can be either Player (player shot it) or Entity (other entity shot it)
            if (dmgSource?.SourceEntity != null)
            {
                // Check if the source entity is an arrow or spear projectile
                string entityCode = dmgSource.SourceEntity.Code?.Path ?? "";
                return entityCode.Contains("arrow") || entityCode.Contains("spear");
            }

            return false;
        }

        public override void OnGameTick(float deltaTime)
        {
            base.OnGameTick(deltaTime);

            // Only apply bleeding on server side
            if (entity.World.Side != EnumAppSide.Server)
            {
                return;
            }

            // If entity is bleeding, apply damage over time
            if (remainingBleedTime > 0)
            {
                timeSinceLastTick += deltaTime;

                // Apply damage every second
                if (timeSinceLastTick >= 1.0f)
                {
                    // Ensure minimum damage threshold
                    float damageToApply = Math.Max(0.0001f, bleedDamagePerSecond);

                    DamageSource bleedDamage = new DamageSource
                    {
                        Source = EnumDamageSource.Internal,
                        Type = EnumDamageType.Injury
                    };

                    entity.ReceiveDamage(bleedDamage, damageToApply);

                    remainingBleedTime -= timeSinceLastTick;
                    timeSinceLastTick = 0f;

                    entity.Api.Logger.Debug($"[HuntingDamageOverTime] {entity.Code?.Path ?? "unknown"} bleeding: {damageToApply} damage, {remainingBleedTime:F1}s remaining");
                }
            }
        }

        public override void OnEntityDeath(DamageSource damageSourceForDeath)
        {
            base.OnEntityDeath(damageSourceForDeath);

            // Clear bleeding on death
            remainingBleedTime = 0f;
            bleedDamagePerSecond = 0f;
        }

        public override void ToBytes(bool forClient)
        {
            base.ToBytes(forClient);
            // Sync bleeding state to clients if needed for visual effects
        }

        public override void FromBytes(bool isSync)
        {
            base.FromBytes(isSync);
        }
    }
}
