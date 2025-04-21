using UnityEngine;
using System.IO; // Required for file operations
using UnityEngine.Experimental.Rendering; // For GraphicsFormat

public static class TextureLoader
{
    /// <summary>
    /// Loads an image file (PNG, JPG, EXR) from the specified path into a Texture2D.
    /// Note: This is a synchronous operation and can cause a frame hitch for large files.
    /// </summary>
    /// <param name="filePath">The full path to the image file.</param>
    /// <param name="mipChain">Should Unity generate mipmaps for the texture? (Default: true)</param>
    /// <param name="linear">Should the texture data be treated as linear color space? (Relevant for color accuracy, default: true - often desired for data/EXR)</param>
    /// <returns>A new Texture2D containing the loaded image data, or null if loading failed.</returns>
    public static Texture2D LoadTexture2DFromFile(string filePath, bool mipChain = true, bool linear = true)
    {
        if (string.IsNullOrEmpty(filePath))
        {
            Debug.LogError("TextureLoader Error: File path is null or empty.");
            return null;
        }

        if (!File.Exists(filePath))
        {
            Debug.LogError($"TextureLoader Error: File not found at path: {filePath}");
            return null;
        }

        Texture2D loadedTexture = null;
        byte[] fileData = null;

        try
        {
            // Read the entire file into a byte array
            fileData = File.ReadAllBytes(filePath);

            // Create a new Texture2D. The dimensions will be determined by LoadImage.
            // Pass 'linear' hint to constructor (3rd parameter) if supported, otherwise it's handled by LoadImage indirectly.
            // Note: The constructor's linear parameter mainly affects textures *created* blank,
            // LoadImage often overrides based on image metadata/format where possible.
            // Explicitly setting linear might be needed less often now, but doesn't hurt.
            loadedTexture = new Texture2D(2, 2, TextureFormat.RGBA32, mipChain, linear); // Default format, might change after LoadImage

            // Load the image data from the byte array.
            // This function will decode PNG, JPG, EXR formats and resize the texture.
            if (loadedTexture.LoadImage(fileData)) // Returns true on success
            {
                // Optionally force filtering mode or wrap mode if needed
                // loadedTexture.filterMode = FilterMode.Point; // Example: For nearest-neighbor sampling
                // loadedTexture.wrapMode = TextureWrapMode.Clamp;

                // Apply changes (needed depending on what's done after LoadImage, though often implicit)
                // loadedTexture.Apply(false, true); // (updateMipmaps=false, makeNoLongerReadable=true to save memory) - be cautious with makeNoLongerReadable

                Debug.Log($"Texture2D loaded successfully from: {filePath} (Format: {loadedTexture.format}, GraphicsFormat: {loadedTexture.graphicsFormat}, Size: {loadedTexture.width}x{loadedTexture.height})");
                return loadedTexture; // Success!
            }
            else
            {
                Debug.LogError($"TextureLoader Error: Failed to load image data from file (potentially corrupt or unsupported format variation): {filePath}");
                Object.Destroy(loadedTexture); // Clean up the failed texture object
                return null;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"TextureLoader Error: Exception occurred while loading file '{filePath}'. Exception: {e.Message}\n{e.StackTrace}");
            if (loadedTexture != null)
            {
                Object.Destroy(loadedTexture); // Clean up if texture was created before exception
            }
            return null;
        }
        // Note: 'fileData' byte array will be garbage collected.
    }


