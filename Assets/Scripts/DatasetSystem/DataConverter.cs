// Scripts/Utility/DataConverter.cs
using UnityEngine;
using Unity.Collections;
using Unity.Mathematics;
using System.Collections.Generic;
using System; // Required for ArgumentException

public static class DataConverter
{
    /// <summary>
    /// Converts cell position and cluster dictionaries into a NativeArray of CellData.
    /// Remember to Dispose the returned NativeArray when done.
    /// </summary>
    /// <param name="cellPositions">Dictionary mapping Cell ID to World Position (Vector2).</param>
    /// <param name="cellClusters">Dictionary mapping Cell ID to Cluster ID.</param>
    /// <param name="allocator">The allocator to use for the NativeArray (e.g., Allocator.Persistent, Allocator.TempJob).</param>
    /// <returns>A NativeArray containing the merged cell data, or an uninitialized array on error.</returns>
    public static NativeArray<CellData> ConvertToNativeArray(
        Dictionary<string, Vector2> cellPositions,
        Dictionary<string, int> cellClusters,
        Allocator allocator)
    {
        if (cellPositions == null || cellClusters == null)
        {
            Debug.LogError("DataConverter Error: Input dictionaries cannot be null.");
            return default; // Return uninitialized array
        }

        int count = cellPositions.Count;
        if (count == 0)
        {
            Debug.LogWarning("DataConverter Warning: cellPositions dictionary is empty.");
            return new NativeArray<CellData>(0, allocator); // Return empty array
        }

        var cellsArray = new NativeArray<CellData>(count, allocator, NativeArrayOptions.UninitializedMemory);
        int index = 0;
        int errors = 0; // Count errors

        foreach (var kvp in cellPositions)
        {
            string id = kvp.Key;
            Vector2 unityPos = kvp.Value;

            // Find corresponding cluster ID, use -1 if not found
            int cluster = cellClusters.TryGetValue(id, out var clusterId) ? clusterId : -1;

            FixedString64Bytes fixedId = default; // Initialize to default
            try
            {
                // --- CORRECTED PART ---
                // Attempt to create the FixedString directly using the constructor.
                fixedId = new FixedString64Bytes(id);
                // --- END CORRECTION ---
            }
            catch (ArgumentException) // Catch exception if string exceeds capacity
            {
                // Handle error: ID is too long for FixedString64Bytes
                Debug.LogWarning($"Cell ID '{id}' is too long for FixedString64Bytes capacity ({FixedString64Bytes.UTF8MaxLengthInBytes} bytes). Skipping or using default ID for this entry.");
                // Optionally: skip this entry entirely by using 'continue'
                // continue;
                errors++;
                // Keep fixedId as default (empty) or assign a specific error marker if needed
            }


            // Add data to the array (even if ID failed, position/cluster might be useful)
            cellsArray[index] = new CellData
            {
                CellId = fixedId, // Will be empty/default if conversion failed
                Position = new float2(unityPos.x, unityPos.y), // Convert Vector2 to float2
                ClusterId = cluster
            };
            index++;
        }

        if (errors > 0)
        {
             Debug.LogWarning($"Encountered {errors} errors during FixedString conversion (IDs likely too long).");
        }

        // Optional: If you decided to 'continue' on error, the actual number of added items might be less than 'count'.
        // You could resize the array here if needed, but usually keeping the slot (possibly with default data) is fine.
        // Example resize (requires Allocator.Temp or Allocator.Persistent):
        // if (errors > 0 && allocator != Allocator.TempJob && allocator != Allocator.Invalid) {
        //    NativeArray<CellData> resizedArray = new NativeArray<CellData>(index, allocator, NativeArrayOptions.UninitializedMemory);
        //    NativeArray<CellData>.Copy(cellsArray, 0, resizedArray, 0, index);
        //    cellsArray.Dispose(); // Dispose the old oversized array
        //    return resizedArray;
        // }


        return cellsArray;
    }
}