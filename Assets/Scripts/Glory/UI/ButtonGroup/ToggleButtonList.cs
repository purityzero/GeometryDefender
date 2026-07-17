using System.Collections.Generic;
using UnityEngine;

public class ToggleButtonList : MonoBehaviour
{
    [SerializeField] UIToggleButton m_ToggleButtonPrefab;
    [SerializeField] Transform m_ButtonParent;
    [Header("# Mutil Select가 가능한 토글 그룹인지 여부 ")]
    [SerializeField] bool m_isRadioMode = false; // 멀티 셀렉트 여부
    [Header("# On 상태인 버튼이 하나뿐일 때 Off 방지 여부")]
    [SerializeField] bool m_isKeepOneSelected = false;

    [SerializeField] private string m_ToggleListId;
    public string toggleListId => m_ToggleListId;

    private List<UIToggleButton> m_ToggleButtons = new List<UIToggleButton>();
    private UnityEngine.Events.UnityAction<int> m_OnClickCB;
    private bool m_isInitialized = false;

    /// <summary>
    /// ToggleListId로 테이블에서 데이터를 찾아 버튼을 자동으로 생성하는 SetData 함수
    /// </summary>
    /// <param name="_onClickCB">버튼 클릭 시 호출될 콜백</param>
    public void SetData(UnityEngine.Events.UnityAction<int> _onClickCB)
    {
        if (string.IsNullOrEmpty(m_ToggleListId) == false)
            SetData(m_ToggleListId, _onClickCB);
    }

    /// <summary>
    /// ToggleListId로 테이블에서 데이터를 찾아 버튼을 자동으로 생성하는 SetData 함수
    /// </summary>
    /// <param name="_toggleListId">ToggleListTable ID</param>
    /// <param name="_onClickCB">버튼 클릭 시 호출될 콜백</param>
    /// <param name="_defaultIndex">기본으로 선택될 버튼의 인덱스</param>
    public void SetData(string _toggleListId, UnityEngine.Events.UnityAction<int> _onClickCB, int _defaultIndex = 0)
    {
        m_ToggleListId = _toggleListId;

        ToggleListTable toggleListTable = TableManager.instance.GetTable<ToggleListTable>();
        ToggleListRecord listRecord = toggleListTable.GetRecordByToggleListId(_toggleListId);
        if (listRecord == null)
        {
            Logger.Error($"[ToggleButtonList] SetData Failed! ToggleListTable Not Found! ToggleListId: {_toggleListId}");
            return;
        }

        if (m_isInitialized == false && string.IsNullOrEmpty(listRecord.PrefabPath) == false)
        {
            if (m_ToggleButtonPrefab == null)
                m_ToggleButtonPrefab = ResUtil.Load<UIToggleButton>(listRecord.PrefabPath);
        }

        ToggleMenuTable toggleMenuTable = TableManager.instance.GetTable<ToggleMenuTable>();
        List<ToggleMenuRecord> menuRecords = toggleMenuTable.FindAllByToggleListId(_toggleListId);
        SetData(menuRecords, _onClickCB, _defaultIndex);
    }

    /// <summary>
    /// 테이블 레코드 목록으로 버튼을 생성하는 SetData 함수
    /// </summary>
    /// <param name="_menuRecords">생성할 버튼들의 데이터 목록</param>
    /// <param name="_onClickCB">버튼 클릭 시 호출될 콜백</param>
    /// <param name="_defaultIndex">기본으로 선택될 버튼의 인덱스</param>
    public void SetData(List<ToggleMenuRecord> _menuRecords, UnityEngine.Events.UnityAction<int> _onClickCB, int _defaultIndex = 0)
    {
        m_OnClickCB = _onClickCB;

        if (m_isInitialized == false)
        {
            m_isInitialized = true;

            m_ToggleButtonPrefab.gameObject.SetActive(true);

            for (int i = 0; i < _menuRecords.Count; ++i)
            {
                UIToggleButton toggleButton = Instantiate(m_ToggleButtonPrefab, m_ButtonParent);
                int index = i;
                ToggleMenuRecord menuRecord = _menuRecords[i];

                Sprite onSprite = (string.IsNullOrEmpty(menuRecord.OnImagePath) == false) ? ResUtil.Load<Sprite>(menuRecord.OnImagePath) : null;
                Sprite offSprite = (string.IsNullOrEmpty(menuRecord.OffImagePath) == false) ? ResUtil.Load<Sprite>(menuRecord.OffImagePath) : null;

                toggleButton.SetData(onSprite, offSprite, menuRecord.OnText, menuRecord.OffText, (button) => OnClickToggle(index));
                m_ToggleButtons.Add(toggleButton);
            }

            m_ToggleButtonPrefab.gameObject.SetActive(false);
        }

        SelectIndex(_defaultIndex);
    }

