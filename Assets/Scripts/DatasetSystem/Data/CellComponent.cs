using Unity.Entities;
using Unity.Collections;
using UnityEngine;

public struct CellComponent : IComponentData
{
    public FixedString64Bytes CellId;
    public int ClusterId;
    // Add any other per-cell runtime data needed
}

public struct SpawnedTag : IComponentData { }