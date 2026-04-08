using UnityEngine;

// 스토리에 들어갈 1페이지
[System.Serializable]
public struct StoryPage
{
    public Sprite cutsceneImage;
    [TextArea(1, 2)] 
    public string storyKey; // 기존 storyText를 storyKey로
}

[CreateAssetMenu(fileName = "NewStoryData", menuName = "Story/StoryData")]
public class StoryData : ScriptableObject
{
    public StoryPage[] pages;
}

