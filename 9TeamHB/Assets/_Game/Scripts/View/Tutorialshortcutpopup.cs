using UnityEngine;

// 튜토리얼 단축키 안내 팝업.
//아무 키 또는 화면 터치 시 닫힘.

public class TutorialShortcutPopup : MonoBehaviour
{
    private bool _ready;

    private void OnEnable()
    {
        _ready = false;
    }

    private void Update()
    {
        // 첫 프레임 스킵 (Instantiate 직후 입력 무시)
        if (!_ready)
        {
            _ready = true;
            return;
        }

        // 아무 키 또는 마우스/터치 입력 시 닫기
        if (Input.anyKeyDown || Input.GetMouseButtonDown(0))
        {
            if (InGameUIManager.Instance != null)
                InGameUIManager.Instance.CloseTutorialPopup();
        }
    }
}