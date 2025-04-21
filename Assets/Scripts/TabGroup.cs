using UnityEngine;

public class TabGroup : MonoBehaviour
{
    [SerializeField] private GameObject activeTabObject;
    [SerializeField] private GameObject defaultTabObject;

    public void SelectTab(GameObject selectedTab)
    {
        activeTabObject.SetActive(false);
        selectedTab.SetActive(true);
        activeTabObject = selectedTab;
    }
}
