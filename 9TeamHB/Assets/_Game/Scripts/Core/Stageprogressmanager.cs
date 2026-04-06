using UnityEngine;

public static class StageProgressManager
{
    private const string ClearKeyPrefix = "StageClear_";  // StageClear_0, StageClear_1, ...
    private const int ClearedValue = 1;
    
    // 해당 인덱스의 스테이지를 클리어했는지 확인.
    public static bool IsCleared(int stageIndex)
    {
        return PlayerPrefs.GetInt(ClearKeyPrefix + stageIndex, 0) >= ClearedValue;
    }
    
    // 해당 인덱스의 스테이지가 플레이 가능한지 확인.
    // Index 0은 항상 해금. 그 외는 이전 스테이지 클리어 시 해금.
   
    public static bool IsUnlocked(int stageIndex)
    {
        if (stageIndex <= 0) return true;
        return IsCleared(stageIndex - 1);
    }

    // 클리어한 스테이지 중 가장 높은 인덱스. 하나도 없으면 -1.
    public static int GetHighestClearedIndex(int totalStages)
    {
        for (int i = totalStages - 1; i >= 0; i--)
        {
            if (IsCleared(i)) return i;
        }
        return -1;
    }

    // 해당 인덱스의 스테이지를 클리어 처리하고 저장.
    public static void MarkCleared(int stageIndex)
    {
        PlayerPrefs.SetInt(ClearKeyPrefix + stageIndex, ClearedValue);
        PlayerPrefs.Save();
        Debug.Log($"[StageProgress] Stage {stageIndex} 클리어 저장 완료.");
    }
    
    // 모든 클리어 기록 초기화 (디버그/테스트용).
    public static void ResetAll(int totalStages)
    {
        for (int i = 0; i < totalStages; i++)
        {
            PlayerPrefs.DeleteKey(ClearKeyPrefix + i);
        }
        PlayerPrefs.Save();
        Debug.Log($"[StageProgress] 전체 클리어 기록 초기화 ({totalStages}개).");
    }

    // 모든 스테이지 강제 클리어 (디버그/테스트용).
    public static void UnlockAll(int totalStages)
    {
        for (int i = 0; i < totalStages; i++)
        {
            PlayerPrefs.SetInt(ClearKeyPrefix + i, ClearedValue);
        }
        PlayerPrefs.Save();
        Debug.Log($"[StageProgress] 전체 스테이지 해금 ({totalStages}개).");
    }
}