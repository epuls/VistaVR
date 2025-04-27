using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using Parquet;
using Parquet.Data;
using Parquet.Schema;

/// <summary>
/// A Unity MonoBehaviour that reads a Parquet file using Parquet.Net's async API,
/// logs available columns, and prints the first few entries of a specified column.
/// Requires Parquet.Net.dll in Assets/Plugins.
/// </summary>
public class VisiumParquetReader : MonoBehaviour
{
    [Tooltip("Path to the Parquet file (relative to Application.dataPath or absolute).")]
    public string parquetFile = "clustering.parquet";

    [Tooltip("Column name to preview first values of.")]
    public string previewColumn = "cluster";


    async void Start()
    {
        Verify();
        var map = await ReadColumnDictionaryAsync(
            parquetFile,
            "barcode",
            "UnsupervisedL1"
        );
        Debug.Log($"Found {map.Count} entries; example: {map.First()}");
        
        var vecMap = await ReadColumnVector2DictionaryAsync(
            parquetFile,
            "barcode",
            "X",
            "Y"
        );
        Debug.Log($"Got {vecMap.Count} entries; example: {vecMap.First()}");

    }

    async void Verify()
    {
        string fullPath = Path.Combine(Application.dataPath, parquetFile);
        if (!File.Exists(fullPath))
        {
            Debug.LogError($"Parquet file not found: {fullPath}");
            return;
        }

        try
        {
            // Create the reader asynchronously
            using ParquetReader reader = await ParquetReader.CreateAsync(fullPath);

            // 1) List out schema fields
            ParquetSchema schema = reader.Schema;
            Debug.Log("Parquet columns:");
            foreach (DataField field in schema.GetDataFields())
            {
                Debug.Log($"• {field.Name} ({field.ClrNullableIfHasNullsType.Name})");
            }

            // 2) Read entire first row group
            DataColumn[] columns = await reader.ReadEntireRowGroupAsync(0);

            // 3) Find and preview the specified column
            DataColumn preview = columns.FirstOrDefault(c => c.Field.Name == previewColumn);
            if (preview == null)
            {
                Debug.LogWarning($"Column '{previewColumn}' not found in Parquet file.");
                return;
            }

            // Extract raw data and log the first few values
            var rawData = preview.Data;
            var previewValues = rawData.Cast<object>().Take(10);
            Debug.Log($"First 10 values of '{previewColumn}': {string.Join(", ", previewValues)}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error reading Parquet file: {e.Message}");
        }
    }
    
    public static async Task<Dictionary<string, string>> ReadColumnDictionaryAsync(
        string parquetFile,
        string keyColumn,
        string valueColumn)
    {
        string fullPath = Path.Combine(Application.dataPath, parquetFile);
        if (!File.Exists(fullPath))
            throw new FileNotFoundException($"Parquet file not found: {fullPath}");

        using ParquetReader reader = await ParquetReader.CreateAsync(fullPath);
        DataColumn[] columns = await reader.ReadEntireRowGroupAsync(0);

        DataColumn keyCol = columns.FirstOrDefault(c => c.Field.Name == keyColumn)
                            ?? throw new KeyNotFoundException($"Key column '{keyColumn}' not found.");
        DataColumn valCol = columns.FirstOrDefault(c => c.Field.Name == valueColumn)
                            ?? throw new KeyNotFoundException($"Value column '{valueColumn}' not found.");

        var keys = keyCol.Data.Cast<object>().Select(o => o?.ToString() ?? string.Empty).ToArray();
        var vals = valCol.Data.Cast<object>().Select(o => o?.ToString() ?? string.Empty).ToArray();

        if (keys.Length != vals.Length)
            throw new InvalidDataException(
                $"Column length mismatch: '{keyColumn}' has {keys.Length}, '{valueColumn}' has {vals.Length}.");

        var dict = new Dictionary<string, string>(keys.Length);
        for (int i = 0; i < keys.Length; i++)
        {
            string k = keys[i];
            string v = vals[i];
            if (!dict.ContainsKey(k))
                dict.Add(k, v);
            else
                Debug.LogWarning($"Duplicate key '{k}' at row {i}, skipping duplicate.");
        }

        return dict;
    }
    
    /// <summary>
    /// Reads three columns from the first row group of a Parquet file and returns a dictionary
    /// mapping values from keyColumn to a Vector2 built from xColumn and yColumn.
    /// </summary>
    public static async Task<Dictionary<string, Vector2>> ReadColumnVector2DictionaryAsync(
        string parquetFile,
        string keyColumn,
        string xColumn,
        string yColumn)
    {
        string fullPath = Path.Combine(Application.dataPath, parquetFile);
        if (!File.Exists(fullPath))
            throw new FileNotFoundException($"Parquet file not found: {fullPath}");

        using ParquetReader reader = await ParquetReader.CreateAsync(fullPath);
        DataColumn[] columns = await reader.ReadEntireRowGroupAsync(0);

        DataColumn keyCol = columns.FirstOrDefault(c => c.Field.Name == keyColumn)
                            ?? throw new KeyNotFoundException($"Key column '{keyColumn}' not found.");
        DataColumn xCol = columns.FirstOrDefault(c => c.Field.Name == xColumn)
                          ?? throw new KeyNotFoundException($"X column '{xColumn}' not found.");
        DataColumn yCol = columns.FirstOrDefault(c => c.Field.Name == yColumn)
                          ?? throw new KeyNotFoundException($"Y column '{yColumn}' not found.");

        var keys = keyCol.Data.Cast<object>().Select(o => o?.ToString() ?? string.Empty).ToArray();
        var xs = xCol.Data.Cast<object>().Select(o => Convert.ToSingle(o)).ToArray();
        var ys = yCol.Data.Cast<object>().Select(o => Convert.ToSingle(o)).ToArray();

        if (keys.Length != xs.Length || xs.Length != ys.Length)
            throw new InvalidDataException(
                $"Column length mismatch: '{keyColumn}' has {keys.Length}, '{xColumn}' has {xs.Length}, '{yColumn}' has {ys.Length}.");

        var dict = new Dictionary<string, Vector2>(keys.Length);
        for (int i = 0; i < keys.Length; i++)
        {
            string k = keys[i];
            Vector2 v = new Vector2(xs[i], ys[i]);
            if (!dict.ContainsKey(k))
                dict.Add(k, v);
            else
                Debug.LogWarning($"Duplicate key '{k}' at row {i}, skipping duplicate.");
        }

        return dict;
    }
    
}
