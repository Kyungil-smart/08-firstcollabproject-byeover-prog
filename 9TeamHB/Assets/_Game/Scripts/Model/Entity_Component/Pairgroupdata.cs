using MyGame2.Stage;

// 페어 그룹 연동 컴포넌트.
// 같은 PairGroup 번호를 가진 엔티티끼리 상호작용한다.

public class PairGroupData : IComponentData
{
    public int PairGroup;

    public PairGroupData(int pairGroup)
    {
        PairGroup = pairGroup;
    }
}