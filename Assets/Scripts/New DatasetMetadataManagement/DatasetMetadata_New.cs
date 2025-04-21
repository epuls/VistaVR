using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// A simple struct to hold RGB color values (0-255) for JSON serialization,
/// as JsonUtility doesn't handle UnityEngine.Color exactly as needed here.
/// </summary>
[System.Serializable]
public struct SerializableColor
{
    public int R;
    public int G;
    public int B;

    // Constructor
    public SerializableColor(int r, int g, int b)
    {
        R = Mathf.Clamp(r, 0, 255);
        G = Mathf.Clamp(g, 0, 255);
        B = Mathf.Clamp(b, 0, 255);
    }

    // Conversion to Unity's Color (assumes full alpha)
    public Color ToUnityColor()
    {
        return new Color(R / 255f, G / 255f, B / 255f, 1f);
    }

    // Static factory method to create from Unity's Color
    public static SerializableColor FromUnityColor(Color unityColor)
    {
        return new SerializableColor(
            Mathf.RoundToInt(unityColor.r * 255),
            Mathf.RoundToInt(unityColor.g * 255),
            Mathf.RoundToInt(unityColor.b * 255)
        );
    }
}

/// <summary>
/// Represents the data for a single layer to be saved/loaded via JSON.
/// </summary>
[System.Serializable]
public class LayerData
{
    public string LayerName;
    public string FileName; // Assuming each layer might reference a specific file
    public int Index;
    public int LayerType; // Integer representation (e.g., 0=TypeA, 1=TypeB)
    public SerializableColor Color;

    // Default constructor (needed for deserialization)
    public LayerData() { }

    // Convenience constructor
    public LayerData(string name, string file, int index, int type, Color unityColor)
    {
        LayerName = name;
        FileName = file;
        Index = index;
        LayerType = type;
        Color = SerializableColor.FromUnityColor(unityColor);
    }

     // Add a constructor that takes a Layer object (assuming your Layer class exists)
    public LayerData(Layer sourceLayer) // Assuming 'Layer' is your existing class
    {
        if (sourceLayer != null)
        {
            LayerName = sourceLayer.Name; // Replace with actual property names
            FileName = sourceLayer.AssociatedFileName; // Replace with actual property names
            Index = sourceLayer.Index; // Replace with actual property names
            LayerType = (int)sourceLayer.LayerType; // Replace with actual property names, cast enum if needed
            Color = SerializableColor.FromUnityColor(sourceLayer.Color); // Replace with actual property names
        }
        else
        {
             Debug.LogError("Cannot create LayerData from a null Layer object.");
             // Initialize with defaults or throw an exception
             LayerName = "ErrorLayer";
             FileName = "";
             Index = -1;
             LayerType = 0;
             Color = new SerializableColor(255, 0, 255); // Default error color (Magenta)
        }
    }

    // Method to convert back to your Layer class (you'll need to implement this)
     public Layer ToLayerObject()
     {
         // Create a new Layer instance and populate it from this LayerData
         Layer layer = new Layer();
         layer.Name = this.LayerName;
         layer.AssociatedFileName = this.FileName;
         layer.Index = this.Index;
         layer.LayerType = (Layer.DataType)this.LayerType; // Cast back to enum
         layer.Color = this.Color.ToUnityColor();
         return layer;
     }
}


/// <summary>
/// The main container class that holds all metadata, designed for JSON serialization.
/// This is the object that will be directly converted to/from JSON.
/// </summary>
[System.Serializable]
public class DatasetMetadataContainer
{
    // --- Dataset Level Metadata ---
    public string DatasetName;
    public int LayerResolution; // Assuming int based on previous code
    public float NormalizationFactor;
    public int ClusterCount;

    // --- Layers ---
    // List to hold all the individual layer data. JsonUtility requires lists/arrays.
    public List<LayerData> Layers;

    // Constructor to initialize the list
    public DatasetMetadataContainer()
    {
        DatasetName = "Untitled Dataset";
        LayerResolution = 512; // Example default
        NormalizationFactor = 1.0f; // Example default
        ClusterCount = 0; // Example default
        Layers = new List<LayerData>();
    }
}

// --- Placeholder for your Layer class (adapt property names) ---
// You would have your actual Layer class definition elsewhere


