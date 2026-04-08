using System.Collections.Generic;
using UnityEngine;
using TMPro;
public enum Language { English = 0, Korean = 1 }

public class LocalizationManager : MonoBehaviour
{
    public static LocalizationManager Instance;
    private Dictionary<string, string[]> localizationData = new Dictionary<string, string[]>();
    public Language currentLanguage = Language.English;
    public TMP_FontAsset mainFont; // 메인으로 모든 글자에 적용되는 폰트
    public TMP_FontAsset storyFont; // 스토리에 적용되는 폰트. 

    public delegate void OnLanguageChanged();
    public static event OnLanguageChanged LanguageChangedEvent;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this; 
            DontDestroyOnLoad(gameObject); 
        
            LoadCSV("LocalizationTable");
            LoadCSV("StoryTable");
        
            // 저장된 언어 불러오기
            currentLanguage = (Language)PlayerPrefs.GetInt("SavedLanguage", (int)Language.English);
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
    
    // LocalizationTable CSV 파일에서  키값, 벨류값. 불러옴 
    void LoadCSV(string fileName)
    {
        TextAsset csvFile = Resources.Load<TextAsset>(fileName); 
        if (csvFile == null) return;

        string[] lines = csvFile.text.Split('\n');
        
       // 셋째줄부터 읽음
        for (int i = 2; i < lines.Length; i++) 
        {
            string line = lines[i].Trim();
            if (string.IsNullOrEmpty(line)) continue;

            string[] columns = line.Split(',');
            if (columns.Length >= 3)
            {
                string englishText = CleanCsvText(columns[1]);
                string koreanText = CleanCsvText(columns[2]);
                
                // 딕셔너리에 key에 대응하는 영한 텍스트 저장. 
                localizationData[columns[0]] = new string[] { englishText, koreanText };
            }
        }
    }

    // CSV 글자 정제
    private string CleanCsvText(string input)
    {
        if (string.IsNullOrEmpty(input)) return "";

        string processed = input;
        
        if (processed.StartsWith("\"") && processed.EndsWith("\""))
        {
            processed = processed.Substring(1, processed.Length - 2);
        }
        
        processed = processed.Replace("\"\"", "\"");
        
        processed = processed.Replace("\\n", "\n");

        return processed;
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

    // 스토리 텍스트 묶음 호출용 startKey입력시 startKey, _01, _02 ... 을 리스트화해서 반환.
    public List<string> GetStorySequence(string startKey)
    {
        List<string> sequence = new List<string>();
        int lastUnderscoreIndex = startKey.LastIndexOf('_');

        // 뒤에 _01 같은 번호가 안 붙은 키면 그냥 하나만 반환
        if (lastUnderscoreIndex == -1)
        {
            if (localizationData.ContainsKey(startKey)) sequence.Add(GetText(startKey));
            return sequence;
        }

        string baseName = startKey.Substring(0, lastUnderscoreIndex);
        string numberPart = startKey.Substring(lastUnderscoreIndex + 1);

        // 뒷부분이 숫자인지 확인
        if (!int.TryParse(numberPart, out int currentIndex))
        {
            if (localizationData.ContainsKey(startKey)) sequence.Add(GetText(startKey));
            return sequence;
        }

        // 시작번호부터 쭉 가져옴. List.Add _00, _01, _02....
        while (true)
        {
            string targetKey = $"{baseName}_{currentIndex:D2}";

            if (localizationData.ContainsKey(targetKey))
            {
                sequence.Add(GetText(targetKey));
                currentIndex++; 
            }
            else
            {
                break; 
            }
        }

        return sequence;
    }
    
    // 언어 바꿀 때 바뀔때 실행되는 함수들 실행 + currentLangauge 값 변경. 
    // 나중에 드롭다운 버튼 클릭시 값을 ChangeLanguage에 넘겨줌. 
    public void ChangeLanguage(int index)
    {
        currentLanguage = (Language)index;
    
        // 언어 설정 저장
        PlayerPrefs.SetInt("SavedLanguage", index);
        PlayerPrefs.Save();
    
        LanguageChangedEvent?.Invoke();
        Debug.Log("Changed Text");
    }
}