// CellSpawnSystem.cs
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;

[BurstCompile]
public partial struct CellSpawnSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        // Only run once, when a spawner exists
        state.RequireForUpdate<CellSpawnerSettings>();
    }

    public void OnUpdate(ref SystemState state)
    {
        var ecb = new EntityCommandBuffer(Allocator.Temp);

        // Query for the single spawner entity
        foreach (var (settings, buffer, entity) in
                 SystemAPI.Query<RefRO<CellSpawnerSettings>, DynamicBuffer<CellPosition>>()
                     .WithEntityAccess())
        {
            int spawned = 0;

            // Iterate your baked positions
            foreach (var elem in buffer)
            {
                if (elem.cluster != settings.ValueRO.ClusterToSpawn)
                    continue;

                if (settings.ValueRO.limitCellSpawn && spawned++ >= settings.ValueRO.cellCountToSpawn)
                    break;

                // Instantiate & position
                var instance = ecb.Instantiate(settings.ValueRO.CellPrefabEntity);
                float3 worldPos = new float3(elem.coords.x, 0f, elem.coords.y)
                                  / settings.ValueRO.normalizationFactor
                                  + settings.ValueRO.offset;
                //ecb.SetComponent(instance, new Translation { Value = worldPos });
            }

            // Tear down the spawner so it only runs once
            ecb.DestroyEntity(entity);
        }

        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }
}