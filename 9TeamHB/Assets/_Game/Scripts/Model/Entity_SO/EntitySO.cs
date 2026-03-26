using System;
using System.Collections.Generic;
using MyGame2.Stage;
using UnityEngine;


[CreateAssetMenu(fileName = "EntitySO", menuName = "Scriptable Objects/EntitySO")]
public class EntitySO : ScriptableObject
{
    // ---설정---
    [Header("설정")]
    [SerializeField] private EntityKind _kind;
    [SerializeField] private GridEntityView _viewPrefab;
    // 프로퍼티
    public EntityKind Kind { get; private set; }
    public GridEntityView Prefab { get { return _viewPrefab; } }


    [SerializeReference] 
    // 기능을 SO화 해서 리스트로 보유 (기능 단위로 조합 가능)
    private List<EntityFunctionSO> _functions;
    public List<EntityFunctionSO> Functions { get; private set; }


    /// 엔티티가 해당 기능이 있는지 확인하고 반환하는 함수
    /// 속도에 문제가 생긴다면 검색기반의 딕셔너리나 해시 구조 필요
    public T GetFunction<T>() where T : EntityFunctionSO
    {
        foreach (var func in _functions)
        {
            if (func is T typedFunc)
            {
                return typedFunc;
            }
        }
        return null;
    }
}
