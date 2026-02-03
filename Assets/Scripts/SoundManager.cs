using System;
using System.Collections.Generic;
using UnityEngine;

public enum SoundType
{
    Land,
    ClearLine,
    GameOver,
    LevelUp,
    Bgm,
    UIClick
}

/// <summary>
/// 사운드 재생을 관리하는 매니저
/// </summary>
public class SoundManager : MonoBehaviour
{
    [Serializable]
    private struct SoundEntry
    {
        public SoundType type;
        public AudioClip clip;
        [Range(0f, 1f)] public float volume;
    }

    [Header("사운드 설정")]
    [SerializeField] private SoundEntry[] soundEntries;

    private Dictionary<SoundType, SoundEntry> soundMap;
    private AudioSource audioSource;
    private AudioSource bgmAudioSource;

    private void Awake()
    {
        audioSource = gameObject.AddComponent<AudioSource>();
        bgmAudioSource = gameObject.AddComponent<AudioSource>();
        bgmAudioSource.loop = true;
        BuildSoundMap();
    }

    private void BuildSoundMap()
    {
        soundMap = new Dictionary<SoundType, SoundEntry>();
        if (soundEntries == null) return;

        foreach (var entry in soundEntries)
        {
            soundMap[entry.type] = entry;
        }
    }

    /// <summary>
    /// 사운드 재생
    /// </summary>
    public void Play(SoundType type)
    {
        if (soundMap.TryGetValue(type, out var entry) && entry.clip != null)
        {
            audioSource.PlayOneShot(entry.clip, entry.volume);
        }
    }

    /// <summary>
    /// BGM 루프 재생
    /// </summary>
    public void PlayBGM()
    {
        if (soundMap.TryGetValue(SoundType.Bgm, out var entry) && entry.clip != null)
        {
            bgmAudioSource.clip = entry.clip;
            bgmAudioSource.volume = entry.volume;
            bgmAudioSource.Play();
        }
    }

    /// <summary>
    /// BGM 정지
    /// </summary>
    public void StopBGM()
    {
        bgmAudioSource.Stop();
    }
}
