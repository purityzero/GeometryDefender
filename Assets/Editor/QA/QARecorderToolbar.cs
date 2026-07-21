using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

// Unity 메인 툴바(Play/Pause/Step 버튼이 있는 그 줄)에 QA 녹화 버튼을 얹는다.
// UnityEditor.Toolbar의 내부 필드(m_Root)/쿼리명("ToolbarZoneRightAlign")에 리플렉션으로 접근하는
// 비공식 방식이라 Unity 버전이 바뀌면 깨질 수 있다 — 실패해도 예외를 던지지 않고 조용히 포기하도록
// 방어적으로 작성(각 단계 null 체크). 에디터를 열 수 없는 환경에서 작성해 실제 Unity
// 6000.3.7f1에서 필드/쿼리명이 맞는지 검증 못 함 — 최초 로드 시 Console 경고로 실패 여부 확인할 것.
[InitializeOnLoad]
public static class QARecorderToolbar
{
    static QARecorderToolbar()
    {
        EditorApplication.delayCall += Inject;
    }

    private static void Inject()
    {
        System.Type toolbarType = typeof(Editor).Assembly.GetType("UnityEditor.Toolbar");
        if (toolbarType == null)
        {
            Debug.LogWarning($"[QARecorderToolbar] Inject Skipped! UnityEditor.Toolbar 타입을 찾을 수 없습니다 (Unity 버전 차이로 추정).");
            return;
        }

        Object[] toolbars = Resources.FindObjectsOfTypeAll(toolbarType);
        if (toolbars.Length == 0)
        {
            Debug.LogWarning($"[QARecorderToolbar] Inject Skipped! Toolbar 인스턴스를 찾을 수 없습니다.");
            return;
        }

        FieldInfo rootField = toolbarType.GetField("m_Root", BindingFlags.NonPublic | BindingFlags.Instance);
        if (rootField == null)
        {
            Debug.LogWarning($"[QARecorderToolbar] Inject Skipped! Toolbar.m_Root 필드를 찾을 수 없습니다 (Unity 버전 차이로 추정).");
            return;
        }

        VisualElement root = rootField.GetValue(toolbars[0]) as VisualElement;
        VisualElement zone = root?.Q("ToolbarZoneRightAlign");
        if (zone == null)
        {
            Debug.LogWarning($"[QARecorderToolbar] Inject Skipped! ToolbarZoneRightAlign을 찾을 수 없습니다 (Unity 버전 차이로 추정).");
            return;
        }

        VisualElement container = new VisualElement
        {
            style =
            {
                flexDirection = FlexDirection.Row,
                marginLeft = 6,
            },
        };

        Button startButton = new Button(QARecorder.StartRecording) { text = "● QA Rec" };
        Button stopButton = new Button(QARecorder.StopRecording) { text = "■ QA Stop" };

        container.Add(startButton);
        container.Add(stopButton);
        zone.Add(container);
    }
}
