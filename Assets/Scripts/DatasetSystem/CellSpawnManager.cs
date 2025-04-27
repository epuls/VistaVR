// Example: Scripts/MonoBehaviours/TriggerScript.cs
// This MonoBehaviour lives OUTSIDE the Subscene/baked world.
// It triggers the spawning process by requesting it from the CellSpawningSystem.
using UnityEngine;
using Unity.Entities;
using Unity.Collections; // Required for NativeArray and Allocator
using System.Collections.Generic; // Required for Dictionary
// using Unity.Jobs; // No longer needed here as system handles jobs


public class CellSpawnManager : MonoBehaviour
{
    // --- References needed ---
    private EntityManager entityManager;
    private CellSpawningSystem cellSpawningSystem;
    private Entity cellEntityPrefab = Entity.Null; // Store the prefab reference when found
    private Entity cellClusterEntityPrefab = Entity.Null;
    private Entity grandparentEntity = Entity.Null;

    // --- Data Source ---
    // Load these dictionaries here or get them from another source
    // Example: Load from CSVs in LoadMyData()
    public Dictionary<string, Vector2> cellPositions = new Dictionary<string, Vector2>();
    public Dictionary<string, int> cellClusters = new Dictionary<string, int>();
    public float normalizationFactor = 27762f;
    public int clusterCount = -1;
    private bool isInitialized = false;

    void Start()
    {
        //Initialize();
        
        // For Debugging: Load data right away after initialization
        //LoadMyData();
        //TriggerSpawn();
    }

    public void Initialize()
    {

        if (isInitialized) return; // Prevent re-initialization

        // Get the default World and EntityManager
        World defaultWorld = World.DefaultGameObjectInjectionWorld;
        if (defaultWorld == null || !defaultWorld.IsCreated)
        {
            Debug.LogError("TriggerScript Error: Default World not found or not created! ECS might not be initialized.", this.gameObject);
            this.enabled = false; // Disable script if ECS isn't ready
            return;
        }
        entityManager = defaultWorld.EntityManager;

        // Get a reference to the CellSpawningSystem
        cellSpawningSystem = defaultWorld.GetExistingSystemManaged<CellSpawningSystem>();
        if (cellSpawningSystem == null)
        {
            Debug.LogError("TriggerScript Error: CellSpawningSystem not found! Make sure it's created and not disabled.", this.gameObject);
            this.enabled = false; // Disable if system is missing
            return;
        }

        // Attempt to find the Spawner Entity using its tag component and get the prefab reference
        TryGetBakedPrefabReference();
        TryGetGrandparentEntityReference();

        isInitialized = true;
        Debug.Log("TriggerScript Initialized.", this.gameObject);
    }

    /// <summary>
    /// Attempts to find the singleton Spawner entity (via SpawnerTagComponent)
    /// and retrieve the baked cell Entity prefab reference from its CellSpawnerComponent.
    /// Stores the result in the cellEntityPrefab field.
    /// </summary>
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
                    this.cellEntityPrefab = spawnerComp.CellEntityPrefab; // Store the reference
                    this.cellClusterEntityPrefab = spawnerComp.ClusterEntityPrefab;
                    this.grandparentEntity = spawnerComp.ClusterParentEntityPrefab;

                    if (this.cellEntityPrefab != Entity.Null)
                    {
                        Debug.Log($"TriggerScript: Found Spawner Entity {spawnerEntity} and got Cell Prefab {this.cellEntityPrefab}", this.gameObject);
                    }
                    else
                    {
                        // This case means the Baker ran but didn't successfully bake the prefab entity
                        Debug.LogWarning("TriggerScript: Found Spawner Entity but Cell Prefab reference is Null in its component. Check Baker logic.", this.gameObject);
                    }
                    
