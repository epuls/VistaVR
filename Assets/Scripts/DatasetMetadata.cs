using UnityEngine;
using System.Collections.Generic; // Needed for potential future use or alternative loading

/// <summary>
/// Holds metadata associated with a dataset.
/// Add new fields here as needed.
/// </summary>
[System.Serializable] // Optional: Makes it visible in Inspector if you ever have a public field of this type
public class DatasetMetadata
{
    // --- Define your parameters here ---
    public int clusterCount = 0; // Provide default values
    public float normalizationFactor = 1.0f;
    public string datasetName = "Unnamed Dataset"; // Example of adding another field
    public List<int> indices;
    public List<string> layerNames;
    public List<int> layerTypes;

    // --- Constants for Key Names (Good Practice) ---
    // Using const strings helps avoid typos when saving/loading
    public const string Key_ClusterCount = "clusterCount";
    public const string Key_NormalizationFactor = "normalizationFactor";
    public const string Key_DatasetName = "datasetName";
    public const string Key_Indices = "layerIndices";
    public const string Key_LayerNames = "layerNames";
    public const string Key_LayerTypes = "layerTypes";
    // Add const strings for new keys here
}