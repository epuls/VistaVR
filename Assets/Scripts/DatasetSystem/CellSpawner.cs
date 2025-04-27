// Scripts/MonoBehaviours/CellSpawner.cs
using UnityEngine;
using Unity.Entities;
using Unity.Collections;
using Unity.Mathematics;
using System.Collections.Generic;
using Unity.Jobs; // Needed for JobHandle
using Unity.Transforms; // Needed for LocalTransform lookup in system

public class CellSpawner : MonoBehaviour
{
    [HideInInspector]
    public Entity cellEntityPrefab = Entity.Null;

    [Header("Data Source (Assign or Load Manually)")]
    public Dictionary<string, Vector2> cellPositions = new Dictionary<string, Vector2>();
    public Dictionary<string, int> cellClusters = new Dictionary<string, int>();

    private EntityManager entityManager;
    private World defaultWorld;
    private CellSpawningSystem cellSpawningSystem; // Reference to the system
    private bool isInitialized = false;

    void Awake()
    {
        Initialize();
    }

    void Initialize()
    {
        if (isInitialized) return;

        cellPositions = CSVImporter.LoadCsvToDictionary("C:\\Users\\epuls\\OneDrive\\Desktop\\Visium_HD_Human_BC_FF_008um\\Spatial-Projection.csv");
        cellClusters =
            CSVImporter.LoadCsvToClusterDictionary(
                "C:\\Users\\epuls\\OneDrive\\Desktop\\Visium_HD_Human_BC_FF_008um\\Graph-Based.csv");

        defaultWorld = World.DefaultGameObjectInjectionWorld;
        if (defaultWorld == null || !defaultWorld.IsCreated)
        {
            Debug.LogError("CellSpawner Error: Default World not found or not created!", this.gameObject);
            this.enabled = false;
            return;
        }
        entityManager = defaultWorld.EntityManager;

        // Get a reference to the CellSpawningSystem
        cellSpawningSystem = defaultWorld.GetExistingSystemManaged<CellSpawningSystem>();
        if (cellSpawningSystem == null)
        {
             Debug.LogError("CellSpawner Error: CellSpawningSystem not found! Make sure it's created.", this.gameObject);
             this.enabled = false;
             return;
        }


        // --- Attempt to retrieve the baked entity prefab ---
        Entity selfEntity = GetSpawnerEntity(); // Uses placeholder GetSpawnerEntity

        if (selfEntity != Entity.Null && entityManager.HasComponent<CellSpawnerComponent>(selfEntity))
        {
            // Ensure GetComponentData is safe to call here if needed
            // Note: Accessing component data directly in Awake/Start can be tricky
            // depending on conversion/baking timing. Best practice often involves
            // systems initializing other systems/components.
            try {
                 if (entityManager.Exists(selfEntity)) { // Check entity exists before getting data
                    CellSpawnerComponent spawnerComp = entityManager.GetComponentData<CellSpawnerComponent>(selfEntity);
                    this.cellEntityPrefab = spawnerComp.CellEntityPrefab;
                    if (this.cellEntityPrefab != Entity.Null) {
                        Debug.Log($"CellSpawner: Successfully retrieved baked Cell Entity Prefab: {this.cellEntityPrefab}", this.gameObject);
                    } else {
                        Debug.LogWarning("CellSpawner: Retrieved CellSpawnerComponent, but the CellEntityPrefab within it is Null.", this.gameObject);
                    }
                 } else {
                     Debug.LogWarning("CellSpawner: Spawner entity does not exist when trying to get CellSpawnerComponent in Initialize.", this.gameObject);
                 }

            } catch (System.Exception ex) {
                 Debug.LogError($"CellSpawner: Error getting CellSpawnerComponent in Initialize. Is the entity valid? Exception: {ex.Message}", this.gameObject);
            }

        }
        else
        {
            Debug.LogWarning("CellSpawner: Could not find CellSpawnerComponent on the spawner's entity during Awake (or GetSpawnerEntity failed). Prefab check will occur during spawn attempt.", this.gameObject);
        }

        isInitialized = true;
    }

    // Placeholder - requires robust implementation if Awake retrieval is needed
    Entity GetSpawnerEntity()
    {
        // This remains a tricky part for MonoBehaviour->Entity linking without specific patterns.
        // Returning Null relies on later checks.
        Debug.LogWarning("GetSpawnerEntity() in CellSpawner is not fully implemented. Awake prefab retrieval might fail.", this.gameObject);
        return Entity.Null;
    }


