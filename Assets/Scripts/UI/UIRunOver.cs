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
        float survivalSeconds = (TimerManager.Current != null) ? TimerManager.Current.elapsedTime : 0f;

        // Score/BossKills/CardsObtained는 아직 별도 집계 시스템이 없어 킬 수를 임시 지표로 사용/0으로 둠
        // — 카드 시스템·보스 처치 구분·런당 Shard 보상이 실제로 생기면 그 값으로 교체할 것
        RunRecord runRecord = new RunRecord
        {
            Score = killCount,
            KillCount = killCount,
            BossKills = 0,
            SurvivalSeconds = survivalSeconds,
            CardsObtained = 0,
        };

        PlayerManager.instance.AddRunRecord(runRecord);

        int shardsEarned = 0;

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
