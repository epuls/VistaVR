using UnityEngine;

public class Dataset : MonoBehaviour
{
    private string datasetName;
    private string folderPath;


    private int layerCount;
    private int normalizationFactor;

    public bool datasetLoaded = false;
    public GameObject DatasetPanel;


    void OnEnable()
    {
        if (!datasetLoaded)
        {
            DatasetPanel.SetActive(true);
        }
    }


    public bool SaveDataset()
    {
        return true;
    }

    public bool LoadDataset()
    {
        return true;
    }

}
