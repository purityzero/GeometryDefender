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

        int killCount = (InGameScene.Current.monsterManager != null) ? InGameScene.Current.monsterManager.killCount.Value : 0;
        int bossKillCount = (InGameScene.Current.monsterManager != null) ? InGameScene.Current.monsterManager.bossKillCount.Value : 0;
        float survivalSeconds = (InGameScene.Current.timerManager != null) ? InGameScene.Current.timerManager.elapsedTime : 0f;
        int cardsObtained = (InGameScene.Current.cardManager != null) ? InGameScene.Current.cardManager.obtainedCardCount : 0;

        // Score는 아직 별도 집계 시스템이 없어 킬 수를 임시 지표로 사용
        // — 카드 시스템이 실제로 생기면 그 값으로 교체할 것 (BossKills/Shards 정산은 2026-07-22에 반영 완료, CardsObtained는 2026-07-24에 반영)
        RunRecord runRecord = new RunRecord
        {
            Score = killCount,
            KillCount = killCount,
            BossKills = bossKillCount,
            SurvivalSeconds = survivalSeconds,
            CardsObtained = cardsObtained,
        };

        PlayerManager.instance.AddRunRecord(runRecord);

        // Assets/Design/05_meta.html "Shards 정산 공식" × Assets/Design/08_balance.html 난이도 배율
        int baseShards = Mathf.FloorToInt(runRecord.SurvivalSeconds / 10f)
            + (runRecord.KillCount / 50)
            + (runRecord.BossKills * 10);

        float difficultyShardMultiplier = (InGameScene.Current.difficultyManager != null) ? InGameScene.Current.difficultyManager.GetShardMultiplier() : 1f;

        // 05_meta.html "ECONOMY" 줄기 — M-303 Shard Bonus 등 해금분을 추가로 곱함
        MetaTreeTable metaTreeTable = TableManager.instance.GetTable<MetaTreeTable>();
        int shardBonusPercent = (metaTreeTable != null) ? metaTreeTable.GetTotalEffectValue(eMetaEffectType.ShardPercent, PlayerManager.instance.playerData.UnlockedMetaNodes) : 0;
        float metaShardMultiplier = 1f + (shardBonusPercent / 100f);

        int shardsEarned = Mathf.RoundToInt(baseShards * difficultyShardMultiplier * metaShardMultiplier);

        PlayerManager.instance.AddCurrency(eCurrencyType.Shard, shardsEarned);

        StringTable stringTable = TableManager.instance.GetTable<StringTable>();

        m_ScoreText.text = runRecord.Score.ToString();
        m_BestText.text = stringTable.GetString("RunOverBest", PlayerManager.instance.playerData.BestScore);

        int totalSeconds = (int)runRecord.SurvivalSeconds;
        int minutes = totalSeconds / 60;
        int seconds = totalSeconds % 60;
        m_StatsValueText.text = $"{minutes:00}:{seconds:00}\n{runRecord.KillCount}\n{runRecord.BossKills}\n{runRecord.CardsObtained}";

        m_ShardsEarnedText.text = $"+{shardsEarned}";
        m_ShardsTotalText.text = stringTable.GetString("RunOverTotal", PlayerManager.instance.GetCurrencyAmount(eCurrencyType.Shard));
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
