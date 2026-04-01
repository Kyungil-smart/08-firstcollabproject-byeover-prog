using System;
using UnityEngine;

namespace MyGame2.Stage
{
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

        [Header("페어 그룹")]
        [Tooltip("같은 번호끼리 자동 페어링 (0 = 페어 없음)\n" +
                 "레버1↔문1 = 같은 번호, 스위치1↔문5 = 같은 번호")]
        public int pairGroup = 0;
    }
}