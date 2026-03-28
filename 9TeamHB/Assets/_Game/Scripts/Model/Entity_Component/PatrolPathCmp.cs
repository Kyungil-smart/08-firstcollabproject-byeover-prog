using MyGame2.Stage;
using UnityEngine;

// 경로 기반 순찰 기능 SO.
// 새 감시자A/B용. 지정된 경로를 따라 이동.
// 기존 PatrolCmp(로봇/동물 자유순찰)과 구분.

[CreateAssetMenu(fileName = "PatrolPath_Cmp", menuName = "Scriptable Objects/EntityCmp/PatrolPathCmp")]
public class PatrolPathCmp : EntityFunctionSO
{
    [Tooltip("루프 경로 시 시계방향 순찰 (우→하→좌→상)")]
    [SerializeField] private bool _loopClockwise = true;

    [Tooltip("비루프 경로 시 시작~끝 왕복 이동")]
    [SerializeField] private bool _canPingPong = true;

    [Tooltip("오브젝트를 무시하고 감시 가능 (벽과 부쉬만 시야 차단)")]
    [SerializeField] private bool _ignoreObjectSight = true;

    [Tooltip("함정/투사체에 영향 없음")]
    [SerializeField] private bool _ignoreTrap = true;

    public override IComponentData CreateComponent(EntityState owner)
    {
        return new PatrolPathData(_loopClockwise, _canPingPong,
            _ignoreObjectSight, _ignoreTrap);
    }
}