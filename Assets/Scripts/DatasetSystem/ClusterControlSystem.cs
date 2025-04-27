// Scripts/ECS/Systems/ClusterControlSystem.cs
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Transforms;
using UnityEngine;

[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateBefore(typeof(TransformSystemGroup))]
public partial class ClusterControlSystem : SystemBase
{
    private EntityQuery _clusterQuery;
    private EntityQuery _requestQuery;
    private EndSimulationEntityCommandBufferSystem _endSimECBSystem;

    protected override void OnCreate()
    {
        base.OnCreate();
        _clusterQuery = GetEntityQuery(ComponentType.ReadOnly<ClusterTagComponent>());
        _requestQuery = GetEntityQuery(ComponentType.ReadOnly<ClusterControlRequest>());
        _endSimECBSystem = World.GetOrCreateSystemManaged<EndSimulationEntityCommandBufferSystem>();
        RequireForUpdate(_requestQuery);
        Debug.Log("ClusterControlSystem created.");
    }

    protected override void OnUpdate()
    {
        // Get all cluster entities - needed for index lookup
        // Use Allocator.Temp as it's short-lived
        NativeArray<Entity> clusterEntities = _clusterQuery.ToEntityArray(Allocator.Temp);

        // Get all request entities and their data
        NativeArray<Entity> requestEntities = _requestQuery.ToEntityArray(Allocator.Temp);
        NativeArray<ClusterControlRequest> requests = _requestQuery.ToComponentDataArray<ClusterControlRequest>(Allocator.Temp);

        // Create ECB for commands
        EntityCommandBuffer ecb = _endSimECBSystem.CreateCommandBuffer();

        // Get necessary lookups (read-only is fine for checking)
        ComponentLookup<Disabled> disabledLookup = GetComponentLookup<Disabled>(true);
        BufferLookup<Child> linkedEntityGroupLookup = GetBufferLookup<Child>(true);

        // Iterate through requests manually
        for (int i = 0; i < requestEntities.Length; i++)
        {
            Entity requestEntity = requestEntities[i];
            ClusterControlRequest request = requests[i];
            int targetIndex = request.TargetClusterIndex;
            bool enable = request.Enable;

            // Validate index
            if (targetIndex >= 0 && targetIndex < clusterEntities.Length)
            {
                Entity targetClusterEntity = clusterEntities[targetIndex];

                // Check if cluster exists (can do this before ECB commands)
                if (EntityManager.Exists(targetClusterEntity))
                {
                    // --- Toggle Parent ---
                    if (enable) {
                        if (disabledLookup.HasComponent(targetClusterEntity)) {
                            ecb.RemoveComponent<Disabled>(targetClusterEntity);
                        }
                    } else {
                        if (!disabledLookup.HasComponent(targetClusterEntity)) {
                            ecb.AddComponent<Disabled>(targetClusterEntity);
                        }
                    }

                    // --- Toggle Children ---
                    if (linkedEntityGroupLookup.HasBuffer(targetClusterEntity))
                    {
                        DynamicBuffer<Child> children = linkedEntityGroupLookup[targetClusterEntity];
                        for (int childIdx = 1; childIdx < children.Length; childIdx++) // Start from 1 to skip parent self-ref
                        {
                            Entity childEntity = children[childIdx].Value;
                            // Check if child exists before queuing command
                            if (EntityManager.Exists(childEntity))
                            {
                                if (enable) {
                                    if (disabledLookup.HasComponent(childEntity)) {
                                        ecb.RemoveComponent<Disabled>(childEntity);
                                    }
                                } else {
                                    if (!disabledLookup.HasComponent(childEntity)) {
                                        ecb.AddComponent<Disabled>(childEntity);
                                    }
                                }
                            }
                        }
                    } else {
                         Debug.LogWarning($"ClusterControlSystem: Target cluster {targetClusterEntity} (Index: {targetIndex}) missing LinkedEntityGroup buffer. Cannot toggle children.");
                    }
                } else {
                     Debug.LogWarning($"ClusterControlSystem: Target cluster entity for index {targetIndex} does not exist.");
                }
            } else {
                 Debug.LogWarning($"ClusterControlSystem: Received request for invalid cluster index {targetIndex}. Max index is {clusterEntities.Length - 1}.");
            }

            // Destroy the request entity
            ecb.DestroyEntity(requestEntity);
        }

        // Dispose temporary arrays
        clusterEntities.Dispose();
        requestEntities.Dispose();
        requests.Dispose();

        // Add dependency to ECB system (no jobs scheduled here directly)
        _endSimECBSystem.AddJobHandleForProducer(this.Dependency);

        // No need to update this.Dependency here as no jobs were scheduled by this system directly
    }
}
