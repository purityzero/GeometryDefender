using Unity.Entities;

// 한 프레임에 여러 타워에서 데미지가 들어올 수 있으므로 Buffer 사용
public struct DamageRequest : IBufferElementData
{
    public int Amount;
}
