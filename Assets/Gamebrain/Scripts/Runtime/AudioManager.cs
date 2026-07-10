using UnityEngine;
using GameBrain.Utils;
using System.Collections.Generic;

namespace GameBrain.Casual
{
    public class AudioManager
    {
        private Feedbacks_SO feedbacks_SO;
        private SettingsData settingsData;
        private Dictionary<EffectType, AudioFxInfo> audioFxDictionary = new Dictionary<EffectType, AudioFxInfo>();
        private AudioSource MusicSource;

        public AudioManager(Feedbacks_SO feedbacks_SO, SettingsData settingsData)
        {
            this.feedbacks_SO = feedbacks_SO;
            this.settingsData = settingsData;
        }

        public void Init()
        {
            SetAudioFxDictionary();
        }

        public void AudioStatusIsChanged(bool status)
        {
            EventBus<AudioStatusIsChangedEvent>.Raise(new AudioStatusIsChangedEvent(status));
        }

        private void SetAudioFxDictionary()
        {
            GameObject audioSourcesHolder = new GameObject("Audio Sources");
            foreach (var effect in feedbacks_SO.EffectPresets)
            {
                if (!audioFxDictionary.ContainsKey(effect.ClipType))
                {
                    AudioFxInfo audioFxInfo = new AudioFxInfo(effect.Clip, effect.Volume, effect.ClipType, audioSourcesHolder.AddComponent<AudioSource>(),
                        effect.HasRandomPitch, effect.Pitch_Min, effect.Pitch_Max);
                    audioFxDictionary.Add(audioFxInfo.ClipType, audioFxInfo);
                }
            }
        }

        public void PlayAudioClip(EffectType clipType)
        {
            if (settingsData.GetAudioFxStatus())
            {
                AudioFxInfo audioFxInfo;
                if (audioFxDictionary.TryGetValue(clipType, out audioFxInfo))
                    audioFxInfo.Play();
            }
        }

        //private void OnMusicStatusChanged(bool status)
        //{
        //    if (status)
        //        PlayMusic();
        //    else
        //        StopMusic();
        //}

        //public void PlayMusic()
        //{
        //    MusicSource.Play();
        //}

        //public void StopMusic()
        //{
        //    MusicSource.Stop();
        //}
    }

    public class AudioFxInfo
    {
        public AudioClip Clip;
        public float Volume;
        public EffectType ClipType;
        public AudioSource AudioSource;
        public bool HasRandomPitch;
        public float Pitch_Min;
        public float Pitch_Max;

        public AudioFxInfo(AudioClip clip, float volume, EffectType clipType, AudioSource audioSource, bool hasRandomPitch, float pitchMin, float pitchMax)
        {
            Clip = clip;
            Volume = volume;
            ClipType = clipType;
            HasRandomPitch = hasRandomPitch;
            Pitch_Min = pitchMin;
            Pitch_Max = pitchMax;

            if (hasRandomPitch)
            {
                if (Pitch_Min == 0)
                    Pitch_Min = 1;
                if (Pitch_Max == 0)
                    Pitch_Max = 1;
            }

            SetSource(audioSource);
        }

        private void SetSource(AudioSource source)
        {
            AudioSource = source;
            AudioSource.clip = Clip;
            AudioSource.volume = Volume;
        }

        public void Play()
        {
            if (Clip == null)
                return;
            if (HasRandomPitch)
                AudioSource.pitch = Random.Range(Pitch_Min, Pitch_Max);
            AudioSource.Play();
        }
    }

    public enum EffectType
    {
        Null = -1,
        Button = 0,
        Drag = 10, 
        Drop = 20,
        LevelComplete = 30,
        BoardObjectTookHit = 70,
        RocketLaunch = 80,
        RocketHit = 90,
        InvalidDrop = 100,
        Blast = 110,
        WoodHit_1 = 120,
        WoodHit_2 = 130,
        WoodHit_3 = 140,
        Ice_1 = 150,
        Ice_2 = 160,
        Ice_3 = 170,
        Hammer = 180,
        Swap = 190,
        Fail = 200,
        UnLockCell = 210,
        Clay = 220,
        PinTap,
        RectangelTap,
        RectangleRolling,
        SlotBoxFull,
        InvalidTap,
        SlotLoaded,
        FireWorks
    }
}
