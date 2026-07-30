using UnityEditor;
using UnityEngine;

public class PlayerDataResetWindow : EditorWindow
{
    // PlayerManager의 private SAVE_KEY/OPTION_SAVE_KEY/ASSET_SAVE_KEY와 각각 동일한 값이어야 함
    // — PlayerManager.cs의 저장 키를 바꾸면 여기도 같이 바꿀 것
    private const string PLAYER_DATA_KEY = "PlayerData";
    private const string OPTION_DATA_KEY = "OptionData";
    private const string ASSET_DATA_KEY = "AssetData";

    [MenuItem("Tools/QA/Save Data Reset")]
    public static void Open()
    {
        GetWindow<PlayerDataResetWindow>("Save Data Reset");
    }

    private void OnGUI()
    {
        DrawPlayerDataSection();
        EditorGUILayout.Space();

        DrawOptionDataSection();
        EditorGUILayout.Space();

        DrawAssetDataSection();
        EditorGUILayout.Space();

        EditorGUILayout.Space();
        if (GUILayout.Button("전체 초기화 (PlayerData + OptionData + AssetData)") == true)
            TryResetAll();
    }

    private void DrawPlayerDataSection()
    {
        EditorGUILayout.LabelField("PlayerData (최고 점수/메타 트리/난이도 해금/최근 기록)", EditorStyles.boldLabel);

        if (PlayerPrefs.HasKey(PLAYER_DATA_KEY) == false)
        {
            EditorGUILayout.HelpBox("저장된 PlayerData가 없습니다 (신규 유저 상태).", MessageType.Info);
        }
        else
        {
            PlayerData playerData = JsonUtility.FromJson<PlayerData>(PlayerPrefs.GetString(PLAYER_DATA_KEY));
            if (playerData == null)
            {
                EditorGUILayout.HelpBox("저장된 PlayerData 파싱에 실패했습니다.", MessageType.Warning);
            }
            else
            {
                EditorGUILayout.LabelField($"최고 점수: {playerData.BestScore}");
                EditorGUILayout.LabelField($"해금된 메타 노드: {playerData.UnlockedMetaNodes.Count}개");
                EditorGUILayout.LabelField($"해금된 난이도: {playerData.UnlockedDifficulties.Count}개");
                EditorGUILayout.LabelField($"최근 플레이 기록: {playerData.RecentRuns.Count}개");
                EditorGUILayout.LabelField($"마지막 플레이: {playerData.LastPlayedAt}");
            }
        }

        if (GUILayout.Button("PlayerData 초기화") == true)
            TryResetSaveKey(PLAYER_DATA_KEY, "PlayerData", "최고 점수/메타 트리 해금/난이도 해금/최근 기록");
    }

    private void DrawOptionDataSection()
    {
        EditorGUILayout.LabelField("OptionData (사운드/진동/언어/FPS 설정)", EditorStyles.boldLabel);

        if (PlayerPrefs.HasKey(OPTION_DATA_KEY) == false)
        {
            EditorGUILayout.HelpBox("저장된 OptionData가 없습니다 (신규 유저 상태).", MessageType.Info);
        }
        else
        {
            OptionData optionData = JsonUtility.FromJson<OptionData>(PlayerPrefs.GetString(OPTION_DATA_KEY));
            if (optionData == null)
            {
                EditorGUILayout.HelpBox("저장된 OptionData 파싱에 실패했습니다.", MessageType.Warning);
            }
            else
            {
                EditorGUILayout.LabelField($"BGM: {optionData.BgmVolume} / SFX: {optionData.SfxVolume} / 진동: {optionData.isHapticOn}");
                EditorGUILayout.LabelField($"FPS: {optionData.FpsOption} / 언어: {optionData.Language}");
            }
        }

        if (GUILayout.Button("OptionData 초기화") == true)
            TryResetSaveKey(OPTION_DATA_KEY, "OptionData", "사운드/진동/언어/FPS 설정");
    }

    private void DrawAssetDataSection()
    {
        EditorGUILayout.LabelField("AssetData (샤드 보유량)", EditorStyles.boldLabel);

        if (PlayerPrefs.HasKey(ASSET_DATA_KEY) == false)
        {
            EditorGUILayout.HelpBox("저장된 AssetData가 없습니다 (신규 유저 상태).", MessageType.Info);
        }
        else
        {
            AssetData assetData = JsonUtility.FromJson<AssetData>(PlayerPrefs.GetString(ASSET_DATA_KEY));
            if (assetData == null)
                EditorGUILayout.HelpBox("저장된 AssetData 파싱에 실패했습니다.", MessageType.Warning);
            else
                EditorGUILayout.LabelField($"샤드: {assetData.Shards}");
        }

        if (GUILayout.Button("AssetData 초기화") == true)
            TryResetSaveKey(ASSET_DATA_KEY, "AssetData", "샤드 보유량");
    }

    private void TryResetSaveKey(string _saveKey, string _displayName, string _description)
    {
        bool isConfirmed = EditorUtility.DisplayDialog(
            $"{_displayName} 초기화",
            $"저장된 {_displayName}({_description})를 초기화합니다.\n다른 저장 데이터는 유지됩니다.\n계속하시겠습니까?",
            "초기화",
            "취소");

        if (isConfirmed == false)
            return;

        PlayerPrefs.DeleteKey(_saveKey);
        PlayerPrefs.Save();
        ReloadPlayerManagerIfPlaying();

        Logger.Log($"[PlayerDataResetWindow] TryResetSaveKey - {_displayName} 초기화 완료");
        Repaint();
    }

    private void TryResetAll()
    {
        bool isConfirmed = EditorUtility.DisplayDialog(
            "전체 초기화",
            "PlayerData/OptionData/AssetData를 전부 초기화합니다(완전 신규 유저 상태).\n계속하시겠습니까?",
            "초기화",
            "취소");

        if (isConfirmed == false)
            return;

        PlayerPrefs.DeleteKey(PLAYER_DATA_KEY);
        PlayerPrefs.DeleteKey(OPTION_DATA_KEY);
        PlayerPrefs.DeleteKey(ASSET_DATA_KEY);
        PlayerPrefs.Save();
        ReloadPlayerManagerIfPlaying();

        Logger.Log("[PlayerDataResetWindow] TryResetAll - 전체 초기화 완료");
        Repaint();
    }

    private void ReloadPlayerManagerIfPlaying()
    {
        if (EditorApplication.isPlaying == true && PlayerManager.instance != null)
            PlayerManager.instance.Load();
    }
}
