using UnityEngine;
using MyGame2.Stage;
using Unity.Cinemachine;

public class CameraManager : MonoBehaviour
{
    [SerializeField] private StageManager stageManager;
    [SerializeField] private CinemachineCamera vcamP1;
    [SerializeField] private CinemachineCamera vcamP2;
    [SerializeField] private BoxCollider2D boundaryCollider;


    void OnEnable()
    {
        stageManager.Events.StageLoaded += SetFollowTargets;
        stageManager.Events.StageLoaded += SetBoundary;
        stageManager.Events.ActivePlayerChanged += SwitchToPlayer;
    }

    void OnDisable()
    {
        stageManager.Events.StageLoaded -= SetFollowTargets;
        stageManager.Events.StageLoaded -= SetBoundary;
        stageManager.Events.ActivePlayerChanged -= SwitchToPlayer;
    }

    public void SwitchToPlayer(int playerId)
    {
        //Id로 Slot찾기
        StageState state = stageManager.CurrentState;
        state.TryGetEntity(playerId, out EntityState player);
        int playerNumber = player.Get<PlayerData>().Slot;
        
        // 활성 플래이어를 입력 받으면 카메라 우선 순위 변경
        vcamP1.Priority = (playerNumber == 1) ? 20 : 10;
        vcamP2.Priority = (playerNumber == 2) ? 20 : 10;
    }
    private void SetFollowTargets(int stageindex)
    {
        // Id 찾기
        StageState state = stageManager.CurrentState;
        int p1Id = state.GetPlayerIdBySlot(1);
        int p2Id = state.GetPlayerIdBySlot(2);
        
        // 타겟 할당
        var request = new ViewRequest
        {
            Id = p1Id,
            Callback = v => SetFollowTargets(vcamP1, v)
        };
        state.Events.RaiseViewRequest(request);
        request.Id = p2Id;
        request.Callback = v => SetFollowTargets(vcamP2, v);
        state.Events.RaiseViewRequest(request);
        
        // 카메라 우선도 - 최초 p1
        vcamP1.Priority = 20;
        vcamP2.Priority = 10;
    }

    private void SetFollowTargets(CinemachineCamera vcam, GridEntityView view)
    {
        vcam.Target.TrackingTarget = view.transform;
        vcam.ForceCameraPosition( view.transform.position, vcam.transform.rotation );
    }

    private void SetBoundary(int stageindex)
    {
        // 콜라이더 크기 변경
        float width = stageManager.CurrentState.Width + 2f; 
        if(width < 17f) width = 17f;
        float height = stageManager.CurrentState.Height + 2f ;
        if(height < 10f) height = 10f;
        boundaryCollider.size = new Vector2(width, height);
        
        // 콜라이더 오프셋
        float offsetX = width * 0.5f;
        float offsetY = -height * 0.5f;
        boundaryCollider.offset = new Vector2(offsetX, offsetY);
        boundaryCollider.transform.position = new Vector2(-1.5f, 1.5f);
        
        // 컨파이너 캐시 업데이트
        var confiner = vcamP1.GetComponent<CinemachineConfiner2D>();
        confiner.InvalidateBoundingShapeCache();
        confiner = vcamP2.GetComponent<CinemachineConfiner2D>();
        confiner.InvalidateBoundingShapeCache();
    }
}
