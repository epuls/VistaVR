// Scripts/ECS/Jobs/SetCellDataJob.cs
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms; // Ensure this using statement is present!

[BurstCompile]
public struct SetCellDataJob : IJobParallelFor
{
    public NativeArray<CellData> SourceCellData;
    public NativeArray<Entity> Entities;

    // --- Use ComponentLookup (obtained from SystemBase/SystemAPI) ---
    [NativeDisableParallelForRestriction] // May still be needed depending on context
    public ComponentLookup<LocalTransform> LocalTransformLookup; // Changed from ComponentDataFromEntity

    [NativeDisableParallelForRestriction] // May still be needed depending on context
    public ComponentLookup<CellComponent> CellComponentLookup; // Changed from ComponentDataFromEntity

    public void Execute(int index)
    {
        Entity currentEntity = Entities[index];
        if (index >= SourceCellData.Length) return; // Basic bounds check

        CellData data = SourceCellData[index];

        // Set LocalTransform component using ComponentLookup
        // Check if the entity actually has the component - Instantiate should add it if the prefab has it.
        if (LocalTransformLookup.HasComponent(currentEntity))
        {
            LocalTransformLookup[currentEntity] = new LocalTransform
            {
                Position = new float3(data.Position.x, 0f, data.Position.y),
                Rotation = quaternion.identity,
                Scale = 1f
            };
        }

        // Set CellComponent component using ComponentLookup
        // Check if the entity has the component - Instantiate does NOT add components
        // that aren't on the prefab, so we usually assume the job adds it if needed,
        // OR the prefab is guaranteed to have it. Let's assume we need to add/set.
        // Note: Adding components in a parallel job is complex. Usually done via ECB or main thread.
        // For simplicity here, we'll assume CellComponent is ADDED by the system or is on the prefab.
        // If it needs adding here, use an EntityCommandBuffer.
         if (CellComponentLookup.HasComponent(currentEntity)) // Check if it exists (e.g., added by prefab)
         {
            CellComponentLookup[currentEntity] = new CellComponent
            {
                CellId = data.CellId,
                ClusterId = data.ClusterId
            };
         }
         // else { /* Handle case where component needs adding - requires ECB */ }

    }
}
