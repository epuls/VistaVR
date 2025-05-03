// Scripts/ECS/Systems/CellSpawningSystem.cs
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine; // For Debug.Log

// Ensure this system runs before the transform systems that use the Parent component
[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateBefore(typeof(TransformSystemGroup))]
public partial class CellSpawningSystem : SystemBase
{
    // --- State ---
    private bool _spawnRequested = false;
    private NativeArray<CellData> _dataToSpawn;
    private Entity _cellPrefabToSpawn;
    private Entity _clusterPrefabToSpawn;
    private Entity _grandparentEntity;
    private int _clusterCount;
    // --- End State ---

    // --- Use an earlier ECB system ---
    private BeginSimulationEntityCommandBufferSystem _beginSimECBSystem; // Changed variable name and type

    protected override void OnCreate()
    {
        base.OnCreate();
        // Get the handle to the BeginSimulation ECB system
        _beginSimECBSystem = World.GetOrCreateSystemManaged<BeginSimulationEntityCommandBufferSystem>(); // Changed system type
        Debug.Log("CellSpawningSystem created and BeginSimulation ECB system referenced.");
    }

    public bool RequestSpawn(NativeArray<CellData> data, Entity cellPrefab, Entity clusterPrefab, int clusterCount, Entity grandparent)
    {
        // ... (RequestSpawn logic remains the same) ...
        if (_spawnRequested) { /* ... */ return false; }
        if (!data.IsCreated || /* ... */ !EntityManager.Exists(grandparent)) { /* ... */ return false; }
        _dataToSpawn = data;
        _cellPrefabToSpawn = cellPrefab;
        _clusterPrefabToSpawn = clusterPrefab;
        _clusterCount = clusterCount;
        _grandparentEntity = grandparent;
        _spawnRequested = true;
        return true;
    }


    protected override void OnUpdate()
    {
        if (!_spawnRequested) { return; }

        JobHandle currentDeps = this.Dependency;
        bool persistentDisposalScheduled = false;
        NativeArray<Entity> cellEntities = default;
        NativeArray<Entity> clusterEntities = default;

        try
        {
            // --- Final Input Validation ---
            if (!_dataToSpawn.IsCreated || /* ... */ !_grandparentEntity.Equals(Entity.Null) && !EntityManager.Exists(_grandparentEntity) || /* ... */ _clusterCount <= 0)
            {
                Debug.LogError("CellSpawningSystem: Invalid data/prefab/grandparent/count during OnUpdate. Disposing data.");
                if (_dataToSpawn.IsCreated) _dataToSpawn.Dispose();
                _spawnRequested = false;
                return;
            }

            // --- Create Command Buffer from the earlier system ---
            EntityCommandBuffer ecb = _beginSimECBSystem.CreateCommandBuffer(); // Use the referenced system

            // --- Instantiate Entities ---
            // 1. Instantiate Cluster Entities
            clusterEntities = EntityManager.Instantiate(_clusterPrefabToSpawn, _clusterCount, Allocator.TempJob);
            // 2. Parent Clusters using ECB
            for (int i = 0; i < clusterEntities.Length; i++)
            {
                ecb.AddComponent(clusterEntities[i], new Parent { Value = _grandparentEntity });
            }
            // 3. Instantiate Cell Entities
            cellEntities = EntityManager.Instantiate(_cellPrefabToSpawn, _dataToSpawn.Length, Allocator.TempJob);

            // --- Prepare and Schedule Jobs ---
            // 4. Prepare SetCellDataJob
            var setCellDataJob = new SetCellDataJob {
                SourceCellData = _dataToSpawn,
                Entities = cellEntities,
                LocalTransformLookup = GetComponentLookup<LocalTransform>(false),
                CellComponentLookup = GetComponentLookup<CellComponent>(false)
             };
            JobHandle cellDataJobHandle = setCellDataJob.Schedule(cellEntities.Length, 64, currentDeps);

            // 5. Prepare SetParentJob (parents cells to clusters)
            var setParentJob = new SetParentJob {
                CellEntities = cellEntities,
                ClusterEntities = clusterEntities,
                CellComponentLookup = GetComponentLookup<CellComponent>(true),
                LinkedEntityGroupLookup = GetBufferLookup<LinkedEntityGroup>(true),
                ECB = ecb.AsParallelWriter() // Pass parallel writer
             };
            JobHandle parentJobHandle = setParentJob.Schedule(cellEntities.Length, 64, cellDataJobHandle);

            // --- Schedule Disposals & Update Dependency ---
            // 6. Schedule disposal of cells (depends on SetParentJob)
            JobHandle combinedHandle = cellEntities.Dispose(parentJobHandle);
            // 7. Schedule disposal of clusters (depends on cell disposal)
            combinedHandle = clusterEntities.Dispose(combinedHandle);
            // 8. Schedule disposal of persistent data (depends on cluster disposal)
            combinedHandle = _dataToSpawn.Dispose(combinedHandle);
            persistentDisposalScheduled = true;

            // 9. Add the final job handle to the ECB system's dependencies.
            _beginSimECBSystem.AddJobHandleForProducer(combinedHandle); // Add to the correct ECB system

            // 10. Update this system's dependency with the FULL chain.
            this.Dependency = combinedHandle;

            // Debug.Log($"CellSpawningSystem: Processed spawn request, scheduled jobs and disposals.");
        }
        catch (System.Exception ex)
        {
             Debug.LogException(ex);
             // ... (Error handling disposal logic remains the same) ...
             if (!persistentDisposalScheduled && _dataToSpawn.IsCreated) { _dataToSpawn.Dispose(); }
             if (cellEntities.IsCreated) { cellEntities.Dispose(); }
             if (clusterEntities.IsCreated) { clusterEntities.Dispose(); }
        }
        finally
        {
            _spawnRequested = false;
        }
    }

     protected override void OnDestroy()
     {
         // ... (OnDestroy logic remains the same) ...
         if (_spawnRequested && _dataToSpawn.IsCreated) { this.CompleteDependency(); if (_dataToSpawn.IsCreated) _dataToSpawn.Dispose(); }
         base.OnDestroy();
     }
}
