using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.IO;

public class GoogleSheetDownloader : MonoBehaviour
{
    // 구글 시트에서 파일 -> 공유 -> 웹에 게시에서 게시했을때 나오는 CSV링크
    [Header("구글 시트 CSV 링크")]
    public string sheetURL = "https://docs.google.com/spreadsheets/d/e/2PACX-1vT2UZqE-l6VeUOQfEa7j8TFFuU0lqMopMzXBgBqE4BfsJlpzjc0t4mtkFtSKVfohXwZZ3PiqwwsUIYC/pub?gid=0&single=true&output=csv";

    // Inspector 창에서 마우스 우클릭으로 웹 데이터 가져오기  
    [ContextMenu("시트 데이터 가져오기")] //우클릭 시 아래 한 개의 함수 실행
    public void DownloadData()
    {
        StartCoroutine(DownloadCSV());
    }

    IEnumerator DownloadCSV()
    {
        using (UnityWebRequest www = UnityWebRequest.Get(sheetURL))
        {
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                // Assets/Resources 폴더 안에 LocalizationTable.csv 라는 이름으로 덮어쓰기 저장
                string path = Application.dataPath + "/Resources/LocalizationTable.csv";
                File.WriteAllText(path, www.downloadHandler.text);

                // 유니티 에디터에게 "새 파일 들어왔으니 새로고침 해줘!" 라고 알림
#if UNITY_EDITOR
                UnityEditor.AssetDatabase.Refresh();
#endif
                Debug.Log("업데이트 완료");
            }
            else
            {
                Debug.LogError("다운로드 실패: " + www.error);
            }
        }
    }
}