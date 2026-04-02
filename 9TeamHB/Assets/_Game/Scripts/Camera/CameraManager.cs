using System.Collections;
using UnityEngine;
using MyGame2.Stage;
using Unity.Cinemachine;

public class CameraManager : MonoBehaviour
{
    [Header("시네머신 관리")]
    [SerializeField] private StageManager stageManager;
    [SerializeField] private CinemachineCamera vcamStart;
    [SerializeField] private CinemachineCamera vcamP1;
    [SerializeField] private CinemachineCamera vcamP2;
    [SerializeField] private CinemachineCamera vcamEvent;
    [SerializeField] private BoxCollider2D boundaryCollider;
    
    [Header("경고 효과")]
    [SerializeField] private RectTransform warningIcon;
    [SerializeField] private FloatEventChannelSO eventHub;
    
    [Header("이벤트 연출")]
    [SerializeField] private Transform marker;
    [SerializeField] private float runningTime = 1f;
    void OnEnable()
    {
        stageManager.Events.StageLoaded += SetFollowTargets;
        stageManager.Events.StageLoaded += SetBoundary;
        stageManager.Events.ActivePlayerChanged += SwitchToPlayer;
        stageManager.Events.EntityKilled += FocusDeadPlayer;
        stageManager.Events.PairActivated += OnOpenDoorEvent;
        eventHub.OnAlertAndChaseRaised += AlertToPlayer;
    }

    void OnDisable()
    {
        stageManager.Events.StageLoaded -= SetFollowTargets;
        stageManager.Events.StageLoaded -= SetBoundary;
        stageManager.Events.ActivePlayerChanged -= SwitchToPlayer;
        stageManager.Events.EntityKilled -= FocusDeadPlayer;
        stageManager.Events.PairActivated -= OnOpenDoorEvent;
        eventHub.OnAlertAndChaseRaised -= AlertToPlayer;
    }

    private void SwitchToPlayer(int playerId)
    {
        //Id로 Slot찾기
        StageState state = stageManager.CurrentState;
        state.TryGetEntity(playerId, out EntityState player);
        int playerNumber = player.Get<PlayerData>().Slot;
        
        // 활성 플래이어를 입력 받으면 카메라 우선 순위 변경
        vcamP1.Priority = (playerNumber == 1) ? 20 : 10;
        vcamP2.Priority = (playerNumber == 2) ? 20 : 10;
    }

    private void FocusDeadPlayer(int playerId)
    {
        //Id로 Slot찾기
        StageState state = stageManager.CurrentState;
        if(state.TryGetEntity(playerId, out EntityState player))
        {
            if (player.Has<PlayerData>())
            {
                int playerNumber = player.Get<PlayerData>().Slot;
            
                // 활성 플래이어를 입력 받으면 카메라 우선 순위 변경
                vcamP1.Priority = (playerNumber == 1) ? 20 : 10;
                vcamP2.Priority = (playerNumber == 2) ? 20 : 10;
            }
        }
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
        // 처음만 컷 방식 이동
        vcamStart.Priority = 0;
        vcamP1.Priority = 20;
        vcamP2.Priority = 10;
    }

    private void SetFollowTargets(CinemachineCamera vcam, GridEntityView view)
    {
        vcam.Follow = view.transform;
        vcam.ForceCameraPosition( view.transform.position, vcam.transform.rotation );
    }

    private void OnOpenDoorEvent(GridPos cell)
    {
        if(IsPointVisible(cell.ToWorld(1f))) return;
        StartCoroutine(ShowEventCamera(cell));
    }
    private IEnumerator ShowEventCamera(GridPos cell)
    {
        var pos = cell.ToWorld(1f);
        marker.position = pos;
        vcamEvent.Follow = marker;
        vcamEvent.ForceCameraPosition(pos, vcamEvent.transform.rotation);
        vcamEvent.Priority = 100;
        yield return null;
        // 게임 일시정지 이벤트
        yield return new WaitForSecondsRealtime(0.1f);
        Time.timeScale = 0f;
        yield return new WaitForSecondsRealtime(runningTime);
        // 게임 시작
        Time.timeScale = 1f;
        vcamEvent.Priority = 0;
    }

    private void SetBoundary(int stageindex)
    {
        // 콜라이더 크기 변경
        float height = stageManager.CurrentState.Height + 2f ;
        float width = stageManager.CurrentState.Width + 2f; 
        boundaryCollider.size = new Vector2(width, height);
        
        // 콜라이더 오프셋
        float offsetX = width * 0.5f;
        float offsetY = -height * 0.5f;
        boundaryCollider.offset = new Vector2(offsetX, offsetY);
        boundaryCollider.transform.position = new Vector2(-1.5f, 1.5f);
        
        // 최소 콜라이더 크기 확보
        float camW = Camera.main.orthographicSize * Camera.main.aspect * 2f;
        float camH = Camera.main.orthographicSize * 2f;
        if( camW < width ) camW = width;
        if( camH < height ) camH = height;
        boundaryCollider.size = new Vector2(width, height);
        
        // 컨파이너 캐시 업데이트
        var confiner = vcamP1.GetComponent<CinemachineConfiner2D>();
        confiner.InvalidateBoundingShapeCache();
        confiner = vcamP2.GetComponent<CinemachineConfiner2D>();
        confiner.InvalidateBoundingShapeCache();
    }

    void AlertToPlayer()
    {
        // Id 찾기
        StageState state = stageManager.CurrentState;
        int p1Id = state.GetPlayerIdBySlot(1);
        int p2Id = state.GetPlayerIdBySlot(2);
        int playerId;
        playerId = (vcamP1.Priority == 20) ? p2Id : p1Id;
        var request = new ViewRequest
        {
            Id = playerId,
            Callback = v => PrintWarningIcon(v)
        };
        stageManager.CurrentState.Events.RaiseViewRequest(request);
    }
    private bool IsPointVisible(GridEntityView view)
    {
        Vector3 worldPos = view.transform.position;
        
        // 월드 좌표를 뷰포트 좌표(0~1 사이)로 변환
        Vector3 viewPos = Camera.main.WorldToViewportPoint(worldPos);
    
        // 0~1 사이라면 화면 안
        return viewPos.x >= 0 && viewPos.x <= 1 && 
               viewPos.y >= 0 && viewPos.y <= 1 && viewPos.z > 0;
    }

    private bool IsPointVisible(Vector3 worldPos)
    {
        Vector3 viewPos = Camera.main.WorldToViewportPoint(worldPos);
        
        return viewPos.x >= 0 && viewPos.x <= 1 && 
               viewPos.y >= 0 && viewPos.y <= 1 && viewPos.z > 0;
    }

    void PrintWarningIcon(GridEntityView view)
    {
        Vector3 viewPos = Camera.main.WorldToViewportPoint(view.transform.position);
        if (IsPointVisible(view))
        {
            warningIcon.gameObject.SetActive(false);
            return;
        }
        
        warningIcon.gameObject.SetActive(true);
        // 화면 비율 기준으로 출력 위치 제한
        float x = Mathf.Clamp(viewPos.x, 0.25f, 0.75f);
        float y = Mathf.Clamp(viewPos.y, 0.25f, 0.75f);
        
        warningIcon.anchorMin = warningIcon.anchorMax = new Vector2(x, y);
            
        
        
    }
}
