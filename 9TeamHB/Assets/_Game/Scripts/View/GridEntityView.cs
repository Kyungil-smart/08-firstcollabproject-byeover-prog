using UnityEngine;

namespace MyGame2.Stage
{
    public class GridEntityView : MonoBehaviour
    {
        [SerializeField] private bool rotateWithFacing = true;
        [SerializeField] private GameObject selectedMarker;

        public int EntityId { get; private set; }
        public EntityKind Kind { get; private set; }

        public void Bind(EntityState entity, float cellSize)
        {
            EntityId = entity.Id;
            Kind = entity.Kind;
            Sync(entity, cellSize);
        }

        public void Sync(EntityState entity, float cellSize)
        {
            transform.position = entity.Position.ToWorld(cellSize);

            if (rotateWithFacing)
            {
                transform.rotation = Quaternion.Euler(0f, 0f, entity.Facing.ToZRotation());
            }

            gameObject.SetActive(entity.IsAlive);
        }

        public void SyncSelection(bool isSelected)
        {
            if (selectedMarker != null)
            {
                selectedMarker.SetActive(isSelected);
            }
        }
    }
}