using System;
using UnityEngine;

// 클립 + 개별 볼륨을 묶는 구조체.
// Inspector에서 클립 옆에 볼륨 슬라이더가 나타남.
[Serializable]
public struct SoundEntry
{
    [Tooltip("오디오 클립")]
    public AudioClip clip;

    [Tooltip("개별 볼륨 (0~1)")]
    [Range(0f, 1f)]
    public float volume;

    // Inspector에서 새로 만들면 volume 기본값이 0이라 안 들리는 문제 방지
    public float SafeVolume => volume <= 0.001f && clip != null ? 1f : volume;
}

public class InGameSoundManager : MonoBehaviour
{
    public static InGameSoundManager Instance;

    [Header("오디오 소스")]
    public AudioSource bgmSource;
    public AudioSource sfxSource;
    
    // BGM

    [Header("BGM")]
    public SoundEntry mainBGM;            // Start_OP_Main (타이틀)
    public SoundEntry bgmInPuzzle1;       // InPuzzle_1
    public SoundEntry bgmInPuzzle2;       // InPuzzle_2
    public SoundEntry bgmStageSelect;     // StageSelect
    public SoundEntry bgmEnding;          // Ending
    
    // SFX — 이동

    [Header("SFX — 이동")]
    public SoundEntry sfxSlimeStep;
    [Tooltip("인간 풋스탭 1~3 순환")]
    public SoundEntry[] sfxFootSteps;
    private int _footStepIndex;
    
    // SFX — 오브젝트

    [Header("SFX — 오브젝트")]
    public SoundEntry sfxObjectPush;
    public SoundEntry sfxIcePush;
    public SoundEntry sfxPressButton;     // 버튼/레버
    public SoundEntry sfxBreakBox;
    public SoundEntry sfxDoorOpen;
    public SoundEntry sfxDoorClose;
    public SoundEntry sfxGetKey;
    public SoundEntry sfxVent;
    public SoundEntry sfxHideOnBush;
    
    // SFX — 게임 이벤트

    [Header("SFX — 게임 이벤트")]
    public SoundEntry sfxDetect;
    public SoundEntry sfxSummon;
    public SoundEntry sfxDamaged;         // 게임 오버 즉시
    public SoundEntry sfxGameOver;        // 게임 오버 팝업
    public SoundEntry sfxGameClearLayer1;
    public SoundEntry sfxGameClearLayer2;
    public SoundEntry sfxGameClearPopup;
    public SoundEntry sfxGameEnter;
    public SoundEntry sfxCorrect;
    
    // SFX — 태그

    [Header("SFX — 태그")]
    public SoundEntry sfxChange2Slime;
    public SoundEntry sfxChange2Human;
    public SoundEntry sfxChrTag;
    
    // SFX — UI

    [Header("SFX — UI")]
    public SoundEntry sfxConfirmUI;
    public SoundEntry sfxCancelUI;
    public SoundEntry defaultButtonClickSFX;
    
    // 생명주기

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        if (mainBGM.clip != null)
            PlayBGM(mainBGM);
    }
    
    // 마스터 볼륨 (설정 슬라이더용)
    private float _masterBGMVolume = 1f;
    private float _masterSFXVolume = 1f;
    private float _currentBGMEntryVolume = 1f;

    public void SetBGMVolume(float volume)
    {
        _masterBGMVolume = volume;
        if (bgmSource != null)
            bgmSource.volume = _masterBGMVolume * _currentBGMEntryVolume;
    }

    public void SetSFXVolume(float volume)
    {
        _masterSFXVolume = volume;
        if (sfxSource != null) sfxSource.volume = _masterSFXVolume;
    }
    
    // SFX 재생 (개별 볼륨 적용)

    public void PlaySFX(SoundEntry entry)
    {
        if (entry.clip == null || sfxSource == null) return;
        sfxSource.PlayOneShot(entry.clip, entry.SafeVolume * _masterSFXVolume);
    }

    // AudioClip 직접 재생 (하위 호환)
    public void PlaySFX(AudioClip clip)
    {
        if (clip == null || sfxSource == null) return;
        sfxSource.PlayOneShot(clip, _masterSFXVolume);
    }

    public void PlayBasicButtonClickSound()
    {
        PlaySFX(defaultButtonClickSFX);
    }

    public void PlayConfirmUI()  { PlaySFX(sfxConfirmUI); }
    public void PlayCancelUI()   { PlaySFX(sfxCancelUI); }

    public void PlayFootStep()
    {
        if (sfxFootSteps == null || sfxFootSteps.Length == 0) return;
        PlaySFX(sfxFootSteps[_footStepIndex]);
        _footStepIndex = (_footStepIndex + 1) % sfxFootSteps.Length;
    }

    public void PlayGameClear()
    {
        PlaySFX(sfxGameClearLayer1);
        PlaySFX(sfxGameClearLayer2);
    }

    public void PlayGameClearPopup() { PlaySFX(sfxGameClearPopup); }
    public void PlayGameOverPopup()  { PlaySFX(sfxGameOver); }

    public void PlayTag(bool toSlime)
    {
        if (toSlime && sfxChange2Slime.clip != null)
            PlaySFX(sfxChange2Slime);
        else if (!toSlime && sfxChange2Human.clip != null)
            PlaySFX(sfxChange2Human);
        else
            PlaySFX(sfxChrTag);
    }
    
    // BGM 재생 (개별 볼륨 적용)

    public void PlayBGM(SoundEntry entry)
    {
        if (entry.clip == null || bgmSource == null) return;
        if (bgmSource.clip == entry.clip && bgmSource.isPlaying) return;

        _currentBGMEntryVolume = entry.SafeVolume;
        bgmSource.clip = entry.clip;
        bgmSource.loop = true;
        bgmSource.volume = _masterBGMVolume * _currentBGMEntryVolume;
        bgmSource.Play();
    }

    // AudioClip 직접 재생 (하위 호환)
    public void PlayBGM(AudioClip clip)
    {
        if (clip == null || bgmSource == null) return;
        if (bgmSource.clip == clip && bgmSource.isPlaying) return;
        _currentBGMEntryVolume = 1f;
        bgmSource.clip = clip;
        bgmSource.loop = true;
        bgmSource.volume = _masterBGMVolume;
        bgmSource.Play();
    }

    public void StopBGM()
    {
        if (bgmSource != null) bgmSource.Stop();
    }

    public void PlayTitleBGM()       { PlayBGM(mainBGM); }
    public void PlayStageSelectBGM() { PlayBGM(bgmStageSelect); }
    public void PlayEndingBGM()      { PlayBGM(bgmEnding); }
}