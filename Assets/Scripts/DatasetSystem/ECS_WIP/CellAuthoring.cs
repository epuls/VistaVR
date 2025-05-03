using Unity.Entities;
using UnityEngine;

public class CellAuthoring : MonoBehaviour
{
    // You could add fields here if you wanted to bake
    // initial default values from the Inspector, but for just
    // adding the component, no fields are needed.
    // For example:
    // public int defaultClusterId = -1;
}

public class CellBaker : Baker<CellAuthoring>
{
    public override void Bake(CellAuthoring authoring)
    {
        // Get the entity for the prefab being baked
        // Use appropriate TransformUsageFlags based on whether the cell needs
        // transform components baked in directly (usually handled by parent/spawner setup)
        // Often Renderable is sufficient if transform is set entirely at runtime.
        // Let's assume Renderable as transform is set by the job.
        var entity = GetEntity(TransformUsageFlags.Dynamic);

        // Add the CellComponent to the baked entity prefab.
        // It will initially have default values (0 for int, null/empty for FixedString).
        AddComponent<CellComponent>(entity);

        // Example: If you added fields to CellAuthoring to set defaults:
        // AddComponent(entity, new CellComponent
        // {
        //     ClusterId = authoring.defaultClusterId
        //     // CellId would likely still be set at runtime
        // });

        Debug.Log($"CellBaker: Added CellComponent to baked entity {entity} for prefab '{authoring.gameObject.name}'");
    }
}