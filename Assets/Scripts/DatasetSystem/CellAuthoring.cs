using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;

// Attach this to an empty GameObject in your scene.
public struct CellSpawnData : IComponentData
{
    public Entity CellPrefab;
    public float3 Translation;
}

public class CellAuthoring : MonoBehaviour
{

    public GameObject CellPrefab;
    public Dictionary<string, Vector2> Cells;
    public Dictionary<string, int> Clusters;

    private void Start()
    {
        
    }

    private class Baker : Baker<CellAuthoring>
    {

        
        
        public override void Bake(CellAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            //AddComponent(entity, new CellSpawnData());
            
        }
    }

    
}