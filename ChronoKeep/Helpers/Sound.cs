using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Chronokeep.Helpers
{
    internal class AudioPlaybackEngine : IDisposable
    {
        private readonly WaveOutEvent outputDevice = new();
        private readonly MixingSampleProvider mixer;

        private static AudioPlaybackEngine? Instance = null;
        private static int CurrentIndex = 0;
        private static CachedSound alert = new(Path.Combine("Sounds", "alert-1.wav"));

        private AudioPlaybackEngine(int sampleRate = 44100, int channelCount = 2)
        {
            mixer = new MixingSampleProvider(WaveFormat.CreateIeeeFloatWaveFormat(sampleRate, channelCount))
            {
                ReadFully = true
            };
            outputDevice.Init(mixer);
            outputDevice.Play();
        }

        public static void PlaySound(string fileName)
        {
            Instance ??= new(44100, 1);
            var input = new AudioFileReader(fileName);
            Instance.AddMixerInput(new AutoDisposeFileReader(input));
        }

        public static void PlaySound(int index)
        {
            if (index != CurrentIndex)
            {
                LoadCachedSound(index);
            }
            Instance ??= new(44100, 1);
            Instance.AddMixerInput(new CachedSoundSampleProvider(alert));
        }

        public static void LoadCachedSound(int index)
        {
            CurrentIndex = index;
            string soundFile = Path.Combine("Sounds", "alert-1.wav");
            switch (index)
            {
                case 1:
                    soundFile = Path.Combine("Sounds", "alert-2.wav");
                    break;
                case 2:
                    soundFile = Path.Combine("Sounds", "alert-3.wav");
                    break;
                case 3:
                    soundFile = Path.Combine("Sounds", "alert-4.wav");
                    break;
                case 4:
                    soundFile = Path.Combine("Sounds", "alert-5.wav");
                    break;
                case 5:
                    soundFile = Path.Combine("Sounds", "emily-runner-here.wav");
                    break;
                case 6:
                    soundFile = Path.Combine("Sounds", "emily-runner-arrived.wav");
                    break;
                case 7:
                    soundFile = Path.Combine("Sounds", "emily-alert-runner-here.wav");
                    break;
                case 8:
                    soundFile = Path.Combine("Sounds", "michael-runner-here.wav");
                    break;
                case 9:
                    soundFile = Path.Combine("Sounds", "michael-runner-arrived.wav");
                    break;
                case 10:
                    soundFile = Path.Combine("Sounds", "michael-alert-runner-here.wav");
                    break;
            }
            alert = new(soundFile);
        }

        public void AddMixerInput(ISampleProvider input)
        {
            if (input.WaveFormat.Channels == mixer.WaveFormat.Channels)
            {
                mixer.AddMixerInput(input);
            }
            else if (input.WaveFormat.Channels == 1 && mixer.WaveFormat.Channels == 2)
            {
                mixer.AddMixerInput(new MonoToStereoSampleProvider(input));
            }
            else if (input.WaveFormat.Channels == 2 && mixer.WaveFormat.Channels == 1)
            {
                mixer.AddMixerInput(new StereoToMonoSampleProvider(input));
            }
        }

        public void Dispose()
        {
            outputDevice.Dispose();
        }
    }

    internal class CachedSoundSampleProvider(CachedSound cachedSound) : ISampleProvider
    {
        private readonly CachedSound cachedSound = cachedSound;
        private long position;

        public int Read(float[] buffer, int offset, int count)
        {
            var availableSamples = cachedSound.AudioData.Length - position;
            var samplesToCopy = Math.Min(availableSamples, count);
            Array.Copy(cachedSound.AudioData, position, buffer, offset, samplesToCopy);
            position += samplesToCopy;
            return (int)samplesToCopy;
        }

        public WaveFormat WaveFormat { get { return cachedSound.WaveFormat; } }
    }

    internal class AutoDisposeFileReader(AudioFileReader reader) : ISampleProvider
    {
        private readonly AudioFileReader reader = reader;
        private bool isDisposed;

        public WaveFormat WaveFormat { get; private set; } = reader.WaveFormat;

        public int Read(float[] buffer, int offset, int count)
        {
            if (isDisposed)
            {
                return 0;
            }
            int read = reader.Read(buffer, offset, count);
            if (read == 0)
            {
                reader.Dispose();
                isDisposed = true;
            }
            return read;
        }
    }

    internal class CachedSound
    {
        public float[] AudioData { get; private set; }
        public WaveFormat WaveFormat { get; private set; }

        public CachedSound(string audioFileName)
        {
            using var audioFileReader = new AudioFileReader(audioFileName);
            WaveFormat = audioFileReader.WaveFormat;
            var wholeFile = new List<float>((int)(audioFileReader.Length / 4));
            var readBuffer = new float[audioFileReader.WaveFormat.SampleRate * audioFileReader.WaveFormat.Channels];
            int samplesRead;
            while ((samplesRead = audioFileReader.Read(readBuffer, 0, readBuffer.Length)) > 0)
            {
                wholeFile.AddRange(readBuffer.Take(samplesRead));
            }
            AudioData = [.. wholeFile];
        }
    }
}