    void Start()
    {
        if (!isInitialized) Initialize();
        if (this.enabled)
        {
           //PopulateExampleData();
        }
        
        SpawnCellsFromData();
    }
    

    /// <summary>
    /// Call this method manually to trigger the cell spawning process via the system.
    /// </summary>
    public void SpawnCellsFromData()
    {
        Debug.Log("Spawning Cells from Data");
        // Ensure system reference is valid
        if (cellSpawningSystem == null)
        {
             Debug.LogError("CellSpawner Error: CellSpawningSystem reference is missing. Cannot spawn.", this.gameObject);
             return;
        }

        // Check prefab validity (might have been set after Awake)
        if (cellEntityPrefab == Entity.Null)
        {
             // Maybe try getting it again now if Awake failed? This is fallback logic.
             Entity selfEntity = GetSpawnerEntity();
             if (selfEntity != Entity.Null && entityManager.Exists(selfEntity) && entityManager.HasComponent<CellSpawnerComponent>(selfEntity)) {
                  this.cellEntityPrefab = entityManager.GetComponentData<CellSpawnerComponent>(selfEntity).CellEntityPrefab;
             }

             if (cellEntityPrefab == Entity.Null) { // Check again
                  Debug.LogError("CellSpawner Error: Cannot spawn, Cell Entity Prefab is Null. Check Baker/Authoring setup and ensure initialization order.", this.gameObject);
                  return;
             }
        }
         // Final check on prefab existence in EntityManager
         if (!entityManager.Exists(cellEntityPrefab)) {
              Debug.LogError($"CellSpawner Error: Cell Entity Prefab {cellEntityPrefab} not found in EntityManager. Cannot spawn.", this.gameObject);
              return;
         }


        if (cellPositions.Count == 0)
        {
            Debug.LogWarning("CellSpawner: Cannot spawn, cell position data is empty.", this.gameObject);
            return;
        }

        Debug.Log($"CellSpawner: Requesting spawn for {cellPositions.Count} cells...", this.gameObject);

        // 1. Convert Dictionaries to NativeArray (using TempJob is safe here)
        NativeArray<CellData> nativeCellData = DataConverter.ConvertToNativeArray(
            cellPositions,
            cellClusters,
            Allocator.TempJob // System handles job dependency, TempJob is fine
        );

        if (!nativeCellData.IsCreated || nativeCellData.Length == 0)
        {
            Debug.LogError("CellSpawner Error: Failed to convert data to NativeArray or data is empty. Aborting spawn.", this.gameObject);
            if(nativeCellData.IsCreated) nativeCellData.Dispose(); // Dispose if created but empty and not passed
            return;
        }
        
        
        // 2. Batch Instantiate Entities
        // Use Allocator.TempJob for the temporary entity array
        NativeArray<Entity> entityArray = entityManager.Instantiate(
            cellEntityPrefab, // Use the baked entity prefab reference
            nativeCellData.Length,
            Allocator.TempJob
        );
        
        // --- Call the System to Schedule the Job ---

        // Pass default(JobHandle) as the input dependency from the MonoBehaviour.
        // The system will combine this with its own internal Dependency property.
        /*
        JobHandle spawnHandle = cellSpawningSystem.ScheduleSpawnJob(
            nativeCellData,
            cellEntityPrefab,
            default(JobHandle) // Pass default handle
        );
        
        spawnHandle.Complete();
        */
        

        // The MonoBehaviour usually doesn't need to track the returned handle.
        // The system manages its own dependency chain via 'this.Dependency'.

        // Log that the request was sent (actual spawning happens via the system job)
        Debug.Log($"CellSpawner: Spawn request sent to CellSpawningSystem for {nativeCellData.Length} cells.", this.gameObject);
    }


    public void ClearAllSpawnedCells()
    {
        if (entityManager == null) {
             Debug.LogWarning("CellSpawner: EntityManager not available. Cannot clear cells.", this.gameObject);
             return;
        }
        // Ensure query is created safely
        try {
            EntityQuery query = entityManager.CreateEntityQuery(typeof(CellComponent));
            int count = query.CalculateEntityCount();
            if (count > 0) {
                entityManager.DestroyEntity(query);
                Debug.Log($"CellSpawner: Cleared {count} spawned cell entities.", this.gameObject);
            } else {
                Debug.Log("CellSpawner: No spawned cell entities found to clear.", this.gameObject);
            }
        } catch (System.Exception ex) {
            Debug.LogError($"CellSpawner: Error during ClearAllSpawnedCells query/destroy. Exception: {ex.Message}", this.gameObject);
        }
    }
}
