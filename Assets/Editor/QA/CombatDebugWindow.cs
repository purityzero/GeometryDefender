using UnityEditor;
using UnityEngine;

// 사용자 요청("치명타랑 내가 카드 고를 수 있는, 그리고 시간 조절할 수 있는 Tool 하나 만들어줘" → "능력 셋팅 가능하게")
// 카드 적용은 CardManager.ApplyCard()를 그대로 재사용 — 모든 능력(스탯/투사체 변형 등)이 카드 하나로 표현되므로
// 카드 목록에서 즉시 적용하는 것 자체가 "능력 셋팅"이 된다(별도 raw 스탯 에디터를 새로 만들지 않음).
public class CombatDebugWindow : EditorWindow
{
    private const float MIN_TIME_SCALE = 1f;
    private const float MAX_TIME_SCALE = 5f;

    private Vector2 m_CardListScrollPosition;

    [MenuItem("Tools/QA/Combat Debug")]
    public static void Open()
    {
        GetWindow<CombatDebugWindow>("Combat Debug");
    }

    private void OnGUI()
    {
        if (EditorApplication.isPlaying == false)
        {
            EditorGUILayout.HelpBox("Play Mode에서만 동작합니다.", MessageType.Info);
            return;
        }

        if (InGameScene.Current == null || InGameScene.Current.towerController == null || InGameScene.Current.cardManager == null)
        {
            EditorGUILayout.HelpBox("InGameScene 진입 후 사용 가능합니다.", MessageType.Info);
            return;
        }

        DrawTimeScaleSection();
        EditorGUILayout.Space();

        DrawWaveSkipSection();
        EditorGUILayout.Space();

        DrawCritSection();
        EditorGUILayout.Space();

        DrawCardSection();
    }

    private void DrawTimeScaleSection()
    {
        EditorGUILayout.LabelField("시간 배속", EditorStyles.boldLabel);
        EditorGUILayout.LabelField($"현재 배속: {Time.timeScale:0.0}x");

        float newTimeScale = EditorGUILayout.Slider(Time.timeScale, MIN_TIME_SCALE, MAX_TIME_SCALE);
        if (newTimeScale != Time.timeScale)
            Time.timeScale = newTimeScale;

        EditorGUILayout.BeginHorizontal();
        for (int speed = 1; speed <= 5; ++speed)
        {
            if (GUILayout.Button($"{speed}x") == true)
                Time.timeScale = speed;
        }
        EditorGUILayout.EndHorizontal();
    }

    // 배속(TimeScale)이 아니라 경과 시간 자체를 순간 이동시켜 Wave를 건너뜀 — TimerManager.elapsedTime(UI/난이도 클리어 판정)과
    // SpawnManager의 웨이브/스폰 판정용 경과 시간이 서로 다른 필드라 둘 다 같이 더해야 어긋나지 않는다(2026-07-24).
    private void DrawWaveSkipSection()
    {
        TimerManager timerManager = InGameScene.Current.timerManager;
        SpawnManager spawnManager = InGameScene.Current.spawnManager;

        WaveTable waveTable = TableManager.instance.GetTable<WaveTable>();
        if (waveTable == null)
        {
            EditorGUILayout.HelpBox("WaveTable을 불러오지 못했습니다.", MessageType.Warning);
            return;
        }

        int totalSeconds = (int)timerManager.elapsedTime;
        WaveRecord activeWave = waveTable.GetActivePhase(totalSeconds);

        EditorGUILayout.LabelField("재생 시간 (Wave 스킵)", EditorStyles.boldLabel);
        EditorGUILayout.LabelField($"현재 시간: {totalSeconds / 60:00}:{totalSeconds % 60:00} / 현재 Wave: {(activeWave != null ? activeWave.Id.ToString() : "-")}");

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("+10초") == true)
            SkipTime(10f, timerManager, spawnManager);
        if (GUILayout.Button("+30초") == true)
            SkipTime(30f, timerManager, spawnManager);
        if (GUILayout.Button("+60초") == true)
            SkipTime(60f, timerManager, spawnManager);
        EditorGUILayout.EndHorizontal();

        foreach (WaveRecord wave in waveTable.list)
        {
            if (GUILayout.Button($"Wave {wave.Id} ({wave.StartTime}s)로 건너뛰기") == true)
                SkipTime(wave.StartTime - timerManager.elapsedTime, timerManager, spawnManager);
        }
    }

    private void SkipTime(float _seconds, TimerManager _timerManager, SpawnManager _spawnManager)
    {
        _timerManager.AddElapsedTime(_seconds);
        _spawnManager.AddElapsedTime(_seconds);
    }

    private void DrawCritSection()
    {
        TowerController towerController = InGameScene.Current.towerController;

        EditorGUILayout.LabelField("치명타", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("치명타 확률 100% (강제 발동)") == true)
            towerController.AddCardCritChance(100f);
        if (GUILayout.Button("치명타 배율 +1.0") == true)
            towerController.AddCardCritMultiplier(1f);
        EditorGUILayout.EndHorizontal();
    }

    private void DrawCardSection()
    {
        EditorGUILayout.LabelField("카드 즉시 적용 (능력 셋팅)", EditorStyles.boldLabel);

        CardTable cardTable = TableManager.instance.GetTable<CardTable>();
        if (cardTable == null)
        {
            EditorGUILayout.HelpBox("CardTable을 불러오지 못했습니다.", MessageType.Warning);
            return;
        }

        StringTable stringTable = TableManager.instance.GetTable<StringTable>();

        m_CardListScrollPosition = EditorGUILayout.BeginScrollView(m_CardListScrollPosition, GUILayout.Height(320));

        foreach (CardRecord record in cardTable.list)
        {
            EditorGUILayout.BeginHorizontal();

            string cardName = (stringTable != null) ? stringTable.GetString(record.NameKey) : record.NameKey;
            EditorGUILayout.LabelField($"[{record.Rarity}] {cardName} — {record.EffectType} {record.EffectValue}");

            if (GUILayout.Button("적용", GUILayout.Width(60)) == true)
                InGameScene.Current.cardManager.ApplyCard(record);

            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.EndScrollView();
    }

    private void Update()
    {
        Repaint();
    }
}
