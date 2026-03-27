using System;
using System.Collections.Generic;
using MyGame2.Stage;
using UnityEngine;


[CreateAssetMenu(fileName = "EntitySO", menuName = "Scriptable Objects/EntitySO")]
public class EntitySO : ScriptableObject
{
    // ---설정---
    [Header("설정")]
    [Tooltip("엔티티 종류")][SerializeField] private EntityKind _kind;
    [Tooltip("이 엔티티가 셀을 점유하는가? (이동 차단)")]
    public bool isBlocking = true;

    [Tooltip("이 엔티티가 카메라 시야를 차단하는가?")]
    public bool blocksCameraSight = false;

    [Tooltip("접촉 시 플레이어를 죽이는가?")]
    public bool isLethal = false;
    
    [SerializeField] private GridEntityView _viewPrefab;
    
    
    
    
    
    // 프로퍼티
    public EntityKind Kind { get { return _kind; } }
    public GridEntityView Prefab { get { return _viewPrefab; } }


    // 기능을 SO화 해서 리스트로 보유 (기능 단위로 조합 가능)
    [SerializeField]public List<EntityFunctionSO> Functions;



    /// 엔티티가 해당 기능이 있는지 확인하고 반환하는 함수
    /// 속도에 문제가 생긴다면 검색기반의 딕셔너리나 해시 구조 필요
    public T GetFunction<T>() where T : EntityFunctionSO
    {
        foreach (var func in Functions)
        {
            if (func is T typedFunc)
            {
                return typedFunc;
            }
        }
        return null;
    }
}
