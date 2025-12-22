using NAudio.Dsp;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TagLib.Mpeg;

namespace MusicPlayer_ovh
{
    public class AudioPlayer: IDisposable
    {
        private IWavePlayer? outputDevice;
        private AudioFileReader? audioFile;
        public event EventHandler? SongFinished;
        public BiQuadFilter[]? filters;
        private EqualizerFilter? eq;
        private MixingSampleProvider mixer;
        private readonly float[] eqFrequencies = { 60, 150, 400, 1000, 2400, 15000 };

        public double TotalSeconds => audioFile?.TotalTime.TotalSeconds ?? 0;
        public string TotalSecondsStr
        {
            get
            {
                if (audioFile == null) return "0:00";

                TimeSpan t = audioFile.TotalTime;
                return string.Format("{0}:{1:D2}", (int)t.TotalMinutes, t.Seconds);
            }
        }
        public double CurrentSeconds => audioFile?.CurrentTime.TotalSeconds ?? 0;
        public string CurrentSecondsStr
        {
            get
            {
                if (audioFile == null) return "0:00";
                TimeSpan t = audioFile.CurrentTime;
                return string.Format("{0}:{1:D2}", (int)t.TotalMinutes, t.Seconds);
            }
        }

        public AudioPlayer()
        {

            var waveFormat = WaveFormat.CreateIeeeFloatWaveFormat(44100, 2);
            mixer = new MixingSampleProvider(waveFormat);
            mixer.ReadFully = true;

            eq = new EqualizerFilter(mixer, new float[] { 60, 150, 400, 1000, 2400, 15000 });

            for(int i = 0; i < 6; i++)
            {

                float gain = float.Parse(Properties.Settings.Default.Gains[i]);
                eq.UpdateFilter(i, eqFrequencies[i], gain);
            }
            outputDevice = new WaveOutEvent();
            outputDevice.Init(eq);
            outputDevice.Play();
        }

        public void Play(string path)
        {

            mixer.RemoveAllMixerInputs();

            var reader = new AudioFileReader(path);

            ISampleProvider input = reader;

            // match mp3s format
            if (reader.WaveFormat.SampleRate != mixer.WaveFormat.SampleRate ||
                reader.WaveFormat.Channels != mixer.WaveFormat.Channels)
            {
                input = new WdlResamplingSampleProvider(reader, mixer.WaveFormat.SampleRate);

                if (reader.WaveFormat.Channels == 1 && mixer.WaveFormat.Channels == 2)
                {
                    input = new MonoToStereoSampleProvider(input);
                }
            }

            mixer.AddMixerInput(input);
            audioFile = reader;

        }

        public void Pause()
        {
            outputDevice?.Pause();
        }

        public void Resume()
        {
            outputDevice?.Play();
        }

        public void Stop()
        {
            outputDevice?.Stop();
            outputDevice?.Dispose();
            audioFile?.Dispose();
            outputDevice = null;
            audioFile = null;
        }

        public void Seek(double seconds)
        {
            if (audioFile != null)
            {
                audioFile.CurrentTime = TimeSpan.FromSeconds(seconds);
            }
        }

        public void Volume(float volume)
        {
            if (audioFile != null)
            {
                audioFile.Volume = volume;
            }
        }

        public void UpdateEQ(int bandIndex, float gain)
        {
            eq?.UpdateFilter(bandIndex, eqFrequencies[bandIndex], gain);
        }

        public void Dispose() => Stop();
    }
}
