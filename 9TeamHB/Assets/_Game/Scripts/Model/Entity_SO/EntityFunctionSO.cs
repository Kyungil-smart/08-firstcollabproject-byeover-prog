using MyGame2.Stage;
using UnityEngine;

// 엔티티의 기능을 모듈화 하기 위한 SO
public abstract class EntityFunctionSO : ScriptableObject
{
    // [Tooltip("SO에셋과 같은 이름으로 작성해주세요")][SerializeField] private string _name;
    // public string Name { get; private set; } ---이름을 쓸 이유가 아직은 없어서 주석 처리
    public abstract IComponentData CreateComponent(EntityState owner);
}