                    if (this.cellClusterEntityPrefab != Entity.Null)
                    {
                        Debug.Log($"TriggerScript: Found Spawner Entity {spawnerEntity} and got Cluster Prefab {this.cellClusterEntityPrefab}", this.gameObject);
                    }
                    else
                    {
                        // This case means the Baker ran but didn't successfully bake the prefab entity
                        Debug.LogWarning("TriggerScript: Found Spawner Entity but Cluster Prefab reference is Null in its component. Check Baker logic.", this.gameObject);
                    }
                    
                    
                    
                 } else {
                      Debug.LogWarning($"TriggerScript: Spawner entity {spawnerEntity} found by query but no longer exists.", this.gameObject);
                      this.cellEntityPrefab = Entity.Null; // Ensure it's null if entity disappeared
                 }

            }
            catch (System.Exception ex)
            {
                 Debug.LogError($"TriggerScript: Error getting CellSpawnerComponent from entity {spawnerEntity}. Exception: {ex.Message}", this.gameObject);
                 this.cellEntityPrefab = Entity.Null; // Ensure it's null on error
            }
        }
        else
        {
            // Handle cases where the singleton wasn't found or wasn't unique
            int count = query.CalculateEntityCount(); // Check how many matched
             if (count == 0) {
                 Debug.LogWarning("TriggerScript: Could not find any Entity with SpawnerTagComponent and CellSpawnerComponent. Make sure the Spawner GameObject is in a Subscene and baked correctly.", this.gameObject);
             } else {
                 // This indicates an issue with setup - should only have one spawner entity
                 Debug.LogWarning($"TriggerScript: Found {count} entities matching query for SpawnerTagComponent/CellSpawnerComponent, but expected a singleton. Cannot reliably get prefab.", this.gameObject);
             }
            this.cellEntityPrefab = Entity.Null; // Ensure prefab is null if lookup fails
        }

        // Dispose of the query object to prevent memory leaks
        query.Dispose();
    }


    void TryGetGrandparentEntityReference()
    {
        if (entityManager == null) {
            Debug.LogError("CellSpawnManager: EntityManager not available during TryGetGrandparentEntityReference.");
            this.grandparentEntity = Entity.Null;
            return;
        }

        EntityQuery query = entityManager.CreateEntityQuery(ComponentType.ReadOnly<GrandparentTagComponent>());

        if (query.TryGetSingletonEntity<Entity>(out Entity foundGrandparent)) // Using TryGetSingletonEntity assumes only ONE grandparent
        {
            this.grandparentEntity = foundGrandparent;
            Debug.Log($"CellSpawnManager: Found Grandparent entity {this.grandparentEntity}", this.gameObject);
        }
        else
        {
            int count = query.CalculateEntityCount();
            if (count == 0) {
                Debug.LogWarning("CellSpawnManager: Could not find any Entity with GrandparentTagComponent. Ensure the Grandparent GameObject is in a Subscene and baked correctly.", this.gameObject);
            } else {
                Debug.LogWarning($"CellSpawnManager: Found {count} entities matching GrandparentTagComponent query, but expected a singleton.", this.gameObject);
            }
            this.grandparentEntity = Entity.Null;
        }
        query.Dispose();
    }
    
    public void LoadMyData(Dictionary<string,Vector2> cellPos, Dictionary<string,int> cellClust)
    {
        cellPositions = cellPos;
        cellClusters = cellClust;
    }

    /// <summary>
    /// Public method to be called by UI Button, game manager, or other logic
    /// to initiate the spawning of cells based on the loaded data.
    /// </summary>
    public void TriggerSpawn()
    {
        // --- Pre-Spawn Checks ---
        // Ensure initialization has happened and references are valid
        if (!isInitialized)
        {
            Debug.LogError("TriggerScript: Not initialized. Cannot trigger spawn.", this.gameObject);
            // Optionally try to initialize again, though it might indicate a deeper issue
            // Initialize();
            // if (!isInitialized) return;
            return;
        }

        // Check if we successfully found the prefab reference during Initialize.
        // If not, try again now - maybe baking finished late or entity appeared.
        if (cellEntityPrefab == Entity.Null)
        {
            Debug.LogWarning("TriggerScript: Cell Prefab reference was null, attempting lookup again before spawning.", this.gameObject);
            TryGetBakedPrefabReference(); // Attempt lookup again
            if (cellEntityPrefab == Entity.Null) // Check again after attempt
            {
                 Debug.LogError("TriggerScript Error: Cell Entity Prefab reference is still missing after check. Cannot trigger spawn. Check Baker/Authoring/Subscene setup.", this.gameObject);
                 return;
            }
        }
         // Final check on prefab existence in EntityManager just before use
         if (!entityManager.Exists(cellEntityPrefab)) {
              Debug.LogError($"TriggerScript Error: Cell Entity Prefab {cellEntityPrefab} reference exists but entity not found in EntityManager. Cannot trigger spawn.", this.gameObject);
              // Maybe prefab entity was destroyed? Clear reference.
              cellEntityPrefab = Entity.Null;
              return;
         }
         
         

        // Check system reference again (paranoid check)
        if (cellSpawningSystem == null)
        {
             Debug.LogError("TriggerScript Error: CellSpawningSystem reference is missing. Cannot trigger spawn.", this.gameObject);
             return;
        }

        // Check if data has been loaded
        if (cellPositions.Count == 0)
        {
            Debug.LogWarning("TriggerScript: No position data loaded or available. Cannot trigger spawn.", this.gameObject);
            return;
        }

        Debug.Log($"TriggerScript: Triggering spawn for {cellPositions.Count} cells...", this.gameObject);

        // --- Data Conversion ---
        // 1. Convert Dictionaries to NativeArray using Allocator.Persistent
        // The System will take ownership of this array and Dispose it later.
        NativeArray<CellData> nativeCellData = DataConverter.ConvertToNativeArray(
            cellPositions,
            cellClusters,
            Allocator.Persistent // MUST use Persistent for data handover to system
        );
        
        Debug.Log($"TriggerScript: Converted data. Length: {nativeCellData.Length}. IsCreated: {nativeCellData.IsCreated}");
        if (nativeCellData.IsCreated && nativeCellData.Length > 0) {
            Debug.Log($"TriggerScript: First CellData Position: {nativeCellData[0].Position}, Cluster: {nativeCellData[0].ClusterId}");
        }

        // Check conversion result
        if (!nativeCellData.IsCreated || nativeCellData.Length == 0)
        {
            Debug.LogError("TriggerScript Error: Failed to convert data to NativeArray or data is empty. Aborting spawn.", this.gameObject);
            // Dispose if we created it but can't pass it to the system
            if(nativeCellData.IsCreated) nativeCellData.Dispose();
            return;
        }
        
        // --- Check if grandparent was found ---
        if (grandparentEntity == Entity.Null) {
            Debug.LogWarning("CellSpawnManager: Grandparent entity reference is null, attempting lookup again.");
            TryGetGrandparentEntityReference();
            if (grandparentEntity == Entity.Null) {
                Debug.LogError("CellSpawnManager Error: Grandparent entity reference is still missing. Cannot trigger spawn.", this.gameObject);
                // Dispose nativeCellData if it was created
                if (nativeCellData.IsCreated) nativeCellData.Dispose();
                return;
            }
        }
        // Final check on grandparent existence
        if (!entityManager.Exists(grandparentEntity)) {
            Debug.LogError($"CellSpawnManager Error: Grandparent entity {grandparentEntity} not found in EntityManager. Cannot trigger spawn.", this.gameObject);
            if (nativeCellData.IsCreated) nativeCellData.Dispose();
            return;
        }


// --- Request Spawn from System ---
// Pass the found grandparentEntity
        bool requestAccepted = cellSpawningSystem.RequestSpawn(
            nativeCellData,
            cellEntityPrefab,
            cellClusterEntityPrefab,
            clusterCount,
            grandparentEntity // Pass the grandparent reference
        );

        // --- Request Spawn from System ---
        // 2. Call the system's public RequestSpawn method, passing ownership of the data
        //bool requestAccepted = cellSpawningSystem.RequestSpawn(nativeCellData, cellEntityPrefab, cellClusterEntityPrefab, clusterCount, grandparentEntity);

        // 3. Handle potential request rejection (e.g., if system was busy processing a previous request)
        if (!requestAccepted)
        {
             Debug.LogWarning("TriggerScript: Spawn request was not accepted by the system (maybe busy?). Disposing persistent data locally.", this.gameObject);
             // If the system didn't accept the request/take ownership, we MUST dispose the persistent array here to prevent a leak.
             nativeCellData.Dispose();
        }
        else
        {
             Debug.Log($"TriggerScript: Spawn request successfully sent to CellSpawningSystem.", this.gameObject);
             // System now owns nativeCellData and is responsible for disposing it.
        }
    }

     // Optional: Add a method to clear cells. This would likely need its own system interaction.
     /*
     public void TriggerClear()
     {
        // Implementation would likely involve:
        // 1. Getting the EntityManager.
        // 2. Creating an EntityQuery for typeof(CellComponent).
        // 3. Calling EntityManager.DestroyEntity(query).
        // OR: Sending a clear request to a dedicated system.
        Debug.Log("TriggerClear: Functionality not fully implemented.");
     }
     */
}
