// Scripts/ECS/Jobs/SetParentJob.cs
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Transforms;
using UnityEngine; // Required for Parent component
// using UnityEngine; // No longer needed for Debug

[BurstCompile]
public struct SetParentJob : IJobParallelFor
{
    // Input data (read-only)
    [ReadOnly] public NativeArray<Entity> CellEntities;
    [ReadOnly] public NativeArray<Entity> ClusterEntities;
    [ReadOnly] public ComponentLookup<CellComponent> CellComponentLookup;
    [ReadOnly] public BufferLookup<LinkedEntityGroup> LinkedEntityGroupLookup;

    // Command buffer for structural changes
    public EntityCommandBuffer.ParallelWriter ECB;

    // --- Removed Manual LEG Management ---
    // public BufferLookup<LinkedEntityGroup> LinkedEntityGroupLookup; // REMOVED
    // --- End Removed ---


    public void Execute(int index)
    {
        Entity cellEntity = CellEntities[index];

        // Get the ClusterId from the cell's component
        if (!CellComponentLookup.HasComponent(cellEntity))
        {
            // Consider logging this error via a separate mechanism if needed
            return; // Skip if cell component is missing
        }
        CellComponent cellData = CellComponentLookup[cellEntity];
        int clusterId = cellData.ClusterId;

        // Validate the cluster ID
        if (clusterId >= 0 && clusterId < ClusterEntities.Length)
        {
            Entity parentClusterEntity = ClusterEntities[clusterId];

            // Add Parent component to the cell entity via ECB
            // This is the only structural change this job should make.
            ECB.AddComponent(index, cellEntity, new Parent { Value = parentClusterEntity });
            ECB.AppendToBuffer(index, cellEntity, new LinkedEntityGroup {Value = parentClusterEntity});
            //ECB.AppendToBuffer(index, parentClusterEntity, new LinkedEntityGroup {Value = cellEntity});

            
            
            if (LinkedEntityGroupLookup.HasBuffer(parentClusterEntity))
            {
                //Debug.Log("Adding buffer");
                ECB.AppendToBuffer(index, parentClusterEntity, new LinkedEntityGroup {Value = cellEntity});
            }
            else
            {
                //Debug.Log("Creating and Adding buffer");
                ECB.AddBuffer<LinkedEntityGroup>(index, parentClusterEntity);
                ECB.AppendToBuffer(index, parentClusterEntity, new LinkedEntityGroup {Value = parentClusterEntity});
                ECB.AppendToBuffer(index, parentClusterEntity, new LinkedEntityGroup {Value = cellEntity});
            }
        }
        // else: Handle invalid cluster IDs if necessary (e.g., log via separate mechanism)
    }
}