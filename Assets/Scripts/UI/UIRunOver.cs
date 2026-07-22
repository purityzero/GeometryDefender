using TMPro;
using UnityEngine;

public class UIRunOver : UIPopup
{
    [SerializeField] private TextMeshProUGUI m_ScoreText;
    [SerializeField] private TextMeshProUGUI m_BestText;
    [SerializeField] private TextMeshProUGUI m_StatsValueText;
    [SerializeField] private TextMeshProUGUI m_ShardsEarnedText;
    [SerializeField] private TextMeshProUGUI m_ShardsTotalText;

    public override void Show()
    {
        base.Show();

        int killCount = (MonsterManager.Current != null) ? MonsterManager.Current.killCount.Value : 0;
        int bossKillCount = (MonsterManager.Current != null) ? MonsterManager.Current.bossKillCount.Value : 0;
        float survivalSeconds = (TimerManager.Current != null) ? TimerManager.Current.elapsedTime : 0f;

        // Score/CardsObtained는 아직 별도 집계 시스템이 없어 킬 수를 임시 지표로 사용/0으로 둠
        // — 카드 시스템이 실제로 생기면 그 값으로 교체할 것 (BossKills/Shards 정산은 2026-07-22에 반영 완료)
        RunRecord runRecord = new RunRecord
        {
            Score = killCount,
            KillCount = killCount,
            BossKills = bossKillCount,
            SurvivalSeconds = survivalSeconds,
            CardsObtained = 0,
        };

        PlayerManager.instance.AddRunRecord(runRecord);

        // Assets/Design/05_meta.html "Shards 정산 공식" × Assets/Design/08_balance.html 난이도 배율
        int baseShards = Mathf.FloorToInt(runRecord.SurvivalSeconds / 10f)
            + (runRecord.KillCount / 50)
            + (runRecord.BossKills * 10);

        float difficultyShardMultiplier = (DifficultyManager.Current != null) ? DifficultyManager.Current.GetShardMultiplier() : 1f;

        // 05_meta.html "ECONOMY" 줄기 — M-303 Shard Bonus 등 해금분을 추가로 곱함
        MetaTreeTable metaTreeTable = TableManager.instance.GetTable<MetaTreeTable>();
        int shardBonusPercent = (metaTreeTable != null) ? metaTreeTable.GetTotalEffectValue(eMetaEffectType.ShardPercent, PlayerManager.instance.playerData.UnlockedMetaNodes) : 0;
        float metaShardMultiplier = 1f + (shardBonusPercent / 100f);

        int shardsEarned = Mathf.RoundToInt(baseShards * difficultyShardMultiplier * metaShardMultiplier);

        PlayerManager.instance.AddCurrency(eCurrencyType.Shard, shardsEarned);

        m_ScoreText.text = runRecord.Score.ToString();
        m_BestText.text = $"Best: {PlayerManager.instance.playerData.BestScore}";

        int totalSeconds = (int)runRecord.SurvivalSeconds;
        int minutes = totalSeconds / 60;
        int seconds = totalSeconds % 60;
        m_StatsValueText.text = $"{minutes:00}:{seconds:00}\n{runRecord.KillCount}\n{runRecord.BossKills}\n{runRecord.CardsObtained}";

        m_ShardsEarnedText.text = $"+{shardsEarned}";
        m_ShardsTotalText.text = $"Total: {PlayerManager.instance.GetCurrencyAmount(eCurrencyType.Shard)}";
    }

    public void OnClickMetaTree()
    {
        UIManager.instance.Get<UIMetaTree>();
    }

    public void OnClickRestart()
    {
        Time.timeScale = 1f;
        SceneManager.instance.NextScene(EScene.InGameScene.ToString());
    }

    public void OnClickMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.instance.NextScene(EScene.TitleScene.ToString());
    }
}
