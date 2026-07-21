using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class MonsterSpawnTestWindow : EditorWindow
{
    private const int MIN_SPAWN_COUNT = 1;
    private const int MAX_SPAWN_COUNT = 300;

    private int m_SpawnCount = 150;
    private bool m_isIncludeNormal = true;
    private bool m_isIncludeElite = true;
    private bool m_isIncludeBoss = true;

    [MenuItem("Tools/QA/Monster Spawn Test")]
    public static void Open()
    {
        GetWindow<MonsterSpawnTestWindow>("Monster Spawn Test");
    }

    private void OnGUI()
    {
        if (EditorApplication.isPlaying == false)
        {
            EditorGUILayout.HelpBox("Play Mode에서만 동작합니다.", MessageType.Info);
            return;
        }

        MonsterManager monsterManager = FindFirstObjectByType<MonsterManager>();
        int aliveCount = (monsterManager != null) ? monsterManager.GetAliveMonsterCount() : 0;
        EditorGUILayout.LabelField($"현재 스폰된 몬스터: {aliveCount}마리", EditorStyles.boldLabel);

        EditorGUILayout.Space();

        m_SpawnCount = EditorGUILayout.IntSlider("스폰 수", m_SpawnCount, MIN_SPAWN_COUNT, MAX_SPAWN_COUNT);

        EditorGUILayout.LabelField("포함할 Variant");
        EditorGUILayout.BeginHorizontal();
        m_isIncludeNormal = EditorGUILayout.ToggleLeft("Normal", m_isIncludeNormal, GUILayout.Width(80));
        m_isIncludeElite = EditorGUILayout.ToggleLeft("Elite", m_isIncludeElite, GUILayout.Width(80));
        m_isIncludeBoss = EditorGUILayout.ToggleLeft("Boss", m_isIncludeBoss, GUILayout.Width(80));
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();

        if (GUILayout.Button($"몬스터 {m_SpawnCount}마리 랜덤 스폰") == true)
            SpawnRandomMix();
    }

    private void SpawnRandomMix()
    {
        MonsterManager monsterManager = FindFirstObjectByType<MonsterManager>();
        if (monsterManager == null)
        {
            Logger.Error($"[MonsterSpawnTestWindow] SpawnRandomMix Failed! MonsterManager not found in scene");
            return;
        }

        EnemyTable enemyTable = TableManager.instance.GetTable<EnemyTable>();
        if (enemyTable == null)
        {
            Logger.Error($"[MonsterSpawnTestWindow] SpawnRandomMix Failed! EnemyTable not loaded");
            return;
        }

        List<EnemyRecord> candidateList = new List<EnemyRecord>();
        if (m_isIncludeNormal == true)
            candidateList.AddRange(enemyTable.dicVariant[eEnemyVariant.Normal]);
        if (m_isIncludeElite == true)
            candidateList.AddRange(enemyTable.dicVariant[eEnemyVariant.Elite]);
        if (m_isIncludeBoss == true)
            candidateList.AddRange(enemyTable.dicVariant[eEnemyVariant.Boss]);

        if (candidateList.Count <= 0)
        {
            Logger.Error($"[MonsterSpawnTestWindow] SpawnRandomMix Failed! 포함된 Variant가 없습니다 (Normal/Elite/Boss 중 하나 이상 켜주세요)");
            return;
        }

        for (int spawnIndex = 0; spawnIndex < m_SpawnCount; ++spawnIndex)
        {
            EnemyRecord record = candidateList[Random.Range(0, candidateList.Count)];
            monsterManager.Spawn(record);
        }

        Logger.Log($"[MonsterSpawnTestWindow] SpawnRandomMix - {m_SpawnCount}마리 스폰 완료 (후보 {candidateList.Count}종)");
    }

    private void Update()
    {
        Repaint();
    }
}
