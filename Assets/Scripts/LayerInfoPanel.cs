using System;
using TMPro;
using UnityEngine;

//TODO: Integrate this into UIManager
public class LayerInfoPanel : MonoBehaviour
{

    [SerializeField] private TextMeshProUGUI LayerNameText;
    [SerializeField] private TextMeshProUGUI LayerTypeText;


    private Layer _selectedLayer;
    
    private void OnEnable()
    {
        LayerEvents.SelectLayer.AddListener(PopulateLayerInfoPanel);
    }

    private void OnDisable()
    {
        LayerEvents.SelectLayer.RemoveListener(PopulateLayerInfoPanel);
    }


    public void MoveLayer(bool up)
    {
        //bool up = dir == 0 ? true : false;
        if(_selectedLayer != null)
            LayerEvents.MoveLayer.Invoke(_selectedLayer, up);
    }


    public void PopulateLayerInfoPanel(Layer selectedLayer)
    {
        _selectedLayer = selectedLayer;
        LayerNameText.text = selectedLayer.Name;
        LayerTypeText.text = $"Layer Type: {selectedLayer.LayerType.ToString()}";
    }
}
