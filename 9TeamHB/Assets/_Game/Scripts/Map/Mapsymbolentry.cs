using System;
using UnityEngine;

namespace MyGame2.Stage
{
    // 맵 텍스트의 한 문자가 어떤 지형/엔티티에 대응하는지 정의한다.
    // MapSymbolRegistrySO의 entries 배열에 들어가는 단위 데이터.
    //
    // [사용 예시]
    // symbol='B', cellFlags=None, spawnsEntity=true,
    // entityConfig=BoxSharedConfig, facing=None
    // → 'B' 문자가 있는 위치에 공용 상자를 스폰한다.
    //
    // symbol='T', cellFlags=Trap, spawnsEntity=false
    // → 'T' 문자가 있는 위치를 함정 타일로 설정한다.

    [Serializable]
    public class MapSymbolEntry
    {
        [Header("기호 정보")]
        [Tooltip("맵 텍스트(.txt)에서 사용할 문자 (1글자)")]
        public char symbol;

        [Tooltip("이 기호가 무엇을 의미하는지 설명 (인스펙터 가독성용, 런타임 미사용)")]
        public string description;

        [Header("지형 설정")]
        [Tooltip("이 셀에 적용할 CellFlags (벽, 골, 함정 등). 엔티티 스폰과 동시에 사용 가능")]
        public CellFlags cellFlags = CellFlags.None;

        [Header("엔티티 스폰 설정")]
        [Tooltip("체크하면 이 기호 위치에 엔티티를 스폰한다")]
        public bool spawnsEntity;

        [Tooltip("스폰할 엔티티 설정 SO (spawnsEntity가 true일 때만 사용)")]
        public EntitySO entityConfig;
        
        [Tooltip("스폰 시 초기 방향")]
        public Direction facing = Direction.None;
    }
}