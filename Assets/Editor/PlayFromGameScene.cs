using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class PlayFromGameScene
{
    static PlayFromGameScene()
    {
        var scene = AssetDatabase.LoadAssetAtPath<SceneAsset>("Assets/Scenes/Game.unity");
        if (scene != null)
            EditorSceneManager.playModeStartScene = scene;
    }

    [MenuItem("Neon Mask/Open Game Scene")]
    static void OpenScene()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/Game.unity");
    }
}
