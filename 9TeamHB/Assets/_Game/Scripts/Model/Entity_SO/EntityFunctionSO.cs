using MyGame2.Stage;
using UnityEngine;


[CreateAssetMenu(fileName = "EntityFunctionSO", menuName = "Scriptable Objects/EntityFunctionSO")]

// 엔티티의 기능을 모듈화 하기 위한 SO
public abstract class EntityFunctionSO : ScriptableObject
{
    [Tooltip("SO에셋과 같은 이름으로 작성해주세요")][SerializeField] private string _name;
    public string Name { get; private set; }
    public abstract IComponentData CreateComponent(EntityState owner);
}
