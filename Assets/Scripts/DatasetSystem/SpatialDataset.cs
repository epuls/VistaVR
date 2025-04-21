using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class SpatialDataset : MonoBehaviour
{
    public Dictionary<string, Vector2> Cells;
    public Dictionary<string, int> Clusters;

    public GameObject spawnParent;
    public GameObject bakedClustersParent;
    public GameObject cellSquare;
    public GameObject bakeLayerPrefab;
    
    // Raw Dataset Panel
    public TMP_InputField spatialProjectionPathInputField;
    public TMP_InputField cellClusteringPathInputField;
    public TMP_InputField bakedDatasetPathInputField;
    
    public TMP_InputField loadBakedDatasetPathInputField;
    
    public bool limitCellSpawn = true; 
    public int cellCountToSpawn = 1000;
    public float normalizationFactor = 1000;
    public Vector3 offset = Vector3.zero;

    public Camera BakeCamera;
    public GameObject BakeCameraPrefab;
    public int ClusterToSpawn;
    public int ClusterCount = 11;
    private int curCluster = 1;

    public List<LayerUI> filters;

    public bool AutoBake = false;
    public bool AutoLoadMostRecent = false; // NEED TO IMPLEMENT
    public string cellBarcodeFilePath;
    public string cellClusterFilePath;
    public string bakedDatasetPath;
    
    
    public bool datasetLoaded = false;
    public GameObject DatasetPanel;

    public string DatasetName = "Test_Dataset";
    private string bakedClusterImageFileName = "cluster_";
    private string metaDataFileName = "dataset_info.txt";
    
    void Start()
    {
        if(AutoBake) LoadAndBake(cellBarcodeFilePath, cellClusterFilePath);

        
    }

    void OnEnable()
    {
        if (!datasetLoaded)
        {
            DatasetPanel.SetActive(true);
        }
        
        spatialProjectionPathInputField.onValueChanged.AddListener(SetSpatialProjPath);
        cellClusteringPathInputField.onValueChanged.AddListener(SetClusterPath);
        bakedDatasetPathInputField.onValueChanged.AddListener(SetBakePath);
        loadBakedDatasetPathInputField.onValueChanged.AddListener(SetBakePath);
        
        LayerEvents.ImportImageLayerEvent.AddListener(ImportImageLayer);
    }

    private void OnDisable()
    {
        spatialProjectionPathInputField.onValueChanged.RemoveListener(SetSpatialProjPath);
        cellClusteringPathInputField.onValueChanged.RemoveListener(SetClusterPath);
        bakedDatasetPathInputField.onValueChanged.RemoveListener(SetBakePath);
        loadBakedDatasetPathInputField.onValueChanged.RemoveListener(SetBakePath);
        
        LayerEvents.ImportImageLayerEvent.RemoveListener(ImportImageLayer);
    }

    void SetSpatialProjPath(string value)
    {
        cellBarcodeFilePath = value;
    }

    void SetClusterPath(string value)
    {
        cellClusterFilePath = value;
    }

    void SetBakePath(string value)
    {
        bakedDatasetPath = value;
    }

    public void LoadAndBakeButton()
    {
        LoadAndBake(cellBarcodeFilePath, cellClusterFilePath);
        DatasetPanel.SetActive(false);
    }
    
    
    void LoadAndBake(string cellPath, string clusterPath)
    {
        Cells = CSVImporter.LoadCsvToDictionary(cellPath); 
        Clusters = CSVImporter.LoadCsvToClusterDictionary(clusterPath);

        
        
        Debug.Log(Clusters.Count);
        Debug.Log(Clusters["s_008um_00269_00526-1"]);

        normalizationFactor = FindMaxAbsoluteValue(Cells);
        
        Debug.Log($"Normalization Factor: {normalizationFactor}");
        
        Debug.Log($"Baked clusters parent? : {bakedClustersParent}, cluster parent? : {spawnParent}");
        BakeCluster(curCluster, 1024, bakedClustersParent);
    }


    /// <summary>
    /// Finds the maximum absolute value from the x or y component of any Vector2 in the dictionary.
    /// </summary>
    /// <param name="vectorDict">Dictionary with string keys and Vector2 values.</param>
    /// <returns>The maximum absolute value found.</returns>
    public static float FindMaxAbsoluteValue(Dictionary<string, Vector2> vectorDict)
    {
        float maxAbsValue = 0f;

        foreach (KeyValuePair<string, Vector2> entry in vectorDict)
        {
            // Get the absolute value for both x and y components
            float absX = Mathf.Abs(entry.Value.x);
            float absY = Mathf.Abs(entry.Value.y);

            // Determine the larger value for the current vector
            float currentMax = (absX > absY) ? absX : absY;

            // Update the overall maximum if necessary
            if (currentMax > maxAbsValue)
            {
                maxAbsValue = currentMax;
            }
        }

        return maxAbsValue;
    }


    void DebugSpawnCells()
    {
        GameObject spawnedCell = null;
        int count = 0;
        foreach (KeyValuePair<string, Vector2> cell in Cells)
        {
            Vector2 coords = cell.Value;
            Vector3 normalizedCoords = new Vector3(coords.x / normalizationFactor, 0, coords.y / normalizationFactor);
            
            normalizedCoords += offset;
            
            spawnedCell = GameObject.Instantiate(cellSquare, Vector3.zero, Quaternion.identity);
            spawnedCell.transform.SetParent(spawnParent.transform);
            spawnedCell.transform.localPosition = normalizedCoords;
            
            if (limitCellSpawn)
            {
                count++;
                if (count == cellCountToSpawn) break;
            }
        }
    }
    
    void DebugSpawnClusters()
    {
        GameObject spawnedCell = null;
        int count = 0;
        foreach (KeyValuePair<string, Vector2> cell in Cells)
        {
            Vector2 coords = cell.Value;
            Vector3 normalizedCoords = new Vector3(coords.x / normalizationFactor, 0, coords.y / normalizationFactor);
            
            normalizedCoords += offset;


            if (Clusters[cell.Key] == ClusterToSpawn)
            {
                
                if (limitCellSpawn)
                {
                    count++;
                    if (count == cellCountToSpawn) break;
                }
                spawnedCell = GameObject.Instantiate(cellSquare, Vector3.zero, Quaternion.identity);
                spawnedCell.transform.SetParent(spawnParent.transform);
                spawnedCell.transform.localPosition = normalizedCoords;
            }
                
            
        }
    }

    void BakeCluster(int id, int textureRes, GameObject clusterParent)
    {
        //  Spawn datalayer prefab and set parent to clusterParent
        var bakedLayerObj = GameObject.Instantiate(bakeLayerPrefab, Vector3.zero, Quaternion.identity);
        bakedLayerObj.name = $"Baked_Cluster_{id}";
        bakedLayerObj.transform.SetParent(clusterParent.transform);
        
        var tmpPos = bakedLayerObj.transform.localPosition;
        bakedLayerObj.transform.localPosition = tmpPos;
        
        //filters[id-1].SetLayer(bakedLayerObj.transform, $"Cluster {id}");
        
        //  Create temporary spawn location for cells
        var tmpParent = new GameObject();
        tmpParent.transform.SetParent(spawnParent.transform);
        tmpParent.name = $"Cluster_{id}";
        tmpParent.transform.localScale = Vector3.one;
        tmpParent.transform.localPosition = Vector3.zero;
        
        GameObject spawnedCell = null;

        var layerTex = CreateRenderTexture(textureRes);
        bakedLayerObj.GetComponent<BakedDataLayer>().LayerRT = layerTex;
        bakedLayerObj.GetComponent<MeshRenderer>().material.SetTexture("_BaseMap", layerTex);
        
        
        BakeCamera.targetTexture = layerTex;
        BakeCamera.gameObject.SetActive(true);
        BakeCamera.enabled = true;
      
        
        
        
        foreach (KeyValuePair<string, Vector2> cell in Cells)
        {
            Vector2 coords = cell.Value;
            Vector3 normalizedCoords = new Vector3(coords.x / normalizationFactor, 0, coords.y / normalizationFactor);
            
            normalizedCoords += offset;


            if (Clusters[cell.Key] == id)
            {
                spawnedCell = GameObject.Instantiate(cellSquare, Vector3.zero, Quaternion.identity);
                spawnedCell.transform.SetParent(tmpParent.transform);
                spawnedCell.transform.localPosition = normalizedCoords;
            }
                
            
        }
    
        
        StartCoroutine(WaitAndCapture(tmpParent, BakeCamera, bakedLayerObj, layerTex));

    }

    private IEnumerator WaitAndCapture(GameObject spawnedCells, Camera bcam, GameObject bakedLayerObj, RenderTexture layerTex)
    {
        Debug.Log("Waiting for camera capture");
        yield return new WaitForSecondsRealtime(1.0f);
        bcam.enabled = false;
        bcam.targetTexture = null;
        bcam.gameObject.SetActive(false);
        spawnedCells.SetActive(false);
        
        // NOTE: This is very important. This builds our UI representation of a layer.
        LayerManager.Instance.BuildLayer(bakedLayerObj, "Cluster", layerTex, false);

        
        if (curCluster < ClusterCount)
        {
            curCluster += 1;
            StartCoroutine(WaitAndCapAgain());
        }
        else
        {
            //LayerManager.Instance.BuildDatasetLayers(bakedClustersParent, "Cluster");
            Debug.Log("Dataset loaded!");
            SaveLayerTextures();
            SaveMetadata();
            
        }
    }

    void SaveMetadata()
    {
        // 1. Create an instance of the metadata container
        DatasetMetadata dataToSave = new DatasetMetadata();

        // 2. Populate it with current values
        dataToSave.clusterCount = ClusterCount;
        dataToSave.normalizationFactor = normalizationFactor;
        dataToSave.datasetName = DatasetName;
        dataToSave.indices = new List<int>();
        dataToSave.layerNames = new List<string>();
        dataToSave.layerTypes = new List<int>();
        
        for (int i = 0; i < LayerManager.Instance.Layers.Count; i++)
        {
            Layer cur = LayerManager.Instance.Layers[i];
            dataToSave.indices.Add(cur.Index);
            dataToSave.layerNames.Add(cur.Name);
            dataToSave.layerTypes.Add((int)cur.LayerType);
        }
        // Set any other fields you added...

        // 3. Define the save path
        string fileName = metaDataFileName;
        string fullPath = Path.Combine(bakedDatasetPath, fileName);

        // 4. Call the static SaveMetadata function
        bool success = DatasetMetadataManager.SaveMetadata(dataToSave, fullPath);

        if (success)
        {
            Debug.Log("Data saved!");
        }
        else
        {
            Debug.LogError("Failed to save data.");
        }
        
    }
    
    void LoadMetadata()
    {
        // 1. Define the path to load from
        string fileName = metaDataFileName;
        string folderPath = bakedDatasetPath;
        string fullPath = Path.Combine(folderPath, fileName);
        
        Debug.Log($"Loading Dataset from Path: {fullPath}");

        // 2. Call the static LoadMetadata function
        DatasetMetadata loadedData = DatasetMetadataManager.LoadMetadata(fullPath);

        // 3. Use the loaded data
        Debug.Log($"Loaded Cluster Count: {loadedData.clusterCount}");
        Debug.Log($"Loaded Normalization Factor: {loadedData.normalizationFactor}");
        Debug.Log($"Loaded Dataset Name: {loadedData.datasetName}");

        ClusterCount = loadedData.clusterCount;
        normalizationFactor = loadedData.normalizationFactor;
        DatasetName = loadedData.datasetName;

        LayerManager.Instance.loadedLayerIndices = loadedData.indices;
        LayerManager.Instance.loadedLayerTypes = loadedData.layerTypes;
        LayerManager.Instance.loadedLayerNames = loadedData.layerNames;


        // Access any other fields you added...
    }

    void ImportImageLayer(string filepath, string layerName)
    {
        //  Spawn datalayer prefab and set parent to clusterParent
        var bakedLayerObj = GameObject.Instantiate(bakeLayerPrefab, Vector3.zero, Quaternion.identity);
        bakedLayerObj.name = $"Loaded_Image";
        bakedLayerObj.transform.SetParent(bakedClustersParent.transform);
            
        //var layerTex = CreateRenderTexture();
        //bakedLayerObj.GetComponent<BakedDataLayer>().LayerRT = layerTex;
        Texture2D loadedT2D = TextureLoader.LoadTexture2DFromFile(filepath);
        bakedLayerObj.GetComponent<MeshRenderer>().material.SetTexture("_BaseMap", loadedT2D);
        RenderTexture loadedRT = TextureLoader.LoadRenderTextureFromFile(filepath, RenderTextureFormat.Default);
            
        LayerManager.Instance.BuildLayer(bakedLayerObj, layerName, loadedRT, true, typeOverride:1);
        LayerEvents.UpdateLayerPositions.Invoke();
    }

    void SaveLayerTextures()
    {
        foreach (Layer l in LayerManager.Instance.Layers)
        {
            string fileName = $"{bakedClusterImageFileName}{l.Index}.png";
            string fullPath = Path.Combine(bakedDatasetPath, fileName);
            
            Debug.Log($"Attempting to save RenderTexture to: {fullPath}");
            
            bool success = RenderTextureSaver.SaveRenderTextureToFile(l.LayerRenderTexture, fullPath, RenderTextureSaver.ImageFormat.PNG);

            if (success)
            {
                Debug.Log("Save successful!");
            }
            else
            {
                Debug.LogError("Save failed. Check console for errors from RenderTextureSaver.");
            }
            
        }
    }

    public void LoadBakedDataset(GameObject clusterParent)
    {
        DatasetPanel.SetActive(false);
        
        LoadMetadata();

        // Load Clusters
        for (int i = 0; i < ClusterCount; i++)
        {
            //  Spawn datalayer prefab and set parent to clusterParent
            var bakedLayerObj = GameObject.Instantiate(bakeLayerPrefab, Vector3.zero, Quaternion.identity);
            bakedLayerObj.name = $"Loaded_Cluster_{i+1}";
            bakedLayerObj.transform.SetParent(clusterParent.transform);
            
            //var layerTex = CreateRenderTexture();
            //bakedLayerObj.GetComponent<BakedDataLayer>().LayerRT = layerTex;
            string filename = $"cluster_{i+1}.png";
            string fullFilepath = Path.Combine(bakedDatasetPath, filename);
            Texture2D loadedT2D = TextureLoader.LoadTexture2DFromFile(fullFilepath);
            bakedLayerObj.GetComponent<MeshRenderer>().material.SetTexture("_BaseMap", loadedT2D);
            RenderTexture loadedRT = TextureLoader.LoadRenderTextureFromFile(fullFilepath, RenderTextureFormat.Default);
            
            LayerManager.Instance.BuildLayer(bakedLayerObj, "Cluster", loadedRT, true);
            LayerEvents.UpdateLayerPositions.Invoke();
            
        }
        
        
    }

    
    
    private IEnumerator WaitAndCapAgain()
    {
        Debug.Log("Waiting for camera capture");
        yield return new WaitForSecondsRealtime(1.0f);
        BakeCluster(curCluster, 1024, bakedClustersParent);
    }
    
    
    public static RenderTexture CreateRenderTexture(int size, FilterMode filterMode = FilterMode.Bilinear, RenderTextureFormat format = RenderTextureFormat.ARGBFloat)
    {
        RenderTexture output = new RenderTexture(size, size, 0, format);
        output.filterMode = filterMode;
        output.enableRandomWrite = true;
        output.Create();
        return output;
    }

    
}
