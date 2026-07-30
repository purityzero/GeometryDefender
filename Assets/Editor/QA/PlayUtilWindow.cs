using UnityEditor;
using UnityEngine;

public class PlayUtilWindow : EditorWindow
{
    private static bool isAuto = false;

    [MenuItem("Tools/QA/AutoMode")]
    public static void Open()
    {
        isAuto = PlayerManager.instance.isAutoMode;
        GetWindow<PlayUtilWindow>("Time Scale");
    }

    private void OnGUI()
    {
        if (EditorApplication.isPlaying == false)
        {
            EditorGUILayout.HelpBox("Play Mode에서만 동작합니다.", MessageType.Info);
            return;
        }
        isAuto = EditorGUILayout.Toggle("AutoMode", isAuto);
        if(PlayerManager.instance.isAutoMode != isAuto)
        {
            PlayerManager.instance.isAutoMode = isAuto;
        }
    }
}
