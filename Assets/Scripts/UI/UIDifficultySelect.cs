using UnityEngine;

public class UIDifficultySelect : UIPopup
{
    [SerializeField] private UIToggleButton m_NormalItem;
    [SerializeField] private UIToggleButton m_HardItem;
    [SerializeField] private UIToggleButton m_HellItem;
    [SerializeField] private UIToggleButton m_InfiniteItem;

    public override void Show()
    {
        base.Show();

        SetupItem(m_NormalItem, eDifficultyLevel.Normal);
        SetupItem(m_HardItem, eDifficultyLevel.Hard);
        SetupItem(m_HellItem, eDifficultyLevel.Hell);
        SetupItem(m_InfiniteItem, eDifficultyLevel.Infinite);
    }

    private void SetupItem(UIToggleButton _item, eDifficultyLevel _level)
    {
        bool isUnlocked = PlayerManager.instance.IsDifficultyUnlocked(_level);

        _item.SetData(true, (toggle) => OnClickDifficulty(_level));
        _item.SetLock(isUnlocked == false);
    }

    private void OnClickDifficulty(eDifficultyLevel _level)
    {
        if (PlayerManager.instance.IsDifficultyUnlocked(_level) == false)
        {
            StringTable stringTable = TableManager.instance.GetTable<StringTable>();
            UIManager.instance.ShowToast(stringTable.GetString("ToastDifficultyLocked"));
            return;
        }

        DifficultyManager.SelectedDifficulty = _level;
        SceneManager.instance.NextScene(EScene.InGameScene.ToString());
    }
}
