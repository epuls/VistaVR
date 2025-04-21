using UnityEngine;
using UnityEngine.Events;

public class LayerEvents : MonoBehaviour
{
    // Create UI panels for data types, and when layer is selected evoke and handle Data Panel toggling
    public static UnityEvent<Layer> SelectLayer = new UnityEvent<Layer>();
    public static UnityEvent<bool> ToggleAllLayers = new UnityEvent<bool>();
    public static UnityEvent<Layer, bool> MoveLayer = new UnityEvent<Layer, bool>();
    

    public static UnityEvent UpdateLayerPositions = new UnityEvent();
    public static UnityEvent ResetView = new UnityEvent();
    public static UnityEvent TopView = new UnityEvent();
    
    
    // Convention is filepath, layername
    public static UnityEvent<string, string> ImportImageLayerEvent = new UnityEvent<string, string>();
}
