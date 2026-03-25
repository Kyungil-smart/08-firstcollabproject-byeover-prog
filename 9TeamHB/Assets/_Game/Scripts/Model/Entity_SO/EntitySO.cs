using System;
using System.Collections.Generic;
using MyGame2.Stage;
using UnityEngine;


[CreateAssetMenu(fileName = "EntitySO", menuName = "Scriptable Objects/EntitySO")]
public class EntitySO : ScriptableObject, ISerializationCallbackReceiver
{
    [SerializeField]
    private EntityKind _kind;
    public EntityKind Kind { get; private set; }


    [SerializeReference] 
    // 기능을 SO화 해서 리스트로 보유 (기능 단위로 조합 가능)
    private List<EntityFunctionSO> _functions;

    private Dictionary<string, EntityFunctionSO> _functionCache = new();



    public void OnBeforeSerialize(){ }

    /// <summary>
    /// 유니티에서 자동 호출, 기능 리스트를 딕셔너리에 캐싱
    /// </summary>
    public void OnAfterDeserialize()
    {
        if (_functionCache == null)
        {
            _functionCache = new Dictionary<string, EntityFunctionSO>();
        }
        _functionCache.Clear();
        foreach (var func in _functions)
        {
            if (func != null && !string.IsNullOrEmpty(func.Name))
            {
                // 일단 이름으로 캐시를 했지만 문자열 기반이라 안전성을 위해 변경 가능
                _functionCache[func.Name] = func;
            }
        }
    }
    /// 엔티티가 해당 기능이 있는지 확인하고 반환하는 함수
    /// null 체크 후 있다면 'is 에셋이름' 으로 다운캐스트하여 사용
    public EntityFunctionSO GetEntityFunc(string name)
    {
        _functionCache.TryGetValue(name, out EntityFunctionSO func);
        return func;
    }
}
