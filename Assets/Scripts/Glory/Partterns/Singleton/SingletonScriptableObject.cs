using UnityEngine;
using System.IO;

public abstract class SingletonScriptableObject<T> : ScriptableObject where T : ScriptableObject
{
    private static T m_Instance;

    public static T Instance
    {
        get
        {
            if (m_Instance == null)
            {
                m_Instance = Resources.Load<T>(typeof(T).Name);

                if (m_Instance == null)
                {
                    m_Instance = CreateInstance<T>();

                    #if UNITY_EDITOR
 					string path = "Assets/Resources/ScriptableObject";
                    if (!Directory.Exists(path))
                    {
                        Directory.CreateDirectory(path);
                        UnityEditor.AssetDatabase.Refresh();
                    }

                    string assetPath = $"{path}/{typeof(T).Name}.asset";
                    UnityEditor.AssetDatabase.CreateAsset(m_Instance, assetPath);
                    UnityEditor.AssetDatabase.SaveAssets();
                    UnityEditor.AssetDatabase.Refresh();

                    Debug.Log($"[SingletonScriptableObject] {typeof(T).Name}을 생성했습니다: {assetPath}");
                    #endif
                }
            }
            return m_Instance;
        }
    }
}