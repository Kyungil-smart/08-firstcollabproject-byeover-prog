using System.Collections.Generic;
using UnityEngine;

namespace MyGame2.Stage
{
    // 맵 텍스트(.txt)의 모든 문자 매핑을 관리하는 레지스트리.
    // 새 기믹/타일/적을 추가할 때 코드 수정 없이 인스펙터에서 엔트리만 추가하면 된다.

    [CreateAssetMenu(
        fileName = "MapSymbolRegistry",
        menuName = "Stage/Map Symbol Registry",
        order = 1)]
    public sealed class MapSymbolRegistrySO : ScriptableObject
    {
        [Header("기호 매핑 목록")]
        [Tooltip("맵 텍스트의 각 문자가 어떤 지형/엔티티에 대응하는지 정의한다.\n" +
                 "새 기믹 추가 시 여기에 엔트리를 추가하면 코드 수정이 필요 없다.")]
        [SerializeField]
        private List<MapSymbolEntry> entries = new List<MapSymbolEntry>();

        // 런타임 캐시 (char → entry 빠른 조회)
        private Dictionary<char, MapSymbolEntry> _lookup;

        // 외부에서 전체 엔트리 목록을 읽을 수 있다 (에디터 도구용).
        public IReadOnlyList<MapSymbolEntry> Entries
        {
            get { return entries; }
        }

        // 기호에 대응하는 엔트리를 조회한다.
        // 등록되지 않은 기호이면 false를 반환한다.
        public bool TryGetEntry(char symbol, out MapSymbolEntry entry)
        {
            if (_lookup == null)
            {
                BuildLookup();
            }

            return _lookup.TryGetValue(symbol, out entry);
        }

        private void BuildLookup()
        {
            _lookup = new Dictionary<char, MapSymbolEntry>(
                entries != null ? entries.Count : 0);

            if (entries == null)
            {
                return;
            }

            for (int i = 0; i < entries.Count; i++)
            {
                MapSymbolEntry e = entries[i];
                if (e == null)
                {
                    continue;
                }

                if (_lookup.ContainsKey(e.symbol))
                {
                    Debug.LogWarning(
                        $"[MapSymbolRegistry] 중복 기호 '{e.symbol}' " +
                        $"(인덱스 {i}: {e.description}). 첫 번째 등록만 유지됩니다.",
                        this);
                    continue;
                }

                _lookup[e.symbol] = e;
            }
        }

        // 인스펙터에서 수정 시 캐시를 초기화하여 다음 조회 시 재구축한다.
        private void OnValidate()
        {
            _lookup = null;
        }

        // 등록된 기호 수를 반환한다.
        public int Count
        {
            get
            {
                return entries != null ? entries.Count : 0;
            }
        }
    }
}