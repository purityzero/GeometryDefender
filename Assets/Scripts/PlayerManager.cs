using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class RunRecord
{
    public int Score;
    public int KillCount;
    public int BossKills;
    public float SurvivalSeconds;
    public int CardsObtained;
    public string PlayedAt;
}

[Serializable]
public class OptionData
{
    public bool isSoundOn = true;
    public bool isHapticOn = true;
    public bool isLeftHandMode = false;
    public eFpsOption FpsOption = eFpsOption.Fps60;
    public eLanguage Language;
    public bool isEnemyDamageTextOn = true;
    public bool isAllyDamageTextOn = true;
}

[Serializable]
public class AssetData
{
    public int Shards;
}

[Serializable]
public class PlayerData
{
    public int Version = 1;
    public List<int> UnlockedMetaNodes = new List<int>();
    public List<eDifficultyLevel> UnlockedDifficulties = new List<eDifficultyLevel> { eDifficultyLevel.Normal };
    public int BestScore;
    public List<RunRecord> RecentRuns = new List<RunRecord>();
    public string LastPlayedAt;
}

public class PlayerManager : MonoSingleton<PlayerManager>
{
    private const string SAVE_KEY = "PlayerData";
    private const string OPTION_SAVE_KEY = "OptionData";
    private const string ASSET_SAVE_KEY = "AssetData";

    private PlayerData m_PlayerData = new PlayerData();
    private OptionData m_OptionData = new OptionData();
    private AssetData m_AssetData = new AssetData();
    private ObservableVariable<int> m_ShardsObservable = new ObservableVariable<int>(0);
    private ObservableVariable<eLanguage> m_LanguageObservable = new ObservableVariable<eLanguage>(eLanguage.Korean);

    public PlayerData playerData => m_PlayerData;
    public OptionData optionData => m_OptionData;
    public ObservableVariable<eLanguage> languageObservable => m_LanguageObservable;

    protected override void Awake()
    {
        base.Awake();
        Load();
    }

    public void Load()
    {
        bool isFirstLaunch = PlayerPrefs.HasKey(OPTION_SAVE_KEY) == false;

        m_PlayerData = LoadData<PlayerData>(SAVE_KEY);
        m_OptionData = LoadData<OptionData>(OPTION_SAVE_KEY);
        m_AssetData = LoadData<AssetData>(ASSET_SAVE_KEY);

        if (isFirstLaunch == true)
            m_OptionData.Language = StringTable.GetDefaultLanguage();

        m_ShardsObservable.Value = m_AssetData.Shards;

        StringTable.CurrentLanguage = m_OptionData.Language;
        m_LanguageObservable.Value = m_OptionData.Language;
        ApplyFpsOption();
    }

    private T LoadData<T>(string _saveKey) where T : new()
    {
        if (PlayerPrefs.HasKey(_saveKey) == false)
            return new T();

        string json = PlayerPrefs.GetString(_saveKey);
        T loadedData = JsonUtility.FromJson<T>(json);
        if (loadedData == null)
        {
            Logger.Error($"[PlayerManager] LoadData Failed! json parse error - {_saveKey} / {json}");
            return new T();
        }

        return loadedData;
    }

    public void Save()
    {
        m_PlayerData.LastPlayedAt = DateTime.Now.ToString("o");

        PlayerPrefs.SetString(SAVE_KEY, JsonUtility.ToJson(m_PlayerData));
        PlayerPrefs.SetString(OPTION_SAVE_KEY, JsonUtility.ToJson(m_OptionData));
        PlayerPrefs.SetString(ASSET_SAVE_KEY, JsonUtility.ToJson(m_AssetData));
        PlayerPrefs.Save();
    }

    public long GetCurrencyAmount(eCurrencyType _currencyType)
    {
        switch (_currencyType)
        {
            case eCurrencyType.Shard:
                return m_AssetData.Shards;
            default:
                Logger.Error($"[PlayerManager] GetCurrencyAmount Failed! unknown type - {_currencyType}");
                return 0;
        }
    }

    public ObservableVariable<int> GetCurrencyObservable(eCurrencyType _currencyType)
    {
        switch (_currencyType)
        {
            case eCurrencyType.Shard:
                return m_ShardsObservable;
            default:
                Logger.Error($"[PlayerManager] GetCurrencyObservable Failed! unknown type - {_currencyType}");
                return null;
        }
    }

