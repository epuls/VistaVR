using UnityEngine;
using System.IO; // Required for file operations
using UnityEngine.Experimental.Rendering; // Required for GraphicsFormat
using Unity.Collections; // Required if needing low-level access (not strictly needed for EncodeTo methods)

public static class RenderTextureSaver
{
    /// <summary>
    /// Saves the contents of a RenderTexture to an image file.
    /// Prefers EXR format for uncompressed, high-fidelity saving.
    /// Can also save as PNG (lossless compression, typically 8-bit).
    /// </summary>
    /// <param name="rt">The RenderTexture to save.</param>
    /// <param name="filePath">The full path (including filename and extension) where the image will be saved.</param>
    /// <param name="format">The desired output format (EXR or PNG).</param>
    /// <returns>True if saving was successful, false otherwise.</returns>
    public static bool SaveRenderTextureToFile(RenderTexture rt, string filePath, ImageFormat format = ImageFormat.EXR)
    {
        if (rt == null)
        {
            Debug.LogError("RenderTextureSaver Error: Provided RenderTexture is null.");
            return false;
        }

        if (string.IsNullOrEmpty(filePath))
        {
            Debug.LogError("RenderTextureSaver Error: File path is null or empty.");
            return false;
        }

        // Ensure the output directory exists
        string directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            try
            {
                Directory.CreateDirectory(directory);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"RenderTextureSaver Error: Could not create directory '{directory}'. Exception: {e.Message}");
                return false;
            }
        }

        RenderTexture previousActive = RenderTexture.active; // Store the currently active render texture
        RenderTexture.active = rt; // Set the target texture as active

        // Create a Texture2D to read the pixels into
        // Use the RenderTexture's dimensions and graphics format for accuracy
        // TextureCreationFlags.None is suitable for ReadPixels
        Texture2D tempTex = new Texture2D(rt.width, rt.height, rt.graphicsFormat, TextureCreationFlags.None);

        bool success = false;
        try
        {
            // Read pixels from the active RenderTexture into the Texture2D
            // Note: This operation can be slow, especially for large textures
            Rect readRect = new Rect(0, 0, rt.width, rt.height);
            tempTex.ReadPixels(readRect, 0, 0);
            tempTex.Apply(); // Apply the pixel changes to the Texture2D

            byte[] encodedBytes;

            // Encode the Texture2D to the chosen format
            switch (format)
            {
                case ImageFormat.EXR:
                    // Determine EXR flags based on texture format for best results
                    Texture2D.EXRFlags exrFlags = Texture2D.EXRFlags.None; // Default: Uncompressed half-float

                    // Use OutputAsFloat if the source render texture is 32-bit float per channel
                    if (rt.graphicsFormat == GraphicsFormat.R32G32B32A32_SFloat) // Check for full float format
                    {
                        exrFlags = Texture2D.EXRFlags.OutputAsFloat;
                    }
                    // Note: Add checks for other float formats if needed (e.g., R16G16B16A16_SFloat also uses None/Half)

                    encodedBytes = tempTex.EncodeToEXR(exrFlags);
                    // Ensure file path has the correct extension
                    if (!filePath.EndsWith(".exr", System.StringComparison.OrdinalIgnoreCase))
                    {
                        filePath += ".exr";
                        Debug.LogWarning($"RenderTextureSaver Warning: File path did not end with .exr, appending it. Final path: {filePath}");
                    }
                    break;

                case ImageFormat.PNG:
                default:
                    // PNG is typically RGBA32 (8 bits per channel) and uses lossless compression.
                    encodedBytes = tempTex.EncodeToPNG();
                     // Ensure file path has the correct extension
                    if (!filePath.EndsWith(".png", System.StringComparison.OrdinalIgnoreCase))
                    {
                        filePath += ".png";
                        Debug.LogWarning($"RenderTextureSaver Warning: File path did not end with .png, appending it. Final path: {filePath}");
                    }
                    break;
            }


            // Write the encoded bytes to the specified file path
            File.WriteAllBytes(filePath, encodedBytes);
            Debug.Log($"RenderTexture saved successfully to: {filePath}");
            success = true;

        }
        catch (System.Exception e)
        {
            Debug.LogError($"RenderTextureSaver Error: Failed to save RenderTexture to '{filePath}'. Exception: {e.Message}\n{e.StackTrace}");
            success = false;
        }
        finally
        {
            // --- Cleanup ---
            // Restore the previously active render texture
            RenderTexture.active = previousActive;

            // Destroy the temporary Texture2D to free up memory
            if (Application.isPlaying)
            {
                UnityEngine.Object.Destroy(tempTex);
            }
            else
            {
                // Use DestroyImmediate if potentially called from the editor outside play mode
                UnityEngine.Object.DestroyImmediate(tempTex);
            }
        }

        return success;
    }

    /// <summary>
    /// Defines the supported image formats for saving.
    /// </summary>
    public enum ImageFormat
    {
        /// <summary>
        /// OpenEXR format. Best for uncompressed high dynamic range (HDR) or high bit-depth data.
        /// Saved as uncompressed 16-bit half-float or 32-bit float depending on RenderTexture format.
        /// </summary>
        EXR,
        /// <summary>
        /// Portable Network Graphics format. Uses lossless compression. Typically 8-bit per channel (RGBA32).
        /// Good for standard dynamic range images where perfect fidelity isn't paramount but file size matters more than EXR.
        /// </summary>
        PNG
    }
}