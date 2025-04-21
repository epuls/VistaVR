using UnityEngine;

public class TransformsButtons : MonoBehaviour
{

    public Camera flatCam;

    
    public void ResetView()
    {
        LayerEvents.ResetView.Invoke();
    }

    public void TopView()
    {
        LayerEvents.TopView.Invoke();
    }

    public void ToggleCamType()
    {
        flatCam.orthographic = !flatCam.orthographic;
    }
}
