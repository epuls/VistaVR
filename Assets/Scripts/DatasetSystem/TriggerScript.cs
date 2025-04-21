// Example: Scripts/MonoBehaviours/TriggerScript.cs
using UnityEngine;
using Unity.Entities;
using Unity.Collections;
using System.Collections.Generic;
using Unity.Jobs; // For JobHandle

public class TriggerScript : MonoBehaviour
{
    // --- References needed ---
    private EntityManager entityManager;
    private CellSpawningSystem cellSpawningSystem;
    private Entity cellEntityPrefab = Entity.Null; // Store the prefab reference when found

    // --- Data Source ---
    public Dictionary<string, Vector2> cellPositions = new Dictionary<string, Vector2>();
    public Dictionary<string, int> cellClusters = new Dictionary<string, int>();

    private bool isInitialized = false;

    void Start()
    {
        Initialize();
        // Example: Load data right away
        LoadMyData();
        TriggerSpawn();
    }

    void Initialize()
    {
        if (isInitialized) return;

        World defaultWorld = World.DefaultGameObjectInjectionWorld;
        if (defaultWorld == null || !defaultWorld.IsCreated)
        {
            Debug.LogError("TriggerScript Error: Default World not found!", this.gameObject);
            this.enabled = false;
            return;
        }
        entityManager = defaultWorld.EntityManager;

        // Get the system reference
        cellSpawningSystem = defaultWorld.GetExistingSystemManaged<CellSpawningSystem>();
        if (cellSpawningSystem == null)
        {
            Debug.LogError("TriggerScript Error: CellSpawningSystem not found!", this.gameObject);
            this.enabled = false;
            return;
        }

        // Find the Spawner Entity using the tag component and get the prefab
        TryGetBakedPrefabReference();

        isInitialized = true;
    }

    // --- UPDATED METHOD ---
    void TryGetBakedPrefabReference()
    {
        // Ensure EntityManager is valid before querying
        if (entityManager == null)
        {
             Debug.LogError("TriggerScript: EntityManager not available during TryGetBakedPrefabReference.", this.gameObject);
             this.cellEntityPrefab = Entity.Null;
             return;
        }

        // Create a query for the singleton entity with the necessary components
        // We need the tag to find it and the CellSpawnerComponent to get the data.
        EntityQuery query = entityManager.CreateEntityQuery(
            ComponentType.ReadOnly<SpawnerTagComponent>(),      // Find via tag
            ComponentType.ReadOnly<CellSpawnerComponent>()    // Ensure data component exists
        );

        // Use TryGetSingletonEntity for safety (handles 0 or >1 entities)
        if (query.TryGetSingletonEntity<Entity>(out Entity spawnerEntity))
        {
            // Found the unique entity, now get the component data
            try
            {
                 // Check entity still exists (paranoid check, usually true if query succeeded)
                 if (entityManager.Exists(spawnerEntity)) {
                    CellSpawnerComponent spawnerComp = entityManager.GetComponentData<CellSpawnerComponent>(spawnerEntity);
                    this.cellEntityPrefab = spawnerComp.CellEntityPrefab;

                    if (this.cellEntityPrefab != Entity.Null)
                    {
                        Debug.Log($"TriggerScript: Found Spawner Entity {spawnerEntity} and got Cell Prefab {this.cellEntityPrefab}", this.gameObject);
                    }
                    else
                    {
                        Debug.LogWarning("TriggerScript: Found Spawner Entity but Cell Prefab reference is Null in its component.", this.gameObject);
                    }
                 } else {
                      Debug.LogWarning($"TriggerScript: Spawner entity {spawnerEntity} found by query but no longer exists.", this.gameObject);
                      this.cellEntityPrefab = Entity.Null;
                 }

            }
            catch (System.Exception ex)
            {
                 Debug.LogError($"TriggerScript: Error getting CellSpawnerComponent from entity {spawnerEntity}. Exception: {ex.Message}", this.gameObject);
                 this.cellEntityPrefab = Entity.Null;
            }
        }
        else
        {
            // Handle cases where the singleton wasn't found or wasn't unique
            int count = query.CalculateEntityCount(); // Check how many matched
             if (count == 0) {
                 Debug.LogWarning("TriggerScript: Could not find any Entity with SpawnerTagComponent and CellSpawnerComponent. Make sure the Spawner GameObject is in a Subscene and baked correctly.", this.gameObject);
             } else {
                 Debug.LogWarning($"TriggerScript: Found {count} entities matching query for SpawnerTagComponent/CellSpawnerComponent, but expected a singleton. Cannot reliably get prefab.", this.gameObject);
             }
            this.cellEntityPrefab = Entity.Null; // Ensure prefab is null if lookup fails
        }

        // Dispose of the query object to prevent memory leaks
        query.Dispose();
    }
    // --- END UPDATED METHOD ---


