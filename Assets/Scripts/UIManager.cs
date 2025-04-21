using TMPro;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    [SerializeField] private TMP_InputField ImportImagePathInputField;
    [SerializeField] private TMP_InputField ImportImageNameInputField;

    public void ImportImageLayer()
    {
        LayerEvents.ImportImageLayerEvent.Invoke(ImportImagePathInputField.text, ImportImageNameInputField.text);
    }

    public void ToggleObject(GameObject toggleObject)
    {
        toggleObject.SetActive(!toggleObject.activeSelf);
    }
}
