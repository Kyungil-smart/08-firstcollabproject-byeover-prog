using UnityEngine;
using System.Collections.Generic;

public class KeyFollower : MonoBehaviour
{
    public Transform target; // 추적 대상
    public float distance = 0.5f; // 간격 설정
    public float minDistance = 0.1f; // 기록용 최소거리
    
    //추적 해나갈 경로
    private List<Vector3> _history = new List<Vector3>(50);

    void Update()
    {
        
        bool isFirstRecord = _history.Count == 0;
        // 첫 기록 0, 나머지는 거리 계산  
        float distFromLast = isFirstRecord ? 
            0 : Vector3.Distance(_history[_history.Count - 1], target.position);

        // 대상이 기록 지점으로부터 충분히 멀어졌을 때만 추가
        if (isFirstRecord || distFromLast > minDistance)
        {
            _history.Add(target.position);
        }
        // 뒤에서부터 'index'만큼 떨어진 과거 데이터 참조
        int index = Mathf.CeilToInt(distance * 10);
        if (_history.Count > index)
        {
            Vector3 targetPos = _history[_history.Count - 1 - index];
        
            // 가만히 있을 때 떨림 방지를 위해 거리 체크 후 이동
            if (Vector3.Distance(transform.position, targetPos) > 0.01f)
            {
                transform.position = Vector3.Lerp(transform.position, 
                    targetPos, Time.deltaTime * 10f);
            }
        }

        // 리스트 크기 관리 (선택적 삭제)
        if (_history.Count > index + 20)
        {
            _history.RemoveAt(0); // 맨 앞의 가장 오래된 데이터 삭제
        }
    }
}