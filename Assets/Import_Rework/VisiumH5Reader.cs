using UnityEngine;
using HDF5.NET;

/// <summary>
/// A simple Unity MonoBehaviour that opens a Visium-style HDF5 file
/// and logs the total number of barcodes plus the first barcode string.
/// Requires HDF5.NET (NuGet) and the native HDF5 library in Assets/Plugins.
/// </summary>
public class VisiumH5Reader : MonoBehaviour
{
    //[Tooltip("Path to the Visium HDF5 file (relative to project root or absolute).")]
    private string h5FilePath = "C:\\Users\\epuls\\Downloads\\feat_mtx.h5";

    void Start()
    {
        // Open the HDF5 file in read-only mode
        using var file = H5File.OpenRead(h5FilePath);

        // Access the "matrix/barcodes" dataset
        var barcodesDataset = file.Dataset("matrix/barcodes");

        // Read all barcodes into a string array
        string[] barcodes = barcodesDataset.ReadString();

        // Log the number of barcodes
        Debug.Log($"Barcodes count: {barcodes.Length}");

        // Log the first barcode if available
        if (barcodes.Length > 0)
        {
            Debug.Log($"First barcode: {barcodes[0]}, Length: {barcodes.Length}");
        }
        else
        {
            Debug.LogWarning("No barcodes found in the HDF5 dataset.");
        }
    }
}