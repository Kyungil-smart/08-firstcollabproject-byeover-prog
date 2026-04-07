using UnityEngine;
using UnityEngine.SceneManagement;

public class StageBackButton : MonoBehaviour
{
    public void OnClickBackButton()
    {
        SceneManager.LoadScene("Title_Scene");
    }
}
