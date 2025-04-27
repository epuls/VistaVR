using UnityEngine;
using Unity.Entities;

public class GrandparentAuthoring : MonoBehaviour
{
    
}

public struct GrandparentTagComponent : IComponentData { }

public class GrandparentBaker : Baker<GrandparentAuthoring>
{
    public override void Bake(GrandparentAuthoring authoring)
    {
        // Get the entity being baked
        var entity = GetEntity(TransformUsageFlags.Dynamic); // Use Dynamic if it will move/scale/rotate

        // Add the tag component
        AddComponent<GrandparentTagComponent>(entity);

        Debug.Log($"GrandparentBaker: Added GrandparentTagComponent to baked entity {entity} for '{authoring.gameObject.name}'");
    }
}