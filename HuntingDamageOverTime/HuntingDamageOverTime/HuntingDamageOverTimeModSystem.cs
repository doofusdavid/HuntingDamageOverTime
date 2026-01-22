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
        public const float BLEEDING_DAMAGE_PER_SECOND = 0.5f;
        public const float BLEEDING_DURATION_SECONDS = 30f;

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
        }

        private void OnEntitySpawn(Entity entity)
        {
            // Add the bleeding behavior to all entities that can take damage
            if (!entity.HasBehavior<EntityBehaviorBleedingDamage>())
            {
                entity.AddBehavior(new EntityBehaviorBleedingDamage(entity));
            }
        }
    }

    public class EntityBehaviorBleedingDamage : EntityBehavior
    {
        private float remainingBleedTime = 0f;
        private float timeSinceLastTick = 0f;

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

            // Debug: Log all damage received
            string sourceInfo = dmgSource?.Source.ToString() ?? "null";
            string sourceEntityCode = dmgSource?.SourceEntity?.Code?.Path ?? "null";
            string damageType = dmgSource?.Type.ToString() ?? "null";
            entity.Api.Logger.Notification($"[HuntingDamageOverTime] {entity.Code?.Path ?? "unknown"} received {damage} damage. Source: {sourceInfo}, SourceEntity: {sourceEntityCode}, Type: {damageType}");

            // Check if damage is from a ranged weapon (arrow or spear)
            if (IsProjectileDamage(dmgSource))
            {
                // Reset/extend the bleeding duration
                remainingBleedTime = HuntingDamageOverTimeModSystem.BLEEDING_DURATION_SECONDS;

                entity.Api.Logger.Notification($"[HuntingDamageOverTime] PROJECTILE HIT! {entity.Code?.Path ?? "unknown"} will bleed for {remainingBleedTime}s");
            }
        }

        private bool IsProjectileDamage(DamageSource dmgSource)
        {
            // Check if the damage has a source entity (the projectile)
            // The source can be either Player (player shot it) or Entity (other entity shot it)
            if (dmgSource?.SourceEntity != null)
            {
                // Check if the source entity is an arrow or spear projectile
                string entityCode = dmgSource.SourceEntity.Code?.Path ?? "";
                entity.Api.Logger.Notification($"[HuntingDamageOverTime] Checking entity code: '{entityCode}'");
                bool isProjectile = entityCode.Contains("arrow") || entityCode.Contains("spear");
                entity.Api.Logger.Notification($"[HuntingDamageOverTime] Is projectile: {isProjectile}");
                return isProjectile;
            }

            entity.Api.Logger.Notification($"[HuntingDamageOverTime] No source entity found");
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
                    float damageToApply = HuntingDamageOverTimeModSystem.BLEEDING_DAMAGE_PER_SECOND;
                    float healthBefore = entity.WatchedAttributes.GetFloat("health", 0);

                    DamageSource bleedDamage = new DamageSource
                    {
                        Source = EnumDamageSource.Internal,
                        Type = EnumDamageType.Injury
                    };

                    bool damageApplied = entity.ReceiveDamage(bleedDamage, damageToApply);
                    float healthAfter = entity.WatchedAttributes.GetFloat("health", 0);

                    remainingBleedTime -= timeSinceLastTick;
                    timeSinceLastTick = 0f;

                    entity.Api.Logger.Notification($"[HuntingDamageOverTime] BLEEDING TICK! {entity.Code?.Path ?? "unknown"}: Applied {damageToApply} damage. Health: {healthBefore:F1} -> {healthAfter:F1}. Damage accepted: {damageApplied}. Remaining bleed time: {remainingBleedTime:F1}s");
                }
            }
        }

        public override void OnEntityDeath(DamageSource damageSourceForDeath)
        {
            base.OnEntityDeath(damageSourceForDeath);

            // Clear bleeding on death
            remainingBleedTime = 0f;
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