    public bool SpendCurrency(eCurrencyType _currencyType, long _amount)
    {
        switch (_currencyType)
        {
            case eCurrencyType.Shard:
                if (m_AssetData.Shards < _amount)
                    return false;

                m_AssetData.Shards -= (int)_amount;
                m_ShardsObservable.Value = m_AssetData.Shards;
                Save();
                return true;
            default:
                Logger.Error($"[PlayerManager] SpendCurrency Failed! unknown type - {_currencyType}");
                return false;
        }
    }

    public bool AddCurrency(eCurrencyType _currencyType, long _amount)
    {
        switch (_currencyType)
        {
            case eCurrencyType.Shard:
                m_AssetData.Shards += (int)_amount;
                m_ShardsObservable.Value = m_AssetData.Shards;
                Save();
                return true;
            default:
                Logger.Error($"[PlayerManager] AddCurrency Failed! unknown type - {_currencyType}");
                return false;
        }
    }

    public void AddRunRecord(RunRecord _runRecord)
    {
        _runRecord.PlayedAt = DateTime.Now.ToString("o");

        m_PlayerData.RecentRuns.Insert(0, _runRecord);
        if (m_PlayerData.RecentRuns.Count > GameConfigTable.MAX_RECENT_RUN_COUNT)
            m_PlayerData.RecentRuns.RemoveRange(GameConfigTable.MAX_RECENT_RUN_COUNT, m_PlayerData.RecentRuns.Count - GameConfigTable.MAX_RECENT_RUN_COUNT);

        if (_runRecord.Score > m_PlayerData.BestScore)
            m_PlayerData.BestScore = _runRecord.Score;

        Save();
    }

    public void UnlockMetaNode(int _nodeId)
    {
        if (m_PlayerData.UnlockedMetaNodes.Contains(_nodeId) == true)
            return;

        m_PlayerData.UnlockedMetaNodes.Add(_nodeId);
        Save();
    }

    public void UnlockDifficulty(eDifficultyLevel _difficulty)
    {
        if (m_PlayerData.UnlockedDifficulties.Contains(_difficulty) == true)
            return;

        m_PlayerData.UnlockedDifficulties.Add(_difficulty);
        Save();
    }

    public bool IsDifficultyUnlocked(eDifficultyLevel _difficulty)
    {
        return m_PlayerData.UnlockedDifficulties.Contains(_difficulty);
    }

    public void SetLanguage(eLanguage _language)
    {
        m_OptionData.Language = _language;
        StringTable.CurrentLanguage = _language;
        m_LanguageObservable.Value = _language;
        Save();
    }

    public void SetSoundOn(bool _isSoundOn)
    {
        m_OptionData.isSoundOn = _isSoundOn;
        Save();

        // TODO: 사운드/BGM 시스템이 생기면 여기서 실제 뮤트 처리
    }

    public void SetHapticOn(bool _isHapticOn)
    {
        m_OptionData.isHapticOn = _isHapticOn;
        Save();

        // TODO: 진동 트리거 시스템이 생기면 여기서 실제 On/Off 반영
    }

    public void SetLeftHandMode(bool _isLeftHandMode)
    {
        m_OptionData.isLeftHandMode = _isLeftHandMode;
        Save();

        // TODO: 인게임 일시정지 버튼이 생기면 여기서 좌/우 위치 반영
    }

    public void SetEnemyDamageTextOn(bool _isEnemyDamageTextOn)
    {
        m_OptionData.isEnemyDamageTextOn = _isEnemyDamageTextOn;
        Save();
    }

    public void SetAllyDamageTextOn(bool _isAllyDamageTextOn)
    {
        m_OptionData.isAllyDamageTextOn = _isAllyDamageTextOn;
        Save();
    }

    public void SetFpsOption(eFpsOption _fpsOption)
    {
        m_OptionData.FpsOption = _fpsOption;
        ApplyFpsOption();
        Save();
    }

    private void ApplyFpsOption()
    {
        switch (m_OptionData.FpsOption)
        {
            case eFpsOption.Fps30:
                Application.targetFrameRate = 30;
                break;
            case eFpsOption.Fps60:
                Application.targetFrameRate = 60;
                break;
            case eFpsOption.Adaptive:
            default:
                Application.targetFrameRate = -1;
                break;
        }
    }

    private void OnApplicationPause(bool _isPaused)
    {
        if (_isPaused == true)
            Save();
    }
}
