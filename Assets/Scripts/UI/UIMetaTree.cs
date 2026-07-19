using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class UIMetaTree : UIBase
{
    [SerializeField] private ToggleButtonList m_BranchTabs;
    [SerializeField] private RectTransform m_Content;
    [SerializeField] private GameObject m_BranchHeaderTemplate;
    [SerializeField] private MetaTreeNodeItem m_NodeTemplate;

    private TextMeshProUGUI m_HeaderText;
    private List<GameObject> m_SpawnedItems = new List<GameObject>();
    private eMetaBranch m_CurrentBranch = eMetaBranch.StartingPower;

    public override void Show()
    {
        base.Show();

        SetupBranchTabs();
    }

    private void SetupBranchTabs()
    {
        m_BranchTabs.SetData(OnClickBranchTab);

        ToggleMenuTable toggleMenuTable = TableManager.instance.GetTable<ToggleMenuTable>();
        List<ToggleMenuRecord> menuRecords = toggleMenuTable.FindAllByToggleListId(m_BranchTabs.toggleListId);

        for (int i = 0; i < menuRecords.Count; ++i)
        {
            UIToggleButton tabToggle = m_BranchTabs.GetToggle<UIToggleButton>(i);
            if (tabToggle == null)
                continue;

            tabToggle.textOn.color = GetBranchColor((eMetaBranch)i);
        }
    }

    private ToggleMenuRecord GetBranchRecord(eMetaBranch _branch)
    {
        ToggleMenuTable toggleMenuTable = TableManager.instance.GetTable<ToggleMenuTable>();
        List<ToggleMenuRecord> menuRecords = toggleMenuTable.FindAllByToggleListId(m_BranchTabs.toggleListId);

        int index = (int)_branch;
        if (index < 0 || index >= menuRecords.Count)
        {
            Logger.Error($"[UIMetaTree] GetBranchRecord Failed! index out of range - {_branch}");
            return null;
        }

        return menuRecords[index];
    }

    private string GetBranchLabel(eMetaBranch _branch)
    {
        ToggleMenuRecord record = GetBranchRecord(_branch);
        return (record != null) ? record.OnText : _branch.ToString();
    }

    private Color GetBranchColor(eMetaBranch _branch)
    {
        ToggleMenuRecord record = GetBranchRecord(_branch);
        if (record == null)
            return Color.white;

        return ColorUtil.GetColorHtml(record.OnColor);
    }

    private void OnClickBranchTab(int _index)
    {
        RefreshNodeList((eMetaBranch)_index);
    }

    private void RefreshNodeList(eMetaBranch _branch)
    {
        m_CurrentBranch = _branch;

        for (int i = 0; i < m_SpawnedItems.Count; ++i)
        {
            Destroy(m_SpawnedItems[i]);
        }
        m_SpawnedItems.Clear();

        MetaTreeTable metaTreeTable = TableManager.instance.GetTable<MetaTreeTable>();
        if (metaTreeTable.branchMap.ContainsKey(_branch) == false)
        {
            Logger.Error($"[UIMetaTree] RefreshNodeList Failed! branch not found - {_branch}");
            return;
        }

        if (m_HeaderText == null)
        {
            m_BranchHeaderTemplate.SetActive(true);
            GameObject header = ResUtil.Create(m_BranchHeaderTemplate, m_Content);
            m_HeaderText = header.GetComponent<TextMeshProUGUI>();
            m_BranchHeaderTemplate.SetActive(false);
        }

        m_HeaderText.SetText(GetBranchLabel(_branch));
        m_HeaderText.color = GetBranchColor(_branch);

        m_NodeTemplate.gameObject.SetActive(true);
        List<MetaTreeRecord> records = metaTreeTable.branchMap[_branch];
        for (int i = 0; i < records.Count; ++i)
        {
            SpawnNode(metaTreeTable, records[i]);
        }
        m_NodeTemplate.gameObject.SetActive(false);
    }

    private void SpawnNode(MetaTreeTable _metaTreeTable, MetaTreeRecord _record)
    {
        MetaTreeNodeItem nodeItem = ResUtil.Create(m_NodeTemplate, m_Content);
        m_SpawnedItems.Add(nodeItem.gameObject);

        nodeItem.SetData(_record.DisplayName, _record.Cost, GetBranchColor(m_CurrentBranch));

        bool isUnlockable = _metaTreeTable.IsUnlockable(_record.Id, PlayerManager.instance.playerData.UnlockedMetaNodes);
        int nodeId = _record.Id;

        bool showUnlockImage = isUnlockable == false;
        nodeItem.SetData(showUnlockImage, (button) => OnClickNode(nodeId));

        if (showUnlockImage == true)
            nodeItem.SetLock(true);
    }

    private void OnClickNode(int _nodeId)
    {
        MetaTreeTable metaTreeTable = TableManager.instance.GetTable<MetaTreeTable>();
        StringTable stringTable = TableManager.instance.GetTable<StringTable>();

        MetaTreeRecord record = metaTreeTable.GetRecordById(_nodeId);
        if (record == null)
        {
            Logger.Error($"[UIMetaTree] OnClickNode Failed! record not found - {_nodeId}");
            return;
        }

        bool isUnlockable = metaTreeTable.IsUnlockable(_nodeId, PlayerManager.instance.playerData.UnlockedMetaNodes);
        if (isUnlockable == false)
        {
            Logger.Log($"[UIMetaTree] OnClickNode Failed! not unlockable - {_nodeId}");
            UIManager.instance.ShowToast(stringTable.GetString("ToastNotUnlockable"));
            RefreshNodeList(m_CurrentBranch);
            return;
        }

        bool isSpent = PlayerManager.instance.SpendCurrency(eCurrencyType.Shard, record.Cost);
        if (isSpent == false)
        {
            Logger.Log($"[UIMetaTree] OnClickNode Failed! not enough shard - {_nodeId}");
            UIManager.instance.ShowToast(stringTable.GetString("ToastNotEnoughShard"));
            RefreshNodeList(m_CurrentBranch);
            return;
        }

        PlayerManager.instance.UnlockMetaNode(_nodeId);
        RefreshNodeList(m_CurrentBranch);
    }
}
