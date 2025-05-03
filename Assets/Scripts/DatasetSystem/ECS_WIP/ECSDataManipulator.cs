// Scripts/MonoBehaviours/ECSDataManipulator.cs
// This MonoBehaviour lives outside the Subscene/baked world.
// It sends requests to enable/disable clusters by creating temporary entities
// with the ClusterControlRequest component.

using System;
using UnityEngine;
using Unity.Entities;

public class ECSDataManipulator : MonoBehaviour
{
    private EntityManager entityManager;
    private bool isInitialized = false;

    private void OnEnable()
    {
        Initialize();
        DisableCluster(0);
        DisableCluster(1);
        DisableCluster(2);
        DisableCluster(3);
        DisableCluster(4);
        DisableCluster(5);
        DisableCluster(6);
        DisableCluster(7);
        DisableCluster(8);
    }

    public void Initialize()
    {
        if (isInitialized) return;

        World defaultWorld = World.DefaultGameObjectInjectionWorld;
        if (defaultWorld == null || !defaultWorld.IsCreated)
        {
            Debug.LogError("ECSDataManipulator Error: Default World not found!", this.gameObject);
            this.enabled = false;
            return;
        }
        entityManager = defaultWorld.EntityManager;
        isInitialized = true;
        Debug.Log("ECSDataManipulator Initialized.", this.gameObject);
    }

    /// <summary>
    /// Sends a request to enable a specific cluster by its index.
    /// </summary>
    /// <param name="clusterIndex">The 0-based index of the cluster to enable.</param>
    public void EnableCluster(int clusterIndex)
    {
        SendControlRequest(clusterIndex, true);
    }

    /// <summary>
    /// Sends a request to disable a specific cluster by its index.
    /// </summary>
    /// <param name="clusterIndex">The 0-based index of the cluster to disable.</param>
    public void DisableCluster(int clusterIndex)
    {
        SendControlRequest(clusterIndex, false);
    }

    /// <summary>
    /// Creates the temporary request entity.
    /// </summary>
    private void SendControlRequest(int targetIndex, bool enable)
    {
        if (!isInitialized || entityManager == null)
        {
            Debug.LogError("ECSDataManipulator Error: Not initialized or EntityManager invalid. Cannot send request.", this.gameObject);
            return;
        }

        // Create a temporary entity to hold the request component
        Entity requestEntity = entityManager.CreateEntity();

        // Add the request component with the desired data
        entityManager.AddComponentData(requestEntity, new ClusterControlRequest
        {
            TargetClusterIndex = targetIndex,
            Enable = enable
        });

        Debug.Log($"ECSDataManipulator: Sent request to {(enable ? "Enable" : "Disable")} cluster index {targetIndex}.", this.gameObject);
    }

    // --- Example Usage ---
    /*
    void Update()
    {
        // Example: Disable cluster 0 when 'D' key is pressed
        if (Input.GetKeyDown(KeyCode.D))
        {
            DisableCluster(0);
        }

        // Example: Enable cluster 0 when 'E' key is pressed
        if (Input.GetKeyDown(KeyCode.E))
        {
            EnableCluster(0);
        }
    }
    */
}
