using System;

namespace MyGame2.Stage
{
    // 맵 파싱 시 생성되는 엔티티 스폰 정보.
    [Serializable]
    public readonly struct SpawnData
    {
        public readonly EntitySO Def;
        public readonly GridPos Position;
        public readonly Direction Facing;
        public readonly int PairGroup;

        public SpawnData(
            EntitySO def, GridPos position, Direction facing,
            int pairGroup = 0)
        {
            Def = def;
            Position = position;
            Facing = facing;
            PairGroup = pairGroup;
        }
    }
}