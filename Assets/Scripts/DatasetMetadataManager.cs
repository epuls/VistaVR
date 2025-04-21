using UnityEngine;
using System.IO;
using System.Collections.Generic;
using System.Globalization; // For CultureInfo.InvariantCulture
using System.Linq; // For Linq operations like Select

public static class DatasetMetadataManager
{
    private const char KeyValueSeparator = ':';
    private const char ListSeparatorInt = ','; // Separator for lists of integers
    private const char ListSeparatorString = '|'; // Separator for lists of strings (less likely to conflict)

    /// <summary>
    /// Saves the provided DatasetMetadata object to a text file.
    /// </summary>
    /// <param name="metadata">The DatasetMetadata object containing the values to save.</param>
    /// <param name="filePath">The full path to the file where metadata will be saved.</param>
    /// <returns>True if saving was successful, false otherwise.</returns>
    public static bool SaveMetadata(DatasetMetadata metadata, string filePath)
    {
        if (metadata == null)
        {
            Debug.LogError("DatasetMetadataManager Error: Metadata object provided is null.");
            return false;
        }
        if (string.IsNullOrEmpty(filePath))
        {
            Debug.LogError("DatasetMetadataManager Error: File path is null or empty.");
            return false;
        }

        // Ensure the directory exists
        string directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            try
            {
                Directory.CreateDirectory(directory);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"DatasetMetadataManager Error: Could not create directory '{directory}'. Exception: {e.Message}");
                return false;
            }
        }

        try
        {
            using (StreamWriter writer = new StreamWriter(filePath, false, System.Text.Encoding.UTF8))
            {
                // --- Write Standard Fields ---
                writer.WriteLine($"{DatasetMetadata.Key_ClusterCount}{KeyValueSeparator} {metadata.clusterCount}");
                writer.WriteLine($"{DatasetMetadata.Key_NormalizationFactor}{KeyValueSeparator} {metadata.normalizationFactor.ToString(CultureInfo.InvariantCulture)}");
                writer.WriteLine($"{DatasetMetadata.Key_DatasetName}{KeyValueSeparator} {metadata.datasetName}");

                // --- Write List Fields ---
                // Indices (List<int>) - Join with ListSeparatorInt
                string indicesString = (metadata.indices != null)
                    ? string.Join(ListSeparatorInt.ToString(), metadata.indices)
                    : ""; // Handle null list case gracefully
                writer.WriteLine($"{DatasetMetadata.Key_Indices}{KeyValueSeparator} {indicesString}");

                // Layer Names (List<string>) - Join with ListSeparatorString
                // Important: If layer names themselves can contain the separator '|', this simple approach breaks.
                // Consider more robust serialization (like JSON) or escaping if needed.
                string layerNamesString = (metadata.layerNames != null)
                    ? string.Join(ListSeparatorString.ToString(), metadata.layerNames)
                    : "";
                writer.WriteLine($"{DatasetMetadata.Key_LayerNames}{KeyValueSeparator} {layerNamesString}");

                // Layer Types (List<int>) - Join with ListSeparatorInt
                string layerTypesString = (metadata.layerTypes != null)
                    ? string.Join(ListSeparatorInt.ToString(), metadata.layerTypes)
                    : "";
                writer.WriteLine($"{DatasetMetadata.Key_LayerTypes}{KeyValueSeparator} {layerTypesString}");

            } // StreamWriter disposed here

            Debug.Log($"Dataset metadata saved successfully to: {filePath}");
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"DatasetMetadataManager Error: Failed to write metadata to '{filePath}'. Exception: {e.Message}\n{e.StackTrace}");
            return false;
        }
    }

    /// <summary>
    /// Loads metadata from a text file into a new DatasetMetadata object.
    /// </summary>
    /// <param name="filePath">The full path to the metadata file.</param>
    /// <returns>A populated DatasetMetadata object, or a new one with default values if the file doesn't exist or an error occurs.</returns>
    public static DatasetMetadata LoadMetadata(string filePath)
    {
        DatasetMetadata metadata = new DatasetMetadata(); // Start with default values (including empty lists)

        if (string.IsNullOrEmpty(filePath))
        {
            Debug.LogError("DatasetMetadataManager Error: File path provided for loading is null or empty.");
            return metadata;
        }

        if (!File.Exists(filePath))
        {
            Debug.LogWarning($"DatasetMetadataManager Info: Metadata file not found at '{filePath}'. Returning default metadata.");
            return metadata;
        }

        Dictionary<string, string> fileData = new Dictionary<string, string>();

        try
        {
            using (StreamReader reader = new StreamReader(filePath, System.Text.Encoding.UTF8))
            {
                string line;
                int lineNumber = 0;
                while ((line = reader.ReadLine()) != null)
                {
                    lineNumber++;
                    line = line.Trim();
                    if (string.IsNullOrEmpty(line) || line.StartsWith("#") || line.StartsWith("//")) continue;

                    int separatorIndex = line.IndexOf(KeyValueSeparator);
                    if (separatorIndex <= 0)
                    {
                        Debug.LogWarning($"DatasetMetadataManager Warning: Skipping malformed line {lineNumber} in '{filePath}': '{line}'");
                        continue;
                    }

                    string key = line.Substring(0, separatorIndex).Trim();
                    string value = line.Substring(separatorIndex + 1).Trim();

                    if (fileData.ContainsKey(key))
                    {
                         Debug.LogWarning($"DatasetMetadataManager Warning: Duplicate key '{key}' found on line {lineNumber} in '{filePath}'. Using the last value found.");
                    }
                    fileData[key] = value;
                }
            }

            // --- Populate the Metadata Object from the Dictionary ---

            // Standard Fields (with improved parsing warnings)
            if (fileData.TryGetValue(DatasetMetadata.Key_ClusterCount, out string clusterCountStr)) {
                if (!int.TryParse(clusterCountStr, out metadata.clusterCount)) {
                     Debug.LogWarning($"DatasetMetadataManager Warning: Could not parse value for '{DatasetMetadata.Key_ClusterCount}' ('{clusterCountStr}') as int in '{filePath}'. Using default ({metadata.clusterCount}).");
                }
            }
            if (fileData.TryGetValue(DatasetMetadata.Key_NormalizationFactor, out string normFactorStr)) {
                 if (!float.TryParse(normFactorStr, NumberStyles.Float, CultureInfo.InvariantCulture, out metadata.normalizationFactor)) {
                     Debug.LogWarning($"DatasetMetadataManager Warning: Could not parse value for '{DatasetMetadata.Key_NormalizationFactor}' ('{normFactorStr}') as float in '{filePath}'. Using default ({metadata.normalizationFactor}).");
                 }
            }
             if (fileData.TryGetValue(DatasetMetadata.Key_DatasetName, out string nameStr)) {
                 metadata.datasetName = nameStr; // Direct assignment
             }


            // --- Populate List Fields ---

            // Indices (List<int>)
            if (fileData.TryGetValue(DatasetMetadata.Key_Indices, out string indicesStr) && !string.IsNullOrEmpty(indicesStr))
            {
                try
                {
                    metadata.indices = indicesStr.Split(ListSeparatorInt)
                                           .Select(s => int.Parse(s.Trim())) // Use Parse (throws exception on failure)
                                           .ToList();
                }
                catch (System.FormatException fe)
                {
                     Debug.LogWarning($"DatasetMetadataManager Warning: Could not parse one or more integers in '{DatasetMetadata.Key_Indices}' ('{indicesStr}') from '{filePath}'. Skipping invalid entries. Error: {fe.Message}");
                     // Fallback: Try parsing individually to keep valid ones
                     metadata.indices = new List<int>();
                     string[] parts = indicesStr.Split(ListSeparatorInt);
                     foreach(string part in parts) {
                         if(int.TryParse(part.Trim(), out int val)) {
                             metadata.indices.Add(val);
                         }
                     }
                }
            }
             else if (fileData.ContainsKey(DatasetMetadata.Key_Indices) && string.IsNullOrEmpty(indicesStr)) {
                 metadata.indices = new List<int>(); // Key exists but value is empty string
             }
            // else: Key not found, keep default empty list

            // Layer Names (List<string>)
            if (fileData.TryGetValue(DatasetMetadata.Key_LayerNames, out string layerNamesStr) && !string.IsNullOrEmpty(layerNamesStr))
            {
                 // Simple split for strings, Trim each part
                metadata.layerNames = layerNamesStr.Split(ListSeparatorString)
                                              .Select(s => s.Trim()) // Trim whitespace from each name
                                              .ToList();
            }
             else if (fileData.ContainsKey(DatasetMetadata.Key_LayerNames) && string.IsNullOrEmpty(layerNamesStr)) {
                 metadata.layerNames = new List<string>(); // Key exists but value is empty string
             }
            // else: Key not found, keep default empty list

            // Layer Types (List<int>) - Similar parsing to Indices
             if (fileData.TryGetValue(DatasetMetadata.Key_LayerTypes, out string layerTypesStr) && !string.IsNullOrEmpty(layerTypesStr))
            {
                try
                {
                    metadata.layerTypes = layerTypesStr.Split(ListSeparatorInt)
                                           .Select(s => int.Parse(s.Trim()))
                                           .ToList();
                }
                catch (System.FormatException fe)
                {
                     Debug.LogWarning($"DatasetMetadataManager Warning: Could not parse one or more integers in '{DatasetMetadata.Key_LayerTypes}' ('{layerTypesStr}') from '{filePath}'. Skipping invalid entries. Error: {fe.Message}");
                     metadata.layerTypes = new List<int>();
                     string[] parts = layerTypesStr.Split(ListSeparatorInt);
                     foreach(string part in parts) {
                         if(int.TryParse(part.Trim(), out int val)) {
                             metadata.layerTypes.Add(val);
                         }
                     }
                }
            }
             else if (fileData.ContainsKey(DatasetMetadata.Key_LayerTypes) && string.IsNullOrEmpty(layerTypesStr)) {
                 metadata.layerTypes = new List<int>(); // Key exists but value is empty string
             }
            // else: Key not found, keep default empty list

            Debug.Log($"Dataset metadata loaded successfully from: {filePath}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"DatasetMetadataManager Error: Failed to read or parse metadata from '{filePath}'. Returning default metadata. Exception: {e.Message}\n{e.StackTrace}");
            return new DatasetMetadata(); // Return default metadata on critical error
        }

        return metadata;
    }
}