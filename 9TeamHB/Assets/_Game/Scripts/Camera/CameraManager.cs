using UnityEngine;
using MyGame2.Stage;
using Unity.Cinemachine;

public class CameraManager : MonoBehaviour
{
    [SerializeField] private StageManager stageManager;
    [SerializeField] private CinemachineCamera vcamP1;
    [SerializeField] private CinemachineCamera vcamP2;

    public void SwitchToPlayer(int playerNumber)
    {
        // 활성 플래이어를 입력 받으면 카메라 우선 순위 변경
        vcamP1.Priority = (playerNumber == 1) ? 20 : 10;
        vcamP2.Priority = (playerNumber == 2) ? 20 : 10;
    }
}
