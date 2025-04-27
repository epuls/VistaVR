// Scripts/Authoring/ClusterAuthoring.cs
// Add this MonoBehaviour to your original CLUSTER GameObject prefab.
using UnityEngine;
using Unity.Entities;
public class ClusterAuthoring : MonoBehaviour
{
    // No fields needed just to add a tag.
    // You could add fields here if clusters needed specific baked data.
}

// Scripts/ECS/Bakers/ClusterBaker.cs
// This Baker runs for the cluster GameObject prefab.

// Simple tag component to identify cluster entities.
public struct ClusterTagComponent : IComponentData { }
// Component added to temporary entities to request enabling/disabling a cluster.
public struct ClusterControlRequest : IComponentData
{
    // Index of the cluster entity to target (assuming 0 to ClusterCount-1).
    // Use a different identifier (e.g., FixedString ID) if indices aren't stable/suitable.
    public int TargetClusterIndex;

    // True to enable (remove Disabled tag), False to disable (add Disabled tag).
    public bool Enable;
}

public class ClusterBaker : Baker<ClusterAuthoring>
{
    public override void Bake(ClusterAuthoring authoring)
    {
        // Get the entity for the prefab being baked
        // Use Dynamic if clusters might move/rotate/scale independently of the grandparent
        var entity = GetEntity(TransformUsageFlags.Dynamic);

        // Add the tag component
        AddComponent<ClusterTagComponent>(entity);
        


        Debug.Log($"ClusterBaker: Added ClusterTagComponent to baked entity {entity} for prefab '{authoring.gameObject.name}'");
    }
}
