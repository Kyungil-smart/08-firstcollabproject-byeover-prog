using UnityEngine;

namespace MyGame2.Stage
{
    // 톱날 함정 비주얼.
    // 앵커 기준 Facing 수직 방향 좌우 1칸 = 3칸 렌더링.

    public class SawTrapVisual : MonoBehaviour
    {
        [Header("셀 스프라이트")]
        [SerializeField] private Sprite cellSprite;

        [Header("렌더링")]
        [SerializeField] private int sortingOrder = -1;
        [SerializeField] private float cellSize = 1f;

        public void BuildVisual(int size, Direction facing)
        {
            // 기존 자식 정리
            for (int i = transform.childCount - 1; i >= 0; i--)
                Destroy(transform.GetChild(i).gameObject);

            if (cellSprite == null) return;

            // 수직 방향 계산
            Vector3 perpOffset = PerpOffset(facing);

            // 3칸: 왼쪽(-1), 앵커(0), 오른쪽(+1)
            for (int i = -1; i <= 1; i++)
            {
                GameObject cell = new GameObject($"Saw_{i + 1}");
                cell.transform.SetParent(transform, false);
                cell.transform.localPosition = perpOffset * i;

                SpriteRenderer sr = cell.AddComponent<SpriteRenderer>();
                sr.sprite = cellSprite;
                sr.sortingOrder = sortingOrder;
            }
        }

        private Vector3 PerpOffset(Direction facing)
        {
            // 항상 가로 3칸 고정
            return new Vector3(cellSize, 0, 0);
        }
    }
}