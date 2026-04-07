using System.Collections.Generic;
using UnityEngine;
using TMPro; 

// 사용 안합니다
// 설명: 스토리의 경우 대사를 마우스 클릭 시 대사를 연속적으로 뱉게 해주는 클래스. (스토리에 한해 LocalizedText대신 사용) 
// StoryUIController는 LocailzedText처럼 테이블에 일치하는 키 값을 startStoryKey에 넣어주면 그 값부터 _01, _02, _03 ... 차례대로 재생할 수 있게 해줌.   
public class StoryUIController : MonoBehaviour
{
    [Header("시작 스토리 키 설정(시작할 키값을 넣습니다. 이름_00")]
    public string startStoryKey;
    [Header("UI 연결")]
    public GameObject storyPanel;       // 대화창 배경 패널
    public TextMeshProUGUI storyText;   // 텍스트
    private List<string> currentStoryList = new List<string>(); // 매니저에서 가져온 스토리 텍스트들을 담아둘 리스트
    private int currentLineIndex = 0; // 현재 몇 번째 줄을 읽고 있는지 인덱스
    private bool isStoryPlaying = false; // 현재 스토리가 재생 중인지 체크

    private string currentStoryKey; // 현재 실행중인 스토리가 뭔지 기억하는 변수 (StartKey값)
    
    
    void Start()
    {
        LocalizationManager.LanguageChangedEvent += OnLanguageChanged;
        
        // 키 값이 비어있지 않다면 자동 실행
        if (!string.IsNullOrEmpty(startStoryKey))
        {
            PlayStory(startStoryKey); 
        }
    }

    void OnDestroy()
    {
        LocalizationManager.LanguageChangedEvent -= OnLanguageChanged;
    }
    
    void Update()
    {
        // 스토리가 재생 중이고 && 마우스 왼쪽 버튼(0)을 클릭했을 때 텍스트 넘김.
        if (isStoryPlaying && Input.GetMouseButtonDown(0))
        {
            ShowNextLine();
        }
    }

    // 스토리를 시작할 때 부르는 함수
    public void PlayStory(string startKey)
    {
        currentStoryKey = startKey; //지금 어떤 스토리 대사 읊고 있는지 저장. 
        
        // LocalizationManager에서 StartKey_00, _01 ... 로 리스트만들기.
        currentStoryList = LocalizationManager.Instance.GetStorySequence(startKey);
        
        
        if (currentStoryList.Count == 0)
        {
            Debug.Log("대사를 찾을 수 없습니다.");
            return;
        }

        if (LocalizationManager.Instance.mainFont != null)
        {
            storyText.font = LocalizationManager.Instance.mainFont;
        }
        
        // 변수 초기화 및 대화창 UI 켜기
        currentLineIndex = 0;
        isStoryPlaying = true;
        if (storyPanel != null) storyPanel.SetActive(true);

        // 첫 번째 대사 출력
        ShowNextLine();
    }

    // 다음 대사로 넘어가는 함수
    private void ShowNextLine()
    {
        if (currentLineIndex < currentStoryList.Count)
        {
            storyText.text = currentStoryList[currentLineIndex]; //현 텍스트 표시
            
            currentLineIndex++; 
        }
        else
        {
            // 더 이상 읽을 대사가 없다면 스토리 종료
            EndStory();
        }
    }

    // 스토리 종료 시 처리
    private void EndStory()
    {
        Debug.Log("스토리 재생 완료");
        isStoryPlaying = false;
        
        // 대화창 배경 패널 끄기
        if (storyPanel != null) storyPanel.SetActive(false);
        
        // 스토리 끝난뒤 다른 씬 로드하거나 하기 .
    }
    
    // 스토리 텍스트 실행 중 바뀌는 경우 현재 설정된 언어로 새로고침.
    private void OnLanguageChanged()
    {
        //스토리가 진행중일때 새로고침. 
        if (isStoryPlaying && !string.IsNullOrEmpty(currentStoryKey)) //IsNullOrEmpty -> String값이 비어있는지 확인 
        {
            // 폰트 유지 .
            if (LocalizationManager.Instance.mainFont != null)
            {
                storyText.font = LocalizationManager.Instance.mainFont;
            }
            
            // 스토리 진행중 언어 바뀔때 갱신
            currentStoryList = LocalizationManager.Instance.GetStorySequence(currentStoryKey);
            
            // Text변경.
            if (currentLineIndex > 0 && currentLineIndex <= currentStoryList.Count)
            {
                storyText.text = currentStoryList[currentLineIndex - 1];
            }
        }
    }
}