    // Example method to load your data - replace with your actual logic
    void LoadMyData()
    {
        /*
         if (cellPositions.Count == 0) // Avoid reloading example data
         {
            cellPositions.Clear();
            cellClusters.Clear();
            // --- Your CSV loading logic here ---
            // Populate cellPositions and cellClusters dictionaries
            // Example:
            cellPositions.Add("cell_T001", new Vector2(10f, 5f));
            cellClusters.Add("cell_T001", 2);
            cellPositions.Add("cell_T002", new Vector2(-3f, -2f));
            cellClusters.Add("cell_T002", 3);
            Debug.Log($"TriggerScript: Loaded data. {cellPositions.Count} positions found.", this.gameObject);
         }
         */
        
        cellPositions = CSVImporter.LoadCsvToDictionary("C:\\Users\\epuls\\OneDrive\\Desktop\\Visium_HD_Human_BC_FF_008um\\Spatial-Projection.csv");
        cellClusters =
            CSVImporter.LoadCsvToClusterDictionary(
                "C:\\Users\\epuls\\OneDrive\\Desktop\\Visium_HD_Human_BC_FF_008um\\Graph-Based.csv");
    }

    // --- Public method to be called by UI Button or other game logic ---
    public void TriggerSpawn()
    {
        // Ensure initialization has happened
        if (!isInitialized)
        {
            Debug.LogError("TriggerScript: Not initialized. Cannot trigger spawn.", this.gameObject);
            Initialize(); // Try to initialize again
            if (!isInitialized) return;
        }

        // Re-check prefab reference in case it wasn't ready during Initialize
        if (cellEntityPrefab == Entity.Null)
        {
            TryGetBakedPrefabReference(); // Attempt lookup again
            if (cellEntityPrefab == Entity.Null) // Check after attempt
            {
                 Debug.LogError("TriggerScript Error: Cell Entity Prefab reference is missing after check. Cannot trigger spawn.", this.gameObject);
                 return;
            }
        }
         // Final check on prefab existence in EntityManager
         if (!entityManager.Exists(cellEntityPrefab)) {
              Debug.LogError($"TriggerScript Error: Cell Entity Prefab {cellEntityPrefab} not found in EntityManager. Cannot trigger spawn.", this.gameObject);
              return;
         }


        if (cellSpawningSystem == null)
        {
             Debug.LogError("TriggerScript Error: CellSpawningSystem reference is missing. Cannot trigger spawn.", this.gameObject);
             return;
        }

        if (cellPositions.Count == 0)
        {
            Debug.LogWarning("TriggerScript: No position data loaded. Cannot trigger spawn.", this.gameObject);
            return;
        }

        Debug.Log($"TriggerScript: Triggering spawn for {cellPositions.Count} cells...", this.gameObject);

        // 1. Convert data (using the static utility)
        NativeArray<CellData> nativeCellData = DataConverter.ConvertToNativeArray(
            cellPositions,
            cellClusters,
            Allocator.TempJob // TempJob is fine, system manages dependency
        );

        if (!nativeCellData.IsCreated || nativeCellData.Length == 0)
        {
            Debug.LogError("TriggerScript Error: Failed to convert data or data is empty. Aborting spawn.", this.gameObject);
            if(nativeCellData.IsCreated) nativeCellData.Dispose();
            return;
        }

        // 2. Call the system's public method
        JobHandle spawnHandle = cellSpawningSystem.ScheduleSpawnJob(
            nativeCellData,
            cellEntityPrefab,
            default(JobHandle) // Pass default handle
        );

        Debug.Log($"TriggerScript: Spawn request sent to system.", this.gameObject);
    }

     // Optional: Clear method - better handled by a system potentially
     /*
     public void TriggerClear() { ... }
     */
}