    /// <summary>
    /// Loads an image file (PNG, JPG, EXR) from the specified path into a new RenderTexture.
    /// This involves loading into a temporary Texture2D first, then blitting to the RenderTexture.
    /// </summary>
    /// <param name="filePath">The full path to the image file.</param>
    /// <param name="renderTextureFormat">Optional: Specify the desired format for the RenderTexture. If Default, it attempts to match the loaded Texture2D's format.</param>
    /// <param name="useMipMap">Should the RenderTexture have mipmaps enabled? (Default: false)</param>
    /// <param name="linear">Hint for loading Texture2D data (passed to LoadTexture2DFromFile). (Default: true)</param>
    /// <returns>A new RenderTexture containing the loaded image data, or null if loading failed.</returns>
    public static RenderTexture LoadRenderTextureFromFile(
        string filePath,
        RenderTextureFormat renderTextureFormat = RenderTextureFormat.Default,
        bool useMipMap = false,
        bool linear = true)
    {
        // 1. Load the image into a Texture2D first
        Texture2D tempTex = LoadTexture2DFromFile(filePath, useMipMap, linear);

        if (tempTex == null)
        {
            // Error already logged by LoadTexture2DFromFile
            return null;
        }

        RenderTexture rt = null;
        try
        {
            // 2. Determine the RenderTexture format
            GraphicsFormat rtGraphicsFormat;
            if (renderTextureFormat == RenderTextureFormat.Default || renderTextureFormat == RenderTextureFormat.DefaultHDR)
            {
                // Try to match the Texture2D's format
                 rtGraphicsFormat = tempTex.graphicsFormat;
                 // Fallback if format is unsupported for Render Textures (less common now with GraphicsFormat)
                 if (!SystemInfo.IsFormatSupported(rtGraphicsFormat, FormatUsage.Render))
                 {
                    Debug.LogWarning($"TextureLoader Warning: GraphicsFormat '{rtGraphicsFormat}' loaded from Texture2D is not supported for RenderTextures. Falling back to ARGB32.");
                    rtGraphicsFormat = GraphicsFormat.R8G8B8A8_UNorm; // A safe default
                 }
            }
            else
            {
                // Use the user-specified format
                rtGraphicsFormat = GraphicsFormatUtility.GetGraphicsFormat(renderTextureFormat, QualitySettings.activeColorSpace == ColorSpace.Linear);
                 if (!SystemInfo.IsFormatSupported(rtGraphicsFormat, FormatUsage.Render))
                 {
                    Debug.LogError($"TextureLoader Error: Specified RenderTextureFormat '{renderTextureFormat}' (GraphicsFormat: {rtGraphicsFormat}) is not supported by the system for Render usage.");
                    Object.Destroy(tempTex); // Clean up Texture2D
                    return null;
                 }
            }


            // 3. Create the RenderTexture
            rt = new RenderTexture(tempTex.width, tempTex.height, 0, rtGraphicsFormat); // 0 depth bits usually okay
            rt.useMipMap = useMipMap && tempTex.mipmapCount > 1; // Only use mipmap if source had them and requested
            rt.autoGenerateMips = false; // We will blit, not generate dynamically

            // Optional: Enable random write if you intend to use compute shaders on it
            // rt.enableRandomWrite = true;

            if (!rt.Create())
            {
                 Debug.LogError($"TextureLoader Error: Failed to create RenderTexture (Size: {rt.width}x{rt.height}, Format: {rt.graphicsFormat}).");
                 Object.Destroy(tempTex); // Clean up Texture2D
                 Object.Destroy(rt);      // Clean up failed RT
                 return null;
            }

            // 4. Blit (copy) the Texture2D data to the RenderTexture
            // Graphics.Blit handles potential color space conversions if necessary
            Graphics.Blit(tempTex, rt);

             // 5. Generate MipMaps for the RenderTexture if requested and possible
            if (rt.useMipMap)
            {
                rt.GenerateMips();
            }

            Debug.Log($"RenderTexture created and loaded successfully from: {filePath} (Size: {rt.width}x{rt.height}, Format: {rt.graphicsFormat})");

            return rt; // Success!
        }
        catch (System.Exception e)
        {
             Debug.LogError($"TextureLoader Error: Exception occurred while creating or blitting to RenderTexture for file '{filePath}'. Exception: {e.Message}\n{e.StackTrace}");
             if (rt != null) Object.Destroy(rt); // Clean up potential partial RT
             // tempTex will be cleaned up below
             return null;
        }
        finally
        {
            // 6. Clean up the temporary Texture2D in all cases (success or failure after this point)
            if (tempTex != null)
            {
                Object.Destroy(tempTex);
            }
        }
    }
}