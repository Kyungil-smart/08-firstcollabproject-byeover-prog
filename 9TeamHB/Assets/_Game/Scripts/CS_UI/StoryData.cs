using UnityEngine;

// 스토리에 들어갈 1페이지
[System.Serializable]
public struct StoryPage
{
    public Sprite cutsceneImage;
    [TextArea(3, 5)] 
    public string storyText;
}

[CreateAssetMenu(fileName = "NewStoryData", menuName = "Story/StoryData")]
public class StoryData : ScriptableObject
{
    public StoryPage[] pages;
}
