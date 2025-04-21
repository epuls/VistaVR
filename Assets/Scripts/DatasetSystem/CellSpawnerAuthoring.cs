/*
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

public class CellSpawnerAuthoring : MonoBehaviour
{
    
    public Dictionary<string, Vector2> Cells;
    public Dictionary<string, int> Clusters;
    public GameObject CellPrefab;


    private class Baker : Baker<CellSpawnerAuthoring>
    {
        public override void Bake(CellSpawnerAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.None);
            AddComponent(entity, new CellSpawnData());
        }
    }

    public partial struct CellSpawnSystem : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            
        }
    }
}
*/


// CellSpawnerAuthoring.cs
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

[DisallowMultipleComponent]
public class CellSpawnerAuthoring : MonoBehaviour
{
    [Header("Prefab & Cells")]
    public GameObject CellPrefab;

    [Tooltip("World‑space XZ positions for your cells")]
    public List<Vector2> Cells;

    [Tooltip("Cluster index for each cell; must line up with Cells.Count")]
    public List<int> Clusters;

    [Header("Spawn Settings")]
    public int ClusterToSpawn = 0;
    public bool limitCellSpawn = false;
    public int cellCountToSpawn = 100;
    public float normalizationFactor = 1f;
    public Vector3 offset = Vector3.zero;
}

// Holds the baked prefab + your spawn settings in ECS
public struct CellSpawnerSettings : IComponentData
{
    public Entity CellPrefabEntity;
    public int ClusterToSpawn;
    public bool limitCellSpawn;
    public int cellCountToSpawn;
    public float normalizationFactor;
    public float3 offset;
}

// A buffer element for each cell: its coords + cluster id
public struct CellPosition : IBufferElementData
{
    public float2 coords;
    public int   cluster;
}

// The Baker that drives conversion from the MonoBehaviour → ECS world
public class CellSpawnerBaker : Baker<CellSpawnerAuthoring>
{
    public override void Bake(CellSpawnerAuthoring authoring)
    {
        // 1) Get a new entity to hold our spawner data
        var e = GetEntity(TransformUsageFlags.None);

        // 2) Declare the prefab as a referenced asset
        //DeclareReferencedPrefab(authoring.CellPrefab);

        // 3) Grab the baked entity for that prefab
        var prefabEntity = GetEntity(authoring.CellPrefab, TransformUsageFlags.Dynamic);

        // 4) Add our settings component
        AddComponent(e, new CellSpawnerSettings
        {
            CellPrefabEntity   = prefabEntity,
            ClusterToSpawn     = authoring.ClusterToSpawn,
            limitCellSpawn     = authoring.limitCellSpawn,
            cellCountToSpawn   = authoring.cellCountToSpawn,
            normalizationFactor = authoring.normalizationFactor,
            offset             = authoring.offset
        });

        // 5) Create & fill a dynamic buffer of all your cell positions + clusters
        var buffer = AddBuffer<CellPosition>(e);
        for (int i = 0; i < authoring.Cells.Count; i++)
        {
            buffer.Add(new CellPosition
            {
                coords  = authoring.Cells[i],
                cluster = authoring.Clusters[i]
            });
        }
    }
}
