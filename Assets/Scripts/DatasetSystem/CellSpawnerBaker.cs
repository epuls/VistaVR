// Scripts/ECS/Bakers/CellSpawnerBaker.cs
// This Baker runs in the editor during the DOTS baking process.
// It finds the CellSpawnerAuthoring component, converts the referenced
// GameObject prefab into an Entity prefab, and stores that reference
// in a CellSpawnerComponent on the spawner's entity.

using UnityEngine;
using Unity.Entities;

// Define the component that will store the baked Entity prefab reference.
// This component will be added to the entity created from the GameObject
// that has the CellSpawnerAuthoring and CellSpawner components.
public struct CellSpawnerComponent : IComponentData
{
    public Entity CellEntityPrefab; // The baked reference to the cell entity
}

public struct SpawnerTagComponent : IComponentData
{
}

// The Baker targets the CellSpawnerAuthoring component.
public class CellSpawnerBaker : Baker<CellSpawnerAuthoring>
{
    public override void Bake(CellSpawnerAuthoring authoring)
    {
        // Check if the authoring component has a valid prefab assigned.
        if (authoring.cellPrefab == null)
        {
            Debug.LogError("CellSpawnerAuthoring: 'Cell Prefab' field is not assigned. Baking cannot proceed.", authoring.gameObject);
            return; // Stop baking for this component if the prefab is missing
        }

        // Get the entity that represents the GameObject this Baker is running on.
        // TransformUsageFlags.None indicates this spawner entity itself doesn't need
        // complex transform components by default (unless it moves, etc.).
        var spawnerEntity = GetEntity(TransformUsageFlags.None);

        try
        {
            // Request the baking system to convert the referenced GameObject prefab
            // into an Entity prefab.
            // TransformUsageFlags.Dynamic is appropriate if the spawned cells will move.
            // Use TransformUsageFlags.Renderable if they only need rendering components.
            // Use TransformUsageFlags.None if they have no transform/rendering needs baked in.
            var entityPrefab = GetEntity(authoring.cellPrefab, TransformUsageFlags.Dynamic);

            // Add the CellSpawnerComponent to the spawner's entity.
            // This component holds the baked Entity prefab reference so the runtime
            // CellSpawner script can access it.
            AddComponent(spawnerEntity, new CellSpawnerComponent
            {
                CellEntityPrefab = entityPrefab
            });
            
            AddComponent(spawnerEntity, new SpawnerTagComponent()
            {
                
            });

             Debug.Log($"CellSpawnerBaker: Successfully baked CellEntityPrefab ({entityPrefab}) onto Spawner Entity ({spawnerEntity})", authoring.gameObject);
        }
        catch (System.Exception e)
        {
             Debug.LogError($"CellSpawnerBaker: Failed to get entity for cellPrefab '{authoring.cellPrefab.name}'. Ensure the prefab is valid. Exception: {e.Message}", authoring.gameObject);
             // Depending on the error, you might still want to add the component
             // AddComponent(spawnerEntity, new CellSpawnerComponent { CellEntityPrefab = Entity.Null });
        }
    }
}
