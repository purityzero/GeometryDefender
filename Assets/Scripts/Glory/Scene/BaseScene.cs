using System.Collections.Generic;
using UnityEngine;

public abstract class BaseScene : SceneSingleton<BaseScene>
{
    // Time.timeScale 대신 이 플래그로 일시정지를 표현 — timeScale은 전역 상태라 무관한 코드(예: 다른 팝업의 Close())가
    // 실수로 되돌리기 쉽고, DOTween/Animator 등 엔진 전반에 영향을 준다. 등록된 IUpdatable 전체(Timer/Spawn/Tower 등)의
    // UpdateLogic() 호출 자체를 건너뛰는 방식이 더 안전하다.
    public bool isPaused { get; set; }

    private List<IUpdatable> m_UpdatableList = new List<IUpdatable>();

    // BaseScene 자신은 자기 자신의 갱신 리스트에 등록될 필요가 없어(등록해도 빈 UpdateLogic()이라 해는 없지만 의미 없는
    // 자기 참조) base.OnEnable()의 Register 호출은 생략한다. 다만 Play 중 재컴파일로 static Current가 초기화된 뒤
    // Awake()는 재호출되지 않으므로, Current 갱신 자체는 여기서 해줘야 한다(SceneSingleton 2026-07-27 참고).
    protected override void OnEnable()
    {
        Current = this;
    }

    protected override void OnDisable() { }

    private void Start()
    {
        OnSetup();
    }

    protected virtual void OnSetup() { }

    public void Register(IUpdatable _updatable)
    {
        if (m_UpdatableList.Contains(_updatable) == true)
            return;

        m_UpdatableList.Add(_updatable);
    }

    public void Unregister(IUpdatable _updatable)
    {
        m_UpdatableList.Remove(_updatable);
    }

    // UIManager.Get<T>()의 UITable 참조(2026-07-15, 사용자 요청)와 동일한 성격의 프로젝트 비의존 원칙 예외 —
    // 씬 전역에서 "키 하나로 SFX 재생"이 필요한 곳(UIButton 등)이 각자 SoundTable을 조회하지 않고 여기로 모으기 위해 신설(2026-07-29, 사용자 요청).
    public void PlaySfx(string _soundKey)
    {
        if (string.IsNullOrEmpty(_soundKey) == true)
            return;

        SoundTable soundTable = TableManager.instance.GetTable<SoundTable>();
        SoundRecord record = soundTable?.GetRecordByKey(_soundKey);
        if (record == null)
        {
            Logger.Error($"[BaseScene] PlaySfx Failed! SoundRecord not found - {_soundKey}");
            return;
        }

        AudioClip clip = ResUtil.Load<AudioClip>(record.ClipPath);
        if (clip == null)
            return;

        SoundManager.instance.PlaySfx(clip, null, record.MaxConcurrent);
    }

    private void Update()
    {
        if (isPaused == true)
            return;

        for (int i = 0; i < m_UpdatableList.Count; ++i)
        {
            m_UpdatableList[i].UpdateLogic();
        }
    }
}
