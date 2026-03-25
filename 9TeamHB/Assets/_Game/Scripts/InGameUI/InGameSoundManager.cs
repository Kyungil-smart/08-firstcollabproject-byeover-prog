using UnityEngine;

public class InGameSoundManager : MonoBehaviour
{
    public static InGameSoundManager Instance;

    [Header("오디오 소스")] 
    public AudioSource bgmSource;
    public AudioSource sfxSource;
    
    [Header("공용 사운드 테이프 (Clip)")]
    public AudioClip mainBGM;        // 게임 배경음악
    public AudioClip defaultButtonClickSFX; // 기본 클릭 소리
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(this);
        }
        else
        {
            Destroy(gameObject);
        }
        
    }

    private void Start()
    {
        
    }

    
    // 슬라이더로 볼륨 설정.
    public void SetBGMVolume(float volume)
    {
        bgmSource.volume = volume;
    }

    public void SetSFXVolume(float volume)
    {
        sfxSource.volume = volume;
    }
    
    // 소리 재생 함수. 
    public void PlaySFX(AudioClip clip)
    {
        sfxSource.PlayOneShot(clip); //PlayOneShot으로 하나의 스피커에서 여러개의 효과음 재생 
    }

    public void PlayBasicButtonClickSound()
    {
        if (defaultButtonClickSFX != null)
        {
            PlaySFX(defaultButtonClickSFX);
        }
    }
    
    // BGM 재생
    public void PlayBGM(AudioClip clip)
    {
        bgmSource.clip = clip;
        bgmSource.loop = true; 
        bgmSource.Play(); //브금은 중복안되게 Play로 재쟁.
    }
}
