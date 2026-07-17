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
public class PlayerData
{
    public int Version = 1;
    public int Shards;
    public List<int> UnlockedMetaNodes = new List<int>();
    public int BestScore;
    public List<RunRecord> RecentRuns = new List<RunRecord>();
    public string LastPlayedAt;

    public bool isSoundOn = true;
    public bool isHapticOn = true;
    public bool isLeftHandMode = false;
    public eFpsOption FpsOption = eFpsOption.Fps60;
}

public class PlayerManager : MonoSingleton<PlayerManager>
{
    private const string SAVE_KEY = "PlayerData";
    private const int MAX_RECENT_RUN_COUNT = 10;

    private PlayerData m_PlayerData = new PlayerData();

    public PlayerData playerData => m_PlayerData;

    protected override void Awake()
    {
        base.Awake();
        Load();
    }

    public void Load()
    {
        if (PlayerPrefs.HasKey(SAVE_KEY) == false)
        {
            m_PlayerData = new PlayerData();
            return;
        }

        string json = PlayerPrefs.GetString(SAVE_KEY);
        PlayerData loadedData = JsonUtility.FromJson<PlayerData>(json);
        if (loadedData == null)
        {
            Debug.LogError($"[PlayerManager] Load Failed! json parse error - {json}");
            m_PlayerData = new PlayerData();
            return;
        }

        m_PlayerData = loadedData;
    }

    public void Save()
    {
        m_PlayerData.LastPlayedAt = DateTime.Now.ToString("o");

        string json = JsonUtility.ToJson(m_PlayerData);
        PlayerPrefs.SetString(SAVE_KEY, json);
        PlayerPrefs.Save();
    }

    public long GetCurrencyAmount(eCurrencyType _currencyType)
    {
        switch (_currencyType)
        {
            case eCurrencyType.Shard:
                return m_PlayerData.Shards;
            default:
                Debug.LogError($"[PlayerManager] GetCurrencyAmount Failed! unknown type - {_currencyType}");
                return 0;
        }
    }

    public bool SpendCurrency(eCurrencyType _currencyType, long _amount)
    {
        switch (_currencyType)
        {
            case eCurrencyType.Shard:
                if (m_PlayerData.Shards < _amount)
                    return false;

                m_PlayerData.Shards -= (int)_amount;
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
