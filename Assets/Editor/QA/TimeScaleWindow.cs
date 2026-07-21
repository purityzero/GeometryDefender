using UnityEditor;
using UnityEngine;

public class TimeScaleWindow : EditorWindow
{
    private const float MIN_TIME_SCALE = 1f;
    private const float MAX_TIME_SCALE = 5f;

    [MenuItem("Tools/QA/Time Scale")]
    public static void Open()
    {
        GetWindow<TimeScaleWindow>("Time Scale");
    }

    private void OnGUI()
    {
        if (EditorApplication.isPlaying == false)
        {
            EditorGUILayout.HelpBox("Play Mode에서만 동작합니다.", MessageType.Info);
            return;
        }

        EditorGUILayout.LabelField($"현재 배속: {Time.timeScale:0.0}x");

        float newTimeScale = EditorGUILayout.Slider(Time.timeScale, MIN_TIME_SCALE, MAX_TIME_SCALE);
        if (newTimeScale != Time.timeScale)
            Time.timeScale = newTimeScale;

        EditorGUILayout.Space();

        EditorGUILayout.BeginHorizontal();
        for (int speed = 1; speed <= 5; ++speed)
        {
            if (GUILayout.Button($"{speed}x") == true)
                Time.timeScale = speed;
        }
        EditorGUILayout.EndHorizontal();
    }

    private void Update()
    {
        Repaint();
    }
}
