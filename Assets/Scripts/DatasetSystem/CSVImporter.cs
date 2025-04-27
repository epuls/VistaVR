using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
public class CSVImporter : MonoBehaviour
{
    /// <summary>
        /// Loads a CSV file and converts its data into a dictionary.
        /// The CSV file should have a header row and columns in this order:
        /// Cell Barcode, X Coordinate, Y Coordinate.
        /// </summary>
        /// <param name="filePath">The full path to the CSV file.</param>
        /// <returns>
        /// A dictionary with the cell barcode as the key and a Vector2 containing the X and Y coordinates as the value.
        /// </returns>
        public static Dictionary<string, Vector2> LoadCsvToDictionary(string filePath)
        {
            Dictionary<string, Vector2> cellDictionary = new Dictionary<string, Vector2>();
    
            if (!File.Exists(filePath))
            {
                Debug.LogError($"CSV file not found at path: {filePath}");
                return cellDictionary;
            }
    
            try
            {
                // Read all lines from the CSV file.
                string[] lines = File.ReadAllLines(filePath);
    
                if (lines.Length <= 1)
                {
                    Debug.LogWarning("CSV file does not contain any data rows.");
                    return cellDictionary;
                }
    
                // Start at index 1 to skip the header row.
                for (int i = 1; i < lines.Length; i++)
                {
                    string line = lines[i];
    
                    // Skip empty or whitespace-only lines.
                    if (string.IsNullOrWhiteSpace(line))
                        continue;
    
                    // Split the line into parts using comma as a separator.
                    string[] parts = line.Split(',');
    
                    if (parts.Length < 3)
                    {
                        Debug.LogWarning($"Line {i + 1} does not have the required three columns. Skipping line.");
                        continue;
                    }
    
                    string cellBarcode = parts[0].Trim();
    
                    // Parse the coordinates. Using float.TryParse to avoid exceptions on invalid formats.
                    if (!float.TryParse(parts[1].Trim(), out float x))
                    {
                        Debug.LogWarning($"Unable to parse X coordinate on line {i + 1}. Skipping line.");
                        continue;
                    }
                    if (!float.TryParse(parts[2].Trim(), out float y))
                    {
                        Debug.LogWarning($"Unable to parse Y coordinate on line {i + 1}. Skipping line.");
                        continue;
                    }
    
                    // Create a Vector2 from the parsed coordinates and add to the dictionary.
                    cellDictionary[cellBarcode] = new Vector2(x, y) / 27762f;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"An error occurred while loading the CSV file: {ex.Message}");
            }
    
            return cellDictionary;
        }
    
    
    public static Dictionary<string, int> LoadCsvToClusterDictionary(string filePath)
    {
        Dictionary<string, int> clusterDictionary = new Dictionary<string, int>();

        if (!File.Exists(filePath))
        {
            Debug.LogError($"CSV file not found at path: {filePath}");
            return clusterDictionary;
        }

        try
        {
            // Read all lines from the CSV file.
            string[] lines = File.ReadAllLines(filePath);

            if (lines.Length <= 1)
            {
                Debug.LogWarning("CSV file does not contain any data rows.");
                return clusterDictionary;
            }

            // Start at index 1 to skip the header row.
            for (int i = 1; i < lines.Length; i++)
            {
                string line = lines[i];

                // Skip empty or whitespace lines.
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                // Split the line into parts using comma as a separator.
                string[] parts = line.Split(',');

                if (parts.Length < 2)
                {
                    Debug.LogWarning($"Line {i + 1} does not have the required two columns. Skipping line.");
                    continue;
                }

                string cellBarcode = parts[0].Trim();
                string clusterText = parts[1].Trim();

                // Expecting clusterText in the format "Cluster X", so remove the prefix "Cluster " to parse the number.
                int clusterNumber = 0;
                if (clusterText.StartsWith("Cluster ", StringComparison.OrdinalIgnoreCase))
                {
                    string clusterNumberString = clusterText.Substring("Cluster ".Length);
                    if (!int.TryParse(clusterNumberString, out clusterNumber))
                    {
                        Debug.LogWarning($"Unable to parse cluster number from '{clusterText}' on line {i + 1}. Skipping line.");
                        continue;
                    }
                }
                else
                {
                    Debug.LogWarning($"Expected 'Cluster <number>' format but got '{clusterText}' on line {i + 1}. Skipping line.");
                    continue;
                }

                // Add the cell barcode and cluster number to the dictionary.
                clusterDictionary[cellBarcode] = clusterNumber;
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"An error occurred while loading the CSV file: {ex.Message}");
        }

        return clusterDictionary;
    }
}
