using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{

    public static GameManager Instance;
    public Mode GameMode = Mode.Flat;
    
    [SerializeField] private List<GameObject> VRObjects;
    [SerializeField] private List<GameObject> FlatObjects;
    [SerializeField] private List<GameObject> ImmersiveObjects;

    [SerializeField] private MeshRenderer PodiumMeshRenderer;
    
    private List<GameObject> _curModeObjects;
    
#if UNITY_EDITOR
    private Mode _prevMode;
    
#endif

    void Start()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(this);
            Debug.LogError("Two GameManagers! There should only be one");
        }
        
        SetGameMode(initial: true);
        
        
#if UNITY_EDITOR
        _prevMode = GameMode;
#endif
    }



    void Update()
    {
        
        
#if UNITY_EDITOR
        if (_prevMode != GameMode)
        {
            _prevMode = GameMode;
            SetGameMode();
        }
#endif
    }
    
    public void SetGameMode(bool initial=false)
    {
        if (!initial)
        {
            Debug.Log($"Changing GameMode to {GameMode}");
            SetGameModeObjects(_curModeObjects, false);
        }
        else
        {
            Debug.Log($"Setting initial Game Mode as {GameMode}");
            SetGameModeObjects(VRObjects, false);
            SetGameModeObjects(FlatObjects, false);
            SetGameModeObjects(ImmersiveObjects, false);
        }
        
        if (GameMode != Mode.Flat)
        {
            PodiumMeshRenderer.enabled = true;
        }
        else
        {
            PodiumMeshRenderer.enabled = false;
        }
        
        switch (GameMode)
        {
            case(Mode.Flat):
                _curModeObjects = FlatObjects;
                break;
            case(Mode.Immersive):
                _curModeObjects = ImmersiveObjects;
                break;
            case(Mode.VR):
                _curModeObjects = VRObjects;
                break;
        }
        
        SetGameModeObjects(_curModeObjects, true);
        
    }


    private void SetGameModeObjects(List<GameObject> objects, bool active)
    {
        foreach (GameObject obj in objects)
        {
            obj.SetActive(active);
        }
    }
    

    public enum Mode
    {
        VR,
        Immersive,
        Flat
    }
}
