using UnityEngine;

public class BakedDataLayer : MonoBehaviour
{
    public Color Tint;
    public Material Mat;
    public RenderTexture LayerRT;

    void Start()
    {
        Mat = this.GetComponent<MeshRenderer>().material;
        Tint = new Color(Random.Range(0,255)/255f, Random.Range(0,255)/255f, Random.Range(0,255)/255f);

        GetComponent<MeshRenderer>().material.SetColor("_BaseColor", Tint);
    }
}
