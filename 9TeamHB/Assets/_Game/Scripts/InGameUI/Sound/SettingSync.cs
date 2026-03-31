using UnityEngine;
using UnityEngine.UI;

public class SettingSync : MonoBehaviour
{
    public Slider bgmSlider;
    public Slider sfxSlider;
    
    private void OnEnable()
    {
        if (InGameSoundManager.Instance != null)
        {
            // 활성화 되면 사운드 매니저의 소리값을 가지고 슬라이더를 맞춤.
            if (bgmSlider != null) bgmSlider.value = InGameSoundManager.Instance.bgmSource.volume;
            if (sfxSlider != null) sfxSlider.value = InGameSoundManager.Instance.sfxSource.volume;
        }
    }
}