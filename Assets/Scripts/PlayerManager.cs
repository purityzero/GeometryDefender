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
    public int BestScore;
    public List<RunRecord> RecentRuns = new List<RunRecord>();
    public string LastPlayedAt;
}

public class PlayerManager : MonoSingleton<PlayerManager>
{
    private const string SAVE_KEY = "PlayerData";
    private const string OPTION_SAVE_KEY = "OptionData";
    private const string ASSET_SAVE_KEY = "AssetData";
    private const int MAX_RECENT_RUN_COUNT = 10;

    private PlayerData m_PlayerData = new PlayerData();
    private OptionData m_OptionData = new OptionData();
    private AssetData m_AssetData = new AssetData();
    private ObservableVariable<int> m_ShardsObservable = new ObservableVariable<int>(0);

    public PlayerData playerData => m_PlayerData;
    public OptionData optionData => m_OptionData;

    protected override void Awake()
    {
        base.Awake();
        Load();
    }

    public void Load()
    {
        m_PlayerData = LoadData<PlayerData>(SAVE_KEY);
        m_OptionData = LoadData<OptionData>(OPTION_SAVE_KEY);
        m_AssetData = LoadData<AssetData>(ASSET_SAVE_KEY);

        m_ShardsObservable.Value = m_AssetData.Shards;
    }

    private T LoadData<T>(string _saveKey) where T : new()
    {
        if (PlayerPrefs.HasKey(_saveKey) == false)
            return new T();

        string json = PlayerPrefs.GetString(_saveKey);
        T loadedData = JsonUtility.FromJson<T>(json);
        if (loadedData == null)
        {
            Debug.LogError($"[PlayerManager] LoadData Failed! json parse error - {_saveKey} / {json}");
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
                Debug.LogError($"[PlayerManager] GetCurrencyAmount Failed! unknown type - {_currencyType}");
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
                Debug.LogError($"[PlayerManager] GetCurrencyObservable Failed! unknown type - {_currencyType}");
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
                Debug.LogError($"[PlayerManager] SpendCurrency Failed! unknown type - {_currencyType}");
                return false;
        }
    }

    public void AddRunRecord(RunRecord _runRecord)
    {
        _runRecord.PlayedAt = DateTime.Now.ToString("o");

        m_PlayerData.RecentRuns.Insert(0, _runRecord);
        if (m_PlayerData.RecentRuns.Count > MAX_RECENT_RUN_COUNT)
            m_PlayerData.RecentRuns.RemoveRange(MAX_RECENT_RUN_COUNT, m_PlayerData.RecentRuns.Count - MAX_RECENT_RUN_COUNT);

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

    private void OnApplicationPause(bool _isPaused)
    {
        if (_isPaused == true)
            Save();
    }
}
