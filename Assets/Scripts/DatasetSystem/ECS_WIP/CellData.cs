using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

[System.Serializable]
public struct CellData
{
    public FixedString64Bytes CellId; // Adjust size (32/64/128/512) as needed
    public float2 Position;           // Using Unity.Mathematics
    public int ClusterId;

    // Optional: Constructor for convenience
    public CellData(FixedString64Bytes id, float2 pos, int cluster)
    {
        CellId = id;
        Position = pos;
        ClusterId = cluster;
    }
}
