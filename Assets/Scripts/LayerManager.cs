using System;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using Random = UnityEngine.Random;

public class LayerManager : MonoBehaviour
{
    public static LayerManager Instance;
    public List<Layer> Layers;

    
    public Transform UILayerParent;
    public Transform UILayerParent_World;

    
    [SerializeField] private GameObject UILayerRepresentationPrefab;
    
    public List<int> loadedLayerIndices;
    public List<int> loadedLayerTypes;
    public List<string> loadedLayerNames;
    
    
    
    void Start()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Debug.LogError("Two LayerManagers! There should only ever be one");
            Destroy(this);
        }
        
        if (GameManager.Instance.GameMode != GameManager.Mode.Flat)
        {
            UILayerParent = UILayerParent_World;
        }
        
    }

    private void OnEnable()
    {
        LayerEvents.MoveLayer.AddListener(MoveLayerPos);
    }

    private void OnDisable()
    {
        LayerEvents.MoveLayer.RemoveListener(MoveLayerPos);
    }

    public void SetDefaultFilenames(string postfix)
    {
        foreach (Layer l in Layers)
        {
            l.AssociatedFileName = $"{l.Name}{postfix}";
        }
    }

    public void Swap<T>(IList<T> list, int indexA, int indexB)
    {
        (list[indexA], list[indexB]) = (list[indexB], list[indexA]);
    }

    public void MoveLayerPos(Layer layerToMove, bool up)
    {
        int curIndex = layerToMove.LayerGameObject.transform.GetSiblingIndex();
        int dir = up ? curIndex - 1 : curIndex + 1;
        layerToMove.LayerGameObject.transform.SetSiblingIndex(dir);
        layerToMove.LayerUIGameObject.transform.SetSiblingIndex(dir);
        
        UpdateLayerIndices();

        LayerEvents.UpdateLayerPositions.Invoke();
    }

    public void UpdateLayerIndices()
    {
        foreach (Layer l in Layers)
        {
            l.Index = l.LayerGameObject.transform.GetSiblingIndex();
        }
    }
    

    // Builds layer representation of all child objects of an object
    public void BuildDatasetLayers(GameObject spawnParent, string layerName, bool randomCol = false)
    {
        int count = spawnParent.transform.childCount;
        Transform parentTransform = spawnParent.transform;
        for (int i = 0; i < count; i++)
        {
            GameObject layerOb = parentTransform.GetChild(i).gameObject;
            Layer addLayer = new Layer(index:i, name:$"{layerName} {i}", layerGameObject:layerOb);
            
            if (randomCol)
            {
                addLayer.Color = new Color(
                    Random.Range(0, 255f) / 255f, 
                    Random.Range(0, 255f) / 255f,
                    Random.Range(0, 255f) / 255f);
            }
            
            var uiObjTmp = SpawnUIRepresentation(UILayerRepresentationPrefab, UILayerParent);
            addLayer.LayerUIScript = uiObjTmp.GetComponent<LayerUI>();
            addLayer.LayerUIGameObject = uiObjTmp;
            
            addLayer.LayerUIScript.SetLayer(layerOb.transform, $"{layerName} {i}", addLayer);
            Layers.Add(addLayer);
        }
        Debug.Log($"Built {layerName} Layers");
    }

    // TODO: Break into two sep funcs (BuildNewLayer, BuildLoadedLayer)
    public void BuildLayer(GameObject layerOb, string layerName, RenderTexture layerRT, bool loading, bool randomCol = false, int typeOverride=0)
    {
        int index = 0;
        string lName = "";
        int layerType = 0;
        
        if(!loading)
        {
            index = Layers.Count;
            lName = $"{layerName} {index}";
        }
        else
        {
            
            if (typeOverride == 0)
            {
                int tmp = Layers.Count;
                index = loadedLayerIndices[tmp];
                lName = loadedLayerNames[tmp];
                layerType = loadedLayerTypes[tmp];
            }
            else
            {
                int tmp = Layers.Count;
                index = tmp;
                lName = layerName;
                layerType = 1;
            }
        }

        if (typeOverride == 1) layerType = 1;
        
        Layer addLayer = new Layer(index:index, name:lName, layerGameObject:layerOb);
            
        if (randomCol)
        {
            addLayer.Color = new Color(
                Random.Range(0, 255f) / 255f, 
                Random.Range(0, 255f) / 255f,
                Random.Range(0, 255f) / 255f);
        }
            
        var uiObjTmp = SpawnUIRepresentation(UILayerRepresentationPrefab, UILayerParent);
        addLayer.LayerUIScript = uiObjTmp.GetComponent<LayerUI>();
        addLayer.LayerUIGameObject = uiObjTmp;
        addLayer.LayerUIScript.SetLayer(layerOb.transform, lName, addLayer);
        addLayer.LayerRenderTexture = layerRT;
        addLayer.LayerType = (Layer.DataType)layerType;
        Layers.Add(addLayer);
    }

    public void BuildLoadedLayer(GameObject layerOb, Layer layer, Texture2D texture2D, RenderTexture renderTexture)
    {
        var uiObjTmp = SpawnUIRepresentation(UILayerRepresentationPrefab, UILayerParent);
        layer.LayerUIScript = uiObjTmp.GetComponent<LayerUI>();
        layer.LayerUIGameObject = uiObjTmp;
        layer.LayerUIScript.SetLayer(layerOb.transform, layer.Name, layer);
        layer.LayerRenderTexture = renderTexture;
        layer.LayerTexture2D = texture2D;
    }

    // Expects a layout group to manage pos
    private GameObject SpawnUIRepresentation(GameObject uiObj, Transform uiParent)
    {
        var spawned = GameObject.Instantiate(uiObj, Vector3.zero, Quaternion.identity);
        spawned.transform.SetParent(uiParent);
        spawned.transform.localPosition = Vector3.zero;
        spawned.transform.localScale = Vector3.one;
        return spawned;
    }

    public void HideAllLayers()
    {
        LayerEvents.ToggleAllLayers.Invoke(false);
    }

    public void ShowAllLayers()
    {
        LayerEvents.ToggleAllLayers.Invoke(true);
    }
    
}

[System.Serializable]
public class Layer
{
    public Layer(int index, GameObject layerGameObject, string name = "New Layer", GameObject layerUIGameObject=null)
    {
        Index = index;
        Name = name;
        Color = Color.green;
        LayerGameObject = layerGameObject;
        
        //  Not yet implemented, but will allow for different data types for layers for different data representation
        LayerType = DataType.Cluster;
    }
    
    public Layer(int index, GameObject layerGameObject, Color color, string name = "New Layer", GameObject layerUIGameObject=null)
    {
        Index = index;
        Name = name;
        Color = color;
        LayerGameObject = layerGameObject;
        
        //  Not yet implemented, but will allow for different data types for layers for different data representation
        LayerType = DataType.Cluster;
    }

    public Layer()
    {
        // Empty constructor
    }

    public enum DataType
    {
        Cluster,
        Image
    }
    
    
    public int Index;
    public string Name;
    public Color Color;
    public GameObject LayerGameObject;
    public GameObject LayerUIGameObject;
    public LayerUI LayerUIScript;
    public RenderTexture LayerRenderTexture;
    public Texture2D LayerTexture2D;
    public DataType LayerType;
    public string AssociatedFileName;
}


