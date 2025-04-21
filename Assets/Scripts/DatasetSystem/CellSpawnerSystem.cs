// Scripts/ECS/Systems/CellSpawningSystem.cs
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine; // For Debug.Log

// Run in the default simulation group
[UpdateInGroup(typeof(SimulationSystemGroup))]
// Optionally disable auto-creation if you want to add it manually
// [DisableAutoCreation]
public partial class CellSpawningSystem : SystemBase // Use SystemBase for convenience
{
    // This system doesn't need an OnUpdate if triggered externally,
    // but OnCreate/OnDestroy might be useful.
    protected override void OnCreate()
    {
        base.OnCreate();
        Debug.Log("CellSpawningSystem created.");
        // Require the CellSpawnerComponent singleton for safety, maybe?
        // RequireSingletonForUpdate<CellSpawnerComponent>(); // Ensures the spawner entity exists
    }

    protected override void OnUpdate()
    {
        // Normally empty if triggered externally via ScheduleSpawnJob
    }

    /// <summary>
    /// Public method called by CellSpawner MonoBehaviour to trigger spawning.
    /// </summary>
    /// <param name="dataToSpawn">The cell data converted by the MonoBehaviour.</param>
    /// <param name="entityPrefab">The baked entity prefab.</param>
    /// <param name="inputDeps">Job dependencies from the caller.</param>
    /// <returns>JobHandle for the scheduled job.</returns>
    public JobHandle ScheduleSpawnJob(NativeArray<CellData> dataToSpawn, Entity entityPrefab, JobHandle inputDeps)
    {
        if (!dataToSpawn.IsCreated || dataToSpawn.Length == 0)
        {
            Debug.LogWarning("CellSpawningSystem: Received empty or invalid data to spawn.");
            return inputDeps; // Return original dependencies
        }
        if (entityPrefab == Entity.Null)
        {
             Debug.LogError("CellSpawningSystem: Received invalid Entity Prefab.");
             return inputDeps;
        }
         if (!EntityManager.Exists(entityPrefab))
         {
              Debug.LogError($"CellSpawningSystem: Received Entity Prefab {entityPrefab} that does not exist.");
              return inputDeps;
         }


        // 1. Instantiate Entities
        // Use Allocator.TempJob for the temporary entity array within the system's scope
        NativeArray<Entity> entityArray = EntityManager.Instantiate(
            entityPrefab,
            dataToSpawn.Length,
            Allocator.TempJob
        );

        // 2. Prepare the Job
        // Get ComponentLookup instances using SystemBase's method
        var job = new SetCellDataJob
        {
            SourceCellData = dataToSpawn, // Pass the data array
            Entities = entityArray,       // Pass the new entities array

            // Get lookups with write access (false = not read-only)
            LocalTransformLookup = GetComponentLookup<LocalTransform>(false),
            CellComponentLookup = GetComponentLookup<CellComponent>(false)
        };

        // 3. Schedule the Job
        // Schedule the job to run in parallel, depending on inputDeps
        JobHandle jobHandle = job.Schedule(dataToSpawn.Length, 64, inputDeps);

        // --- IMPORTANT ---
        // Since the NativeArrays (dataToSpawn, entityArray) were allocated
        // with TempJob, they MUST be associated with a job handle to ensure
        // their disposal doesn't happen before the job completes.
        // By returning the jobHandle, the system's dependency manager
        // will handle this correctly. The caller MonoBehaviour doesn't need
        // to complete or dispose directly.

        Debug.Log($"CellSpawningSystem: Scheduled spawn job for {dataToSpawn.Length} entities.");

        // Return the handle for the scheduled job
        return jobHandle;
    }
}

