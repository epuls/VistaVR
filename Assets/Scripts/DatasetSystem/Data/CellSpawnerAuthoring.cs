// Scripts/MonoBehaviours/CellSpawnerAuthoring.cs
// This component goes on the SAME GameObject as CellSpawner.
// Its purpose is to hold the reference to the GameObject prefab
// that the Baker will convert into an Entity prefab.

using UnityEngine;
using Unity.Entities; // Required for Baker interaction (implicitly)

public class CellSpawnerAuthoring : MonoBehaviour
{
    [Header("Prefab Configuration")]
    [Tooltip("Assign the ORIGINAL GameObject prefab for the cell here.")]
    public GameObject cellPrefab; // Assign your cell GameObject prefab in the Inspector
    public GameObject clusterPrefab;
    public GameObject clusterParentPrefab;
}

