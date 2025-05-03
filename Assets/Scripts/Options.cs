using System.Collections.Generic;
using TMPro;
using Unity.Scenes;
using UnityEngine;
using UnityEngine.UI;

public class Options : MonoBehaviour
{
    public static Options Instance;

    private bool loaded;
    [SerializeField] private List<Material> skyboxMaterials;
    
    [SerializeField] private float ImmersiveRotateDataSens;
    [SerializeField] private float ImmersiveTranslateDataSens;
    
    [SerializeField] private float VRRotateDataSens;
    [SerializeField] private float VRTranslateDataSens;
    
    [SerializeField] private float FlatRotateDataSens;
    [SerializeField] private float FlatTranslateDataSens;
    
    [SerializeField] private Button[] buttons;
    [SerializeField] private TextMeshProUGUI[] texts;
    [SerializeField] private Image[] panels;

    public ColorBlock darkModeButtonColorBlock;
    public ColorBlock darkModeToggleColorBlock = ColorBlock.defaultColorBlock;
    
    public UIColorMode _UIColorMode = UIColorMode.DarkMode;
    
    [SerializeField] int panelShade = 35;
    [SerializeField] float nestedMultiplier = 2f;
    [SerializeField] float curMultiplier = 1;
    
    void Start()
    {
        if (Instance != null)
        {
            Debug.LogError("Two Options instances! Did we load the scene twice?");
            Destroy(this);
        }
        else
        {
            Instance = this;
        }
        
        LoadSettings();

        
        GetUIElements();
        SetUIElementColors(buttons, panels, texts);
    }



    private void GetUIElements()
    {
        buttons = (Button[])GameObject.FindObjectsByType(typeof(Button), FindObjectsInactive.Include, FindObjectsSortMode.InstanceID);
        texts = (TextMeshProUGUI[])GameObject.FindObjectsByType(typeof(TextMeshProUGUI), FindObjectsInactive.Include, FindObjectsSortMode.InstanceID);
        panels = (Image[])GameObject.FindObjectsByType(typeof(Image), FindObjectsInactive.Include, FindObjectsSortMode.InstanceID);
        
    }

    private ColorBlock GetDarkmodeColorBlock()
    {
        
        return darkModeButtonColorBlock;
        
        // Probably just remove below
        ColorBlock cb = new ColorBlock();
        byte baseTint = 125;
        cb.normalColor = new Color32(baseTint, baseTint, baseTint, 255);
        cb.highlightedColor = new Color32(58, 128, 172, 255); 
        cb.pressedColor = new Color32(48, 120, 150, 255);
        cb.disabledColor = new Color32(baseTint, baseTint, baseTint, 100);
        cb.colorMultiplier = 1;
        cb.fadeDuration = 0.5f;

        
        return cb;
    }

    public enum UIColorMode
    {
        LightMode,
        DarkMode
    }

    // Currently just sets UI to dark mode if darkmode is enabled
    public void SetUIElementColors(Button[] buttons, Image[] panels, TextMeshProUGUI[] texts)
    {

        if (_UIColorMode == UIColorMode.LightMode) return;
        ColorBlock buttonCb = GetDarkmodeColorBlock();

        // For dark mode
        Color textCol = Color.white;
        
        foreach (Button b in buttons)
        {
            b.colors = buttonCb;
        }
        

        foreach (TextMeshProUGUI tmp in texts)
        {
            tmp.color = textCol;
        }

        
        

        foreach (Image im in panels)
        {
            if (im.gameObject.TryGetComponent(out Button b))
            {
                // don't change image, this is also a button
            }
            else
            {
                curMultiplier = 1;
                if (im.gameObject.GetComponentInParent(typeof(Image)))
                {
                    Debug.Log("Found nested panel");
                    curMultiplier *= nestedMultiplier;
                    if (im.gameObject.transform.parent.GetComponentInParent(typeof(Image)))
                    {
                        curMultiplier *= nestedMultiplier;
                        Debug.Log("Double nested panel");
                    }
                }

            
                Color32 panelColor = new Color32((byte)(panelShade * curMultiplier), (byte)(panelShade * curMultiplier), (byte)(panelShade * curMultiplier), 255);
                im.color = panelColor;
            }
            
        }
    }
    



    private void SaveSettings()
    {
        
    }

    private void LoadSettings()
    {
        if (loaded) return;
        loaded = true;
    }
    
    public void SetSkybox(int id)
    {
        RenderSettings.skybox = skyboxMaterials[id];
    }

    public Vector2 GetDataManipSensitivity()
    {
        LoadSettings();
        
        Vector2 sens = new Vector2();
        switch (GameManager.Instance.GameMode)
        {
            case(GameManager.Mode.Flat):
                sens.x = FlatRotateDataSens;
                sens.y = FlatTranslateDataSens;
                break;
            case(GameManager.Mode.Immersive):
                sens.x = ImmersiveRotateDataSens;
                sens.y = ImmersiveTranslateDataSens;
                break;
            case(GameManager.Mode.VR):
                sens.x = VRRotateDataSens;
                sens.y = VRTranslateDataSens;
                break;
        }

        return sens;
    }
}
