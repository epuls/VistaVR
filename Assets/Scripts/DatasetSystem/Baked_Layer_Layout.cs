using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Baked_Layer_Layout : MonoBehaviour
{

    public Vector3 offset = Vector3.zero;
    public Vector3 startPoint = new Vector3(0, 0.5f, 0);
    public float dataScale = 1.0f;
    public Slider OffsetYSlider;
    public Slider OffsetYSlider_ImmersiveVR;
    public Slider ScaleSlider;
    public Slider ScaleSlider_ImmersiveVR;

    private Vector3 defaultPos;
    private Vector3 defaultRot;

    private void Start()
    {
        UpdateChildPositions(offset, startPoint);
        UpdateScale(dataScale);
        defaultPos = transform.localPosition;
        defaultRot = transform.localRotation.eulerAngles;

        
    }

    private void OnEnable()
    {
        LayerEvents.UpdateLayerPositions.AddListener(UpdateChildPositionsE);
        LayerEvents.ResetView.AddListener(ResetToDefaultPos);
        LayerEvents.TopView.AddListener(SetTopView);
    }

    private void OnDisable()
    {
        LayerEvents.UpdateLayerPositions.RemoveListener(UpdateChildPositionsE);
        LayerEvents.ResetView.RemoveListener(ResetToDefaultPos);
        LayerEvents.TopView.RemoveListener(SetTopView);
    }

    public void UpdateChildPositions(Vector3 offset, Vector3 startPoint)
    {
        int childCount = transform.childCount;

        for (int i = 0; i < childCount; i++)
        {
            var curChild = transform.GetChild(i).transform;
            curChild.localPosition = startPoint + (offset * i);
            curChild.localRotation = Quaternion.Euler(Vector3.zero);
        }
    }
    
    public void UpdateChildPositionsE()
    {
        int childCount = transform.childCount;

        for (int i = 0; i < childCount; i++)
        {
            var curChild = transform.GetChild(i).transform;
            curChild.localPosition = startPoint + (offset * i);
        }
    }

    public void UpdateScale(float scale)
    {
        transform.localScale = new Vector3(dataScale, dataScale, dataScale);
    }


    public void ResetToDefaultPos()
    {
        transform.localPosition = defaultPos;
        transform.localRotation = Quaternion.Euler(defaultRot);
    }

    public void SetTopView()
    {
        transform.localPosition = new Vector3(0, 1.5f, 0);
        transform.localRotation = Quaternion.Euler(new Vector3(-90, 0, 0));
    }

    public void SetOffsetY()
    {
        if (GameManager.Instance.GameMode != GameManager.Mode.Flat)
        {
            offset.y = OffsetYSlider_ImmersiveVR.value;
            //dataScale = ScaleSlider_ImmersiveVR.value;
        }
        else
        {
            offset.y = OffsetYSlider.value;
        }
        
        
        UpdateChildPositions(offset, startPoint);
        //UpdateScale(dataScale);
    }

    public void SetScale()
    {
        if (GameManager.Instance.GameMode != GameManager.Mode.Flat)
        {
            //offset.y = OffsetYSlider_ImmersiveVR.value;
            dataScale = ScaleSlider_ImmersiveVR.value;
        }
        else
        {
            dataScale = ScaleSlider.value;
        }
        
        //UpdateChildPositions(offset, startPoint);
        UpdateScale(dataScale);
    }
    
}
