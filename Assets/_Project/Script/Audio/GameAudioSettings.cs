using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public enum GameAudioChannel
{
    Music,
    SoundEffect
}

public static class GameAudioSettings
{
    private const string MusicKey = "audio.music.volume";
    private const string SfxKey = "audio.sfx.volume";
    private const string MutedKey = "audio.muted";
    private static readonly Dictionary<int, float> BaseVolumes = new Dictionary<int, float>();

    public static float MusicVolume { get; private set; } = 0.8f;
    public static float SfxVolume { get; private set; } = 0.8f;
    public static bool IsMuted { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        MusicVolume = PlayerPrefs.GetFloat(MusicKey, 0.8f);
        SfxVolume = PlayerPrefs.GetFloat(SfxKey, 0.8f);
        IsMuted = PlayerPrefs.GetInt(MutedKey, 0) != 0;
        AudioListener.volume = IsMuted ? 0f : 1f;
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode) => ApplyAllSources();

    public static void SetMusicVolume(float value)
    {
        MusicVolume = Mathf.Clamp01(value);
        PlayerPrefs.SetFloat(MusicKey, MusicVolume);
        PlayerPrefs.Save();
        ApplyAllSources();
    }

    public static void SetSfxVolume(float value)
    {
        SfxVolume = Mathf.Clamp01(value);
        PlayerPrefs.SetFloat(SfxKey, SfxVolume);
        PlayerPrefs.Save();
        ApplyAllSources();
    }

    public static void SetMuted(bool muted)
    {
        IsMuted = muted;
        AudioListener.volume = muted ? 0f : 1f;
        PlayerPrefs.SetInt(MutedKey, muted ? 1 : 0);
        PlayerPrefs.Save();
    }

    public static void ApplyAllSources()
    {
        foreach (AudioSource source in Object.FindObjectsByType<AudioSource>(FindObjectsSortMode.None))
        {
            if (source == null) continue;
            GameAudioChannelMarker marker = source.GetComponent<GameAudioChannelMarker>();
            GameAudioChannel channel = marker != null
                ? marker.Channel
                : source.loop ? GameAudioChannel.Music : GameAudioChannel.SoundEffect;
            int id = source.GetInstanceID();
            if (!BaseVolumes.TryGetValue(id, out float baseVolume))
            {
                baseVolume = source.volume;
                BaseVolumes[id] = baseVolume;
            }
            source.volume = baseVolume * (channel == GameAudioChannel.Music ? MusicVolume : SfxVolume);
        }
    }
}

[DisallowMultipleComponent]
[RequireComponent(typeof(AudioSource))]
public sealed class GameAudioChannelMarker : MonoBehaviour
{
    [SerializeField] private GameAudioChannel channel = GameAudioChannel.SoundEffect;
    public GameAudioChannel Channel => channel;
    private void Awake() => GameAudioSettings.ApplyAllSources();
}

public sealed class AudioOptionsPanelController : MonoBehaviour
{
    [SerializeField] private Slider musicSlider;
    [SerializeField] private Slider sfxSlider;
    [SerializeField] private Toggle muteToggle;

    public void Configure(Slider music, Slider sfx, Toggle mute)
    {
        musicSlider = music;
        sfxSlider = sfx;
        muteToggle = mute;
        Bind();
    }

    private void Awake() => Bind();

    private void Bind()
    {
        if (musicSlider != null)
        {
            musicSlider.SetValueWithoutNotify(GameAudioSettings.MusicVolume);
            musicSlider.onValueChanged.RemoveListener(GameAudioSettings.SetMusicVolume);
            musicSlider.onValueChanged.AddListener(GameAudioSettings.SetMusicVolume);
        }
        if (sfxSlider != null)
        {
            sfxSlider.SetValueWithoutNotify(GameAudioSettings.SfxVolume);
            sfxSlider.onValueChanged.RemoveListener(GameAudioSettings.SetSfxVolume);
            sfxSlider.onValueChanged.AddListener(GameAudioSettings.SetSfxVolume);
        }
        if (muteToggle != null)
        {
            muteToggle.SetIsOnWithoutNotify(GameAudioSettings.IsMuted);
            muteToggle.onValueChanged.RemoveListener(GameAudioSettings.SetMuted);
            muteToggle.onValueChanged.AddListener(GameAudioSettings.SetMuted);
        }
    }
}
