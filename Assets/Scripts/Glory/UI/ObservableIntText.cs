using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

public abstract class ObservableIntText<TSource> : MonoBehaviour, IUpdatable where TSource : SceneSingleton<TSource>
{
    // 기존 파생 클래스(TowerHealthText/KillCountText)가 각자 직접 갖고 있던 직렬화 필드명을 그대로 이어받아
    // 이 공용 필드로 옮김 — 씬에 이미 저장된 TMP 참조가 리네임으로 끊기지 않도록 함
    [FormerlySerializedAs("m_HpText")]
    [FormerlySerializedAs("m_KillCountText")]
    [SerializeField] private TextMeshProUGUI m_ValueText;

    private ObservableVariable<int> m_RegisteredObservable;

    private void Start()
    {
        BaseScene.Current.Register(this);
    }

    private void OnDestroy()
    {
        BaseScene.Current?.Unregister(this);

        if (m_RegisteredObservable != null)
            m_RegisteredObservable.UnregisterObserver(OnValueChanged);
    }

    public void UpdateLogic()
    {
        if (m_RegisteredObservable != null)
            return;

        TSource source = GetSource();
        if (source == null)
            return;

        m_RegisteredObservable = GetObservable(source);
        m_RegisteredObservable.RegisterObserver(OnValueChanged);
    }

    private void OnValueChanged(int _oldValue, int _newValue)
    {
        m_ValueText.text = Format(_newValue);
    }

    // TSource.Current(static)는 제네릭 타입 매개변수로 직접 접근 불가(C# 제약) — 구체 타입을 아는 파생 클래스가 대신 반환
    protected abstract TSource GetSource();
    protected abstract ObservableVariable<int> GetObservable(TSource _source);
    protected abstract string Format(int _value);
}
