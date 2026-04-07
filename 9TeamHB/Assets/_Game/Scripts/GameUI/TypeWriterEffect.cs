using System.Collections;
using UnityEngine;
using TMPro;

public class TypewriterEffect : MonoBehaviour
{
    [Header("텍스트 컴포넌트")]
    public TextMeshProUGUI targetText;

    [Header("타이핑 설정")]
    public float typeSpeed = 0.05f; 

    private Coroutine typingCoroutine;

    
    public void PlayText(string fullText)
    {
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
        }
        typingCoroutine = StartCoroutine(TypeTextRoutine(fullText));
    }

    private IEnumerator TypeTextRoutine(string fullText)
    {
        targetText.text = fullText;
        
        targetText.ForceMeshUpdate(); 
        int totalVisibleCharacters = targetText.textInfo.characterCount;
        
        targetText.maxVisibleCharacters = 0;
        
        for (int i = 0; i <= totalVisibleCharacters; i++)
        {
            targetText.maxVisibleCharacters = i;
            
            //InGameSoundManager.Instance.PlayBasicButtonClickSound();

            yield return new WaitForSeconds(typeSpeed);
        }
    }
    
    public void SkipTyping()
    {
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
        }
        targetText.maxVisibleCharacters = 99999; 
    }
}