using UnityEngine;
using System.IO;
using System.Collections.Generic;
using System.Linq; // Needed for OrderBy

public static class DatasetMetadataManager_Json
{
    /// <summary>
    /// Saves the provided DatasetMetadataContainer object to a JSON file.
    /// </summary>
    /// <param name="metadataContainer">The container object with all data to save.</param>
    /// <param name="filePath">The full path to the file where metadata will be saved (e.g., "Assets/Data/MyDataset.json").</param>
    /// <param name="prettyPrint">Whether to format the JSON for human readability.</param>
    /// <returns>True if saving was successful, false otherwise.</returns>
    public static bool SaveMetadata(DatasetMetadataContainer metadataContainer, string filePath, bool prettyPrint = true)
    {
        if (metadataContainer == null)
        {
            Debug.LogError("DatasetMetadataManager_Json Error: Metadata container provided is null.");
            return false;
        }
        if (string.IsNullOrEmpty(filePath))
        {
            Debug.LogError("DatasetMetadataManager_Json Error: File path is null or empty.");
            return false;
        }

        // Ensure the directory exists
        string directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directory)) // Could be null if saving to root (not recommended)
        {
            try
            {
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                    Debug.Log($"Created directory: {directory}");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"DatasetMetadataManager_Json Error: Could not create directory '{directory}'. Exception: {e.Message}");
                return false;
            }
        }
        else
        {
             Debug.LogWarning($"DatasetMetadataManager_Json Warning: Saving metadata without a specific directory path: '{filePath}'");
        }


        try
        {
            // Ensure layers are sorted by index before saving, if desired
            // You might want to do this *before* calling SaveMetadata depending on your workflow
             metadataContainer.Layers = metadataContainer.Layers?.OrderBy(l => l.Index).ToList() ?? new List<LayerData>();


            // Serialize the object to JSON format
            string jsonString = JsonUtility.ToJson(metadataContainer, prettyPrint);

            // Write the JSON string to the file
            File.WriteAllText(filePath, jsonString, System.Text.Encoding.UTF8);

            Debug.Log($"Dataset metadata saved successfully to: {filePath}");
            return true;
        }
        catch (System.Exception e)
        {
            // Catch potential exceptions during serialization or file I/O
            Debug.LogError($"DatasetMetadataManager_Json Error: Failed to save metadata to '{filePath}'. Exception: {e.Message}\n{e.StackTrace}");
            return false;
        }
    }

    /// <summary>
    /// Loads metadata from a JSON file into a new DatasetMetadataContainer object.
    /// </summary>
    /// <param name="filePath">The full path to the JSON metadata file.</param>
    /// <returns>A populated DatasetMetadataContainer object, or null if the file doesn't exist or an error occurs during loading/parsing.</returns>
    public static DatasetMetadataContainer LoadMetadata(string filePath)
    {
        if (string.IsNullOrEmpty(filePath))
        {
            Debug.LogError("DatasetMetadataManager_Json Error: File path provided for loading is null or empty.");
            return null; // Return null to indicate failure clearly
        }

        if (!File.Exists(filePath))
        {
            Debug.LogWarning($"DatasetMetadataManager_Json Info: Metadata file not found at '{filePath}'. Cannot load.");
            return null; // Return null as the file doesn't exist
        }

        try
        {
            // Read the entire JSON file content
            string jsonString = File.ReadAllText(filePath, System.Text.Encoding.UTF8);

            // Deserialize the JSON string back into our container object
            DatasetMetadataContainer loadedData = JsonUtility.FromJson<DatasetMetadataContainer>(jsonString);

            if (loadedData == null)
            {
                 Debug.LogError($"DatasetMetadataManager_Json Error: Failed to parse JSON from '{filePath}'. JsonUtility returned null. The file might be empty, malformed, or not represent a DatasetMetadataContainer.");
                 return null;
            }

            // Ensure the Layers list is not null after loading, even if empty in JSON
            if (loadedData.Layers == null)
            {
                loadedData.Layers = new List<LayerData>();
            }

             // Optional: Sort layers by index upon loading if needed
             loadedData.Layers = loadedData.Layers.OrderBy(l => l.Index).ToList();


            Debug.Log($"Dataset metadata loaded successfully from: {filePath}");
            return loadedData;
        }
        catch (System.ArgumentException argEx) // JsonUtility often throws this for parse errors
        {
             Debug.LogError($"DatasetMetadataManager_Json Error: Failed to parse JSON from '{filePath}'. The file might be malformed. Exception: {argEx.Message}\n{argEx.StackTrace}");
             return null;
        }
        catch (System.Exception e) // Catch other potential exceptions (e.g., file I/O)
        {
            Debug.LogError($"DatasetMetadataManager_Json Error: Failed to read or parse metadata from '{filePath}'. Exception: {e.Message}\n{e.StackTrace}");
            return null; // Return null on critical error
        }
    }
}