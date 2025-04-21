using System;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class LayerUI : MonoBehaviour
{

    [FormerlySerializedAs("Layer")] public Transform layerTransform;
    public Layer Layer;
    private bool active = true;
    public TextMeshProUGUI FilterText;
    private int activeColorVal = 0; // Black text
    private int disabledColorVal = 200; // Grey Text
    private int initialLayerIndex = 0;

    public Toggle LayerToggle;

    private void OnEnable()
    {
        LayerEvents.ToggleAllLayers.AddListener(ToggleLayer);
    }

    private void OnDisable()
    {
        LayerEvents.ToggleAllLayers.RemoveListener(ToggleLayer);
    }
    
    

    public void Toggle()
    {
        active = !active;
        float textColVal = active ? activeColorVal / 255f : disabledColorVal / 255f;
        FilterText.color = new Color(textColVal, textColVal, textColVal);
        layerTransform.gameObject.SetActive(active);
    }


    public void SetLayer(Transform layerT, string filterText, Layer lay)
    {
        layerTransform = layerT;
        FilterText.text = filterText;
        Layer = lay;

    }

    public void SelectLayer()
    {
        LayerEvents.SelectLayer.Invoke(Layer);
    }

    public void ToggleLayer(bool on)
    {
        LayerToggle.isOn = on;
    }
}
