using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
    public Dictionary<string, string> Clusters;
    public Dictionary<string, int> ClusterIDs;
    public Dictionary<int, string> InverseClusterIDs;

    public GameObject spawnParent;
    public GameObject bakedClustersParent;
    public GameObject cellSquare;
    public GameObject bakeLayerPrefab;

    //public CellSpawnManager cellSpawnManager;
    //public ECSDataManipulator dataManipulatorECS;
    
    // Raw Dataset Panel
    public TMP_InputField spatialProjectionPathInputField;
    [FormerlySerializedAs("cellClusteringPathInputField")] public TMP_InputField parquetClusteringColumnInputField;
    public TMP_InputField bakedDatasetPathInputField;
    
    public TMP_InputField loadBakedDatasetPathInputField;
    
    public bool limitCellSpawn = true; 
    public int cellCountToSpawn = 1000;
    public float normalizationFactor = 1000;
    public Vector3 offset = Vector3.zero;

    public Camera BakeCamera;
    public GameObject BakeCameraPrefab;
    public int ClusterToSpawn;
    private int ClusterCount = 0;
    private int curCluster = 0;

    public List<LayerUI> filters;

    public bool AutoBake = false;
    public string cellBarcodeFilePath;
    public string cellClusterColumnNameParquet;
    public string bakedDatasetPath;
    public int resolution = 2048;
    
    
    public bool datasetLoaded = false;
    public GameObject DatasetPanel;

    public string DatasetName = "Test_Dataset";
    private string bakedClusterImageFileName = "cluster_";
    private string metaDataFileName = "dataset_metadata.json";
    
    
    
    void Start()
    {
        if(AutoBake) LoadAndBake(cellBarcodeFilePath, cellClusterColumnNameParquet);
        
    }

    void OnEnable()
    {
        if (!datasetLoaded)
        {
            DatasetPanel.SetActive(true);
        }
        
        spatialProjectionPathInputField.onValueChanged.AddListener(SetSpatialProjPath);
        parquetClusteringColumnInputField.onValueChanged.AddListener(SetClusterColumnName);
        bakedDatasetPathInputField.onValueChanged.AddListener(SetBakePath);
        loadBakedDatasetPathInputField.onValueChanged.AddListener(SetBakePath);
        
        LayerEvents.ImportImageLayerEvent.AddListener(ImportImageLayer);
    }

    private void OnDisable()
    {
        spatialProjectionPathInputField.onValueChanged.RemoveListener(SetSpatialProjPath);
        parquetClusteringColumnInputField.onValueChanged.RemoveListener(SetClusterColumnName);
        bakedDatasetPathInputField.onValueChanged.RemoveListener(SetBakePath);
        loadBakedDatasetPathInputField.onValueChanged.RemoveListener(SetBakePath);
        
        LayerEvents.ImportImageLayerEvent.RemoveListener(ImportImageLayer);
    }

    void SetSpatialProjPath(string value)
    {
        cellBarcodeFilePath = value;
    }

    void SetClusterColumnName(string value)
    {
        cellClusterColumnNameParquet = value;
    }

    void SetBakePath(string value)
    {
        bakedDatasetPath = value;
    }

    public void LoadAndBakeButton()
    {
        LoadAndBake(cellBarcodeFilePath, cellClusterColumnNameParquet);
        DatasetPanel.SetActive(false);
    }

    private bool _cellCoordsReady = false;
    private bool _cellClustersReady = false;
    void LoadAndBake(string cellPath, string clusterPath)
    {
        //Cells = CSVImporter.LoadCsvToDictionary(cellPath); 
        //Clusters = CSVImporter.LoadCsvToClusterDictionary(clusterPath);

        //Cells = ParquetReaderUtility.ReadColumnVector2Dictionary(cellPath, "barcodes", "X", "Y");
        //Clusters = ParquetReaderUtility.ReadColumnDictionary(clusterPath, "barcodes", "UnsupervisedL1");
        
        
        StartCoroutine(ParquetReaderUtility.ReadColumnVector2DictionaryCoroutine(
            cellBarcodeFilePath,
            "barcode",
            "X",
            "Y",
            dict =>
            {
                Cells = dict;
                Debug.Log($"Loaded {Cells.Count} barcoded cells");
                normalizationFactor = FindMaxAbsoluteValue(Cells);
                Debug.Log($"Normalization Factor: {normalizationFactor}");
                _cellCoordsReady = true;
                StartCoroutine(ParquetReaderUtility.ReadColumnDictionaryCoroutine(
                    cellBarcodeFilePath,
                    "barcode",
                    cellClusterColumnNameParquet,
                    clustersDict =>
                    {
                        Clusters = clustersDict;
                        Debug.Log($"Loaded cluster labels for {Clusters.Count} barcoded cells");
                        ClusterIDs = GetClusterIDs(Clusters);
                        InverseClusterIDs = GetInverseClusterIDs(ClusterIDs);
                        ClusterCount = ClusterIDs.Count-1;
                        _cellClustersReady = true;
                        TryBake();
                    },
                    err => {
                        Debug.LogError($"Failed: {err}");
                    }
                ));
            }
        ));
        
        
    }

    void TryBake()
    {
        if(_cellClustersReady && _cellCoordsReady)
        {
            Debug.Log("All data is loaded, baking now.");
            _cellClustersReady = false;
            _cellCoordsReady = false;
            BakeCluster(curCluster, resolution, bakedClustersParent);
            return;
        }
        Debug.Log("Tried to bake but not all data loaded. This is expected and not an error.");
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
    
    public static int FindClusterCount(Dictionary<string, string> vectorDict)
    {
        List<string> tmpClusters = new List<string>();

        foreach (KeyValuePair<string, string> entry in vectorDict)
        {
            // Get the absolute value for both x and y components
            if (!tmpClusters.Contains(entry.Value))
            {
                tmpClusters.Add(entry.Value);
            }
        }

        return tmpClusters.Count;
    }

    public static Dictionary<string,int> GetClusterIDs(Dictionary<string, string> clusterDict)
    {
        int curIdx = 0;
        Dictionary<string, int> outDict = new Dictionary<string, int>();

        foreach (KeyValuePair<string, string> entry in clusterDict)
        {
            //print($"Clusters: {entry.Key} | {entry.Value}");
            // Get the absolute value for both x and y components
            if (!outDict.ContainsKey(entry.Value))
            {
                outDict.Add(entry.Value, curIdx);
                curIdx++;
            }
        }

        if (outDict.ContainsKey(""))
        {
            int tmp = outDict[""];
            outDict.Remove("");
            outDict.Add("NO_DATA",tmp);
        }

        return outDict;
    }

    public static Dictionary<int, string> GetInverseClusterIDs(Dictionary<string, int> clusterIDs)
    {
        Dictionary<int, string> outDict = new Dictionary<int, string>();
        foreach (var kvp in clusterIDs)
        {
            outDict.Add(kvp.Value, kvp.Key);
        }

        return outDict;
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

           /*
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
            */
            
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
            
            string clusterName = Clusters[cell.Key] == "" ? "NO_DATA" : Clusters[cell.Key];
            int clusterID = ClusterIDs[clusterName];

            if (clusterID == id)
            {
                Vector2 coords = cell.Value;
                Vector3 normalizedCoords = new Vector3(coords.x / normalizationFactor, 0, coords.y / normalizationFactor);
                normalizedCoords += offset;
                
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
        LayerManager.Instance.BuildLayer(bakedLayerObj, InverseClusterIDs[curCluster], layerTex, false);

        
        if (curCluster < ClusterCount)
        {
            curCluster += 1;
            LayerEvents.UpdateLayerPositions.Invoke();
            StartCoroutine(WaitAndCapAgain());
        }
        else
        {
            //LayerManager.Instance.BuildDatasetLayers(bakedClustersParent, "Cluster");
            Debug.Log("Dataset loaded!");
            //LayerManager.Instance.SetClusterNames(ClusterIDs);
            LayerManager.Instance.SetDefaultFilenames(".png");
            LayerEvents.UpdateLayerPositions.Invoke();
            
            SaveMetadata();
            SaveLayerTextures();
            
        }
    }

    void SaveMetadata()
    {
        
        if (LayerManager.Instance == null || LayerManager.Instance.Layers == null) // Assuming LayerManager has a List<Layer> Layers
        {
            Debug.LogError("LayerManager or its Layers list is missing!");
            return;
        }

        // 1. Create the main container
        DatasetMetadataContainer container = new DatasetMetadataContainer();

        // 2. Populate dataset-level metadata (get these values from your tool/manager)
        container.DatasetName = "JSON_TEST"; 
        container.LayerResolution = resolution; 
        container.NormalizationFactor = normalizationFactor;
        container.ClusterCount = ClusterCount; 

        // 3. Convert runtime Layer objects into LayerData objects
        container.Layers = LayerManager.Instance.Layers 
            .Where(layer => layer != null) 
            .Select(runtimeLayer => new LayerData(runtimeLayer)) 
            .ToList();

        // 4. Ensure layers are sorted by index 
        container.Layers = container.Layers.OrderBy(l => l.Index).ToList();
        
        string fileName = metaDataFileName;
        string saveFilePath = Path.Combine(bakedDatasetPath, fileName);


        
        bool success = DatasetMetadataManager_Json.SaveMetadata(container, saveFilePath);

        if (success)
        {
            Debug.Log("Dataset successfully saved via JSON.");
        }
        else
        {
            Debug.LogError("Failed to save dataset via JSON.");
        }
        
    }
    
    void LoadMetadata(GameObject clusterParent)
    {
        string fileName = metaDataFileName;
        string folderPath = bakedDatasetPath;
        string loadFilePath = Path.Combine(folderPath, fileName);
        /*
        // 1. Define the path to load from
        
        
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
        */
        
        DatasetMetadataContainer loadedData = DatasetMetadataManager_Json.LoadMetadata(loadFilePath);

        if (loadedData == null)
        {
            Debug.LogError($"Failed to load dataset from {loadFilePath}. Check previous errors.");
            // Handle error appropriately (e.g., show message to user, load defaults)
            return;
        }

        ClusterCount = loadedData.ClusterCount;
        normalizationFactor = loadedData.NormalizationFactor;
        DatasetName = loadedData.DatasetName;



        // 4. Create runtime Layer objects from the loaded LayerData
        if (loadedData.Layers != null)
        {
            foreach (LayerData data in loadedData.Layers) // Assumes Layers are already sorted by index from LoadMetadata
            {

                Layer newLayer = data.ToLayerObject();
                
                //  Spawn datalayer prefab and set parent to clusterParent
                var bakedLayerObj = GameObject.Instantiate(bakeLayerPrefab, Vector3.zero, Quaternion.identity);
                bakedLayerObj.name = $"{newLayer.Name}";
                bakedLayerObj.transform.SetParent(clusterParent.transform);
                
            

                string filename = newLayer.AssociatedFileName;
                Debug.Log($"Associated file: {filename}");
                string fullFilepath = Path.Combine(bakedDatasetPath, filename);
                Texture2D loadedT2D = TextureLoader.LoadTexture2DFromFile(fullFilepath);
                bakedLayerObj.GetComponent<MeshRenderer>().material.SetTexture("_BaseMap", loadedT2D);
                RenderTexture loadedRT = TextureLoader.LoadRenderTextureFromFile(fullFilepath, RenderTextureFormat.Default);
            
                //LayerManager.Instance.BuildLayer(bakedLayerObj, newLayer.Name, loadedRT, true);
                LayerManager.Instance.BuildLoadedLayer(bakedLayerObj, newLayer, loadedT2D, loadedRT);
                


                LayerManager.Instance.Layers.Add(newLayer); // Add the fully configured layer
                LayerEvents.UpdateLayerPositions.Invoke();
            }
        }

        Debug.Log($"Dataset successfully loaded from {loadFilePath}. Applied {loadedData.Layers?.Count ?? 0} layers.");

        LayerEvents.UpdateLayerPositions.Invoke();
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
            
            string fullPath = Path.Combine(bakedDatasetPath, l.AssociatedFileName);
            
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
        
        LoadMetadata(clusterParent);

        /*
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
        */
        
        
    }

    
    
    private IEnumerator WaitAndCapAgain()
    {
        Debug.Log("Waiting for camera capture");
        yield return new WaitForSecondsRealtime(1.0f);
        BakeCluster(curCluster, resolution, bakedClustersParent);
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
