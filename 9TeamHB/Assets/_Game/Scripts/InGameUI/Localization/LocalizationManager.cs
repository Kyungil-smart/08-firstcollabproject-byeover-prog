using System.Collections.Generic;
using UnityEngine;

public enum Language { English = 0, Korean = 1 }

public class LocalizationManager : MonoBehaviour
{
    public static LocalizationManager Instance;
    private Dictionary<string, string[]> localizationData = new Dictionary<string, string[]>();
    public Language currentLanguage = Language.English;

    public delegate void OnLanguageChanged();
    public static event OnLanguageChanged LanguageChangedEvent;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this; DontDestroyOnLoad(gameObject); 
            LoadCSV();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        // ChangeLanguage((int)Language.Korean); //언어 한국어 변경 test 코드 
    }
    // LocalizationTable CSV 파일에서  키값, 헤더, 벨류값 등등.. 불러옴 
    void LoadCSV()
    {
        TextAsset csvFile = Resources.Load<TextAsset>("LocalizationTable"); // 모든 Resources 폴더 LocalizationTable찾아 로드 
        if (csvFile == null) return;

        string[] lines = csvFile.text.Split('\n');
        for (int i = 2; i < lines.Length; i++) // 첫 줄(헤더) 제외
        {
            string line = lines[i].Trim();
            if (string.IsNullOrEmpty(line)) continue;

            string[] columns = line.Split(',');
            if (columns.Length >= 3)
            {
                // Key 값 기준 행당 키의 영어, 한글 문자열들을 딕셔너리에 저장 
                localizationData[columns[0]] = new string[] { columns[1], columns[2] };
                //Debug.Log($"Key: {columns[0]}, English: {columns[1]}, Korean: {columns[2]}");
            }
        }
    }

    // 현재 설정된 언어의 텍스트르 얻기.
    public string GetText(string key)  
    {
        if (localizationData.ContainsKey(key))
        {
            return localizationData[key][(int)currentLanguage];
        }
        return key; 
    }

    // 언어 바꿀 때 바뀔때 실행되는 함수들 실행 + currentLangauge 값 변경. 
    // 나중에 드롭다운 버튼 클릭시 값을 ChangeLanguage에 넘겨줌. 
    public void ChangeLanguage(int index)
    {
        currentLanguage = (Language)index;
        LanguageChangedEvent?.Invoke(); // 전체 UI 새로고침 ( LocalizedText에서 텍스트 바꾸는 함수 등록해놨음)
        Debug.Log("Changed Text");
    }
}