using UnityEngine;

namespace MyGame2.Stage
{
    // 톱날 함정 프리팹에 부착하는 비주얼 컴포넌트.
    // GridEntityView.Bind() 후 호출되면
    // SawTrapData.Size만큼 셀 단위로 자식 스프라이트를 자동 생성한다.

    public class SawTrapVisual : MonoBehaviour
    {
        [Header("셀 스프라이트")]
        [Tooltip("톱날 1칸짜리 스프라이트")]
        [SerializeField] private Sprite cellSprite;

        [Header("렌더링")]
        [Tooltip("톱날 sortingOrder")]
        [SerializeField] private int sortingOrder = -1;

        [Tooltip("셀 크기 (보통 1)")]
        [SerializeField] private float cellSize = 1f;

        // GridEntityView.Bind() 이후에 호출
        public void BuildVisual(int size, Direction facing)
        {
            // 기존 자식 정리
            for (int i = transform.childCount - 1; i >= 0; i--)
                Destroy(transform.GetChild(i).gameObject);

            if (cellSprite == null)
            {
                Debug.LogWarning("[SawTrapVisual] cellSprite가 할당되지 않음");
                return;
            }

            // Facing 방향에 따른 오프셋 계산
            Vector3 offset = FacingToOffset(facing);

            for (int i = 0; i < size; i++)
            {
                GameObject cell = new GameObject($"Saw_{i}");
                cell.transform.SetParent(transform, false);
                // 앵커(0,0)에서 facing 방향으로 i칸씩 이동
                cell.transform.localPosition = offset * i;

                SpriteRenderer sr = cell.AddComponent<SpriteRenderer>();
                sr.sprite = cellSprite;
                sr.sortingOrder = sortingOrder;
            }
        }

        private Vector3 FacingToOffset(Direction facing)
        {
            switch (facing)
            {
                case Direction.Right: return new Vector3(cellSize, 0, 0);
                case Direction.Left:  return new Vector3(-cellSize, 0, 0);
                case Direction.Up:    return new Vector3(0, cellSize, 0);
                case Direction.Down:  return new Vector3(0, -cellSize, 0);
                default:              return new Vector3(cellSize, 0, 0);
            }
        }
    }
}