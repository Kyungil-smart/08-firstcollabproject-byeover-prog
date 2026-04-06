using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.IO;

[System.Serializable]
public class GoogleSheetData
{
    [Tooltip("저장될 시트 이름")]
    public string fileName;
    
    [Tooltip("테이블 CSV 링크")]
    public string sheetURL;
}

public class GoogleSheetUpdater : MonoBehaviour
{
    [Header("다운로드할 구글 시트 목록")]
    public GoogleSheetData[] sheetsToDownload;

    [ContextMenu("시트 데이터 가져오기")]
    public void DownloadData()
    {
        StartCoroutine(DownloadAllCSV());
    }

    IEnumerator DownloadAllCSV()
    {
        bool hasError = false;
        
        foreach (GoogleSheetData sheet in sheetsToDownload)
        {
            if (string.IsNullOrEmpty(sheet.sheetURL) || string.IsNullOrEmpty(sheet.fileName))
            {
                continue;
            }

            using (UnityWebRequest www = UnityWebRequest.Get(sheet.sheetURL))
            {
                yield return www.SendWebRequest();

                if (www.result == UnityWebRequest.Result.Success)
                {
                    // 지정된 이름으로 csv 파일 저장
                    string path = Application.dataPath + $"/_Game/Resources/{sheet.fileName}.csv";
                    File.WriteAllText(path, www.downloadHandler.text, System.Text.Encoding.UTF8);
                    Debug.Log($"[{sheet.fileName}] 업데이트 완료");
                }
                else
                {
                    Debug.LogError($"[{sheet.fileName}] 다운로드 실패: " + www.error);
                    hasError = true;
                }
            }
        }
        
#if UNITY_EDITOR
        // 다운로드 후 에셋 데이터베이스 새로고침
        UnityEditor.AssetDatabase.Refresh();
#endif
        
        if (!hasError)
        {
            
        }
    }
}