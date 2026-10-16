using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;


[InitializeOnLoad]
public static class PlayFromMainMenu
{
    const string MenuPath = "Tools/Play From Main Menu";
    const string ScenePath = "Assets/Scenes/MainMenu.unity";
    const string PrefKey = "PlayFromMainMenu";

    static PlayFromMainMenu()
    {
        EditorApplication.delayCall += Apply;
    }

    static bool Enabled
    {
        get => EditorPrefs.GetBool(PrefKey, true);
        set => EditorPrefs.SetBool(PrefKey, value);
    }

    [MenuItem(MenuPath)]
    static void Toggle()
    {
        Enabled = !Enabled;
        Apply();
    }

    [MenuItem(MenuPath, true)]
    static bool ToggleValidate()
    {
        Menu.SetChecked(MenuPath, Enabled);
        return true;
    }

    static void Apply()
    {
        SceneAsset menu = AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath);
        EditorSceneManager.playModeStartScene = Enabled ? menu : null;

        if (Enabled && menu == null)
            Debug.LogWarning($"PlayFromMainMenu: couldn't find {ScenePath}");
    }
}