    /// <summary>
    /// 직접 버튼 개수와 콜백을 설정하는 SetData 함수
    /// </summary>
    /// <param name="_count">생성할 버튼의 개수</param>
    /// <param name="_onClickCB">버튼 클릭 시 호출될 콜백</param>
    /// <param name="_defaultIndex">기본으로 선택될 버튼의 인덱스</param>
    public void SetData(int _count, UnityEngine.Events.UnityAction<int> _onClickCB, int _defaultIndex = 0)
    {
        m_OnClickCB = _onClickCB;

        if (m_isInitialized == false)
        {
            m_isInitialized = true;

            m_ToggleButtonPrefab.gameObject.SetActive(true);

            for (int i = 0; i < _count; ++i)
            {
                UIToggleButton toggleButton = Instantiate(m_ToggleButtonPrefab, m_ButtonParent);
                int index = i;
                toggleButton.SetData(false, (button) => OnClickToggle(index));
                m_ToggleButtons.Add(toggleButton);
            }

            m_ToggleButtonPrefab.gameObject.SetActive(false);
        }

        SelectIndex(_defaultIndex);
    }

    /// <summary>
    /// 프리팹 경로로 버튼을 생성하고, 버튼 개수와 콜백을 설정하는 SetData 함수
    /// </summary>
    /// <param name="_prefabPath">버튼 프리팹 경로</param>
    /// <param name="_count">생성할 버튼의 개수</param>
    /// <param name="_onClickCB">버튼 클릭 시 호출될 콜백</param>
    /// <param name="_defaultIndex">기본으로 선택될 버튼의 인덱스</param>
    public void SetData(string _prefabPath, int _count, UnityEngine.Events.UnityAction<int> _onClickCB, int _defaultIndex = 0)
    {
        if (m_isInitialized == false)
            m_ToggleButtonPrefab = ResUtil.Load<UIToggleButton>(_prefabPath);

        SetData(_count, _onClickCB, _defaultIndex);
    }

    /// <summary>
    /// 지정 인덱스 버튼을 선택 상태로 설정하고 콜백을 호출한다.
    /// </summary>
    public void SelectIndex(int _index)
    {
        if (_index < 0 || _index >= m_ToggleButtons.Count)
            return;

        if (m_isRadioMode == true)
        {
            for (int i = 0; i < m_ToggleButtons.Count; ++i)
            {
                m_ToggleButtons[i].SetToggle(i == _index);
            }
        }
        else
        {
            m_ToggleButtons[_index].SetToggle(true);
        }

        m_OnClickCB?.Invoke(_index);
    }

    public T GetToggle<T>(int _index) where T : UIToggleButton
    {
        if (_index < 0 || _index >= m_ToggleButtons.Count)
        {
            Logger.Error($"[ToggleButtonList] GetToggle Failed! Index Out of Range! - {_index}");
            return null;
        }

        if (m_ToggleButtons[_index] is T == false)
        {
            Logger.Error($"[ToggleButtonList] GetToggle Failed! Type Cast Failed! - {_index}");
            return null;
        }

        return m_ToggleButtons[_index] as T;
    }

    public void Clear()
    {
        m_isInitialized = false;

        for (int i = 0; i < m_ToggleButtons.Count; ++i)
        {
            if (m_ToggleButtons[i] == null)
                continue;

            Destroy(m_ToggleButtons[i].gameObject);
        }

        m_ToggleButtons.Clear();
    }

    private void OnClickToggle(int _index)
    {
        if (m_isRadioMode == true)
        {
            for (int i = 0; i < m_ToggleButtons.Count; ++i)
            {
                if (i != _index)
                    m_ToggleButtons[i].SetToggle(false);
            }
        }

        if (m_isKeepOneSelected == true && m_ToggleButtons[_index].isOn == false)
        {
            bool anyOn = false;
            for (int i = 0; i < m_ToggleButtons.Count; ++i)
            {
                if (m_ToggleButtons[i].isOn == true)
                {
                    anyOn = true;
                    break;
                }
            }

            if (anyOn == false)
            {
                m_ToggleButtons[_index].SetToggle(true);
                return;
            }
        }

        m_OnClickCB?.Invoke(_index);
    }
}
