using UnityEngine;

// 매 프레임 로직이 필요한 Actor(ActorPlayer 등)를 위해 IUpdatable을 지원한다.
// 등록/해제는 OnEnable/OnDisable이 아니라 Open()/Close()에서 한다 — CullingObject처럼 자기 자신을
// SetActive(false)하는 컴포넌트가 같은 오브젝트에 붙어도(ActorMonster) 그 SetActive 토글이
// Open()/Close()를 재호출하지 않으므로, "자기 비활성화가 곧 영구 등록 해제"로 이어지는 문제가 없다.
public class Actor : FactoryObject, IUpdatable
{
    public override void Open()
    {
        base.Open();
        BaseScene.Current?.Register(this);
    }

    public override void Close()
    {
        base.Close();
        BaseScene.Current?.Unregister(this);
    }

    public virtual void UpdateLogic() { }
}
