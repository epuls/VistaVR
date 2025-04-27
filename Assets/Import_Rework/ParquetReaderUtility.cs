using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;
using Parquet;
using Parquet.Data;
using Parquet.Schema;

/// <summary>
/// Utility for reading Parquet files with Parquet.Net in Unity.
/// Provides both async methods and synchronous wrappers for convenience.
/// Requires Parquet.Net.dll in Assets/Plugins.
/// </summary>
public static class ParquetReaderUtility
{
    /// <summary>
    /// Async: Builds a dictionary mapping keyColumn -> valueColumn (string to string) from first row group.
    /// </summary>
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
    /// Async: Builds a dictionary mapping keyColumn -> Vector2(xColumn, yColumn) from first row group.
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

    /// <summary>
    /// Blocking call wrapper for ReadColumnDictionaryAsync.
    /// Note: This will block the calling thread until complete.
    /// </summary>
    public static Dictionary<string, string> ReadColumnDictionary(
        string parquetFile,
        string keyColumn,
        string valueColumn)
    {
        return ReadColumnDictionaryAsync(parquetFile, keyColumn, valueColumn)
            .GetAwaiter().GetResult();
    }

    /// <summary>
    /// Blocking call wrapper for ReadColumnVector2DictionaryAsync.
    /// Note: This will block the calling thread until complete.
    /// </summary>
    public static Dictionary<string, Vector2> ReadColumnVector2Dictionary(
        string parquetFile,
        string keyColumn,
        string xColumn,
        string yColumn)
    {
        return ReadColumnVector2DictionaryAsync(parquetFile, keyColumn, xColumn, yColumn)
            .GetAwaiter().GetResult();
    }
    
    

    /// <summary>
    /// Coroutine wrapper: asynchronously reads key/value columns without blocking main thread.
    /// Usage: StartCoroutine(ParquetReaderUtility.ReadColumnDictionaryCoroutine(
    ///     "file.parquet", "key", "value", dict => { /* use dict */ }, err => { /* handle error */ }));
    /// </summary>
    public static System.Collections.IEnumerator ReadColumnDictionaryCoroutine(
        string parquetFile,
        string keyColumn,
        string valueColumn,
        Action<Dictionary<string,string>> onSuccess,
        Action<Exception> onError = null)
    {
        var task = ReadColumnDictionaryAsync(parquetFile, keyColumn, valueColumn);
        // wait until task completes
        yield return new WaitUntil(() => task.IsCompleted);
        if (task.IsFaulted)
        {
            onError?.Invoke(task.Exception);
        }
        else
        {
            onSuccess(task.Result);
        }
    }

    /// <summary>
    /// Coroutine wrapper: asynchronously reads key/x/y columns into Vector2 without blocking main thread.
    /// Usage: StartCoroutine(ParquetReaderUtility.ReadColumnVector2DictionaryCoroutine(
    ///     "file.parquet", "key", "xCol", "yCol", vecDict => { /* use vector map */ }, err => { /* handle error */ }));
    /// </summary>
    public static System.Collections.IEnumerator ReadColumnVector2DictionaryCoroutine(
        string parquetFile,
        string keyColumn,
        string xColumn,
        string yColumn,
        Action<Dictionary<string,Vector2>> onSuccess,
        Action<Exception> onError = null)
    {
        var task = ReadColumnVector2DictionaryAsync(parquetFile, keyColumn, xColumn, yColumn);
        yield return new WaitUntil(() => task.IsCompleted);
        if (task.IsFaulted)
        {
            onError?.Invoke(task.Exception);
        }
        else
        {
            onSuccess(task.Result);
        }
    }
}

// Example usage elsewhere (non-async context):
// var map = ParquetReaderUtility.ReadColumnDictionary("clustering.parquet", "barcode", "cluster");
// var vecMap = ParquetReaderUtility.ReadColumnVector2Dictionary("clustering.parquet", "barcode", "x_coord", "y_coord");
