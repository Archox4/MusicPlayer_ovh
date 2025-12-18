using NAudio.Wave;
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

        public void Play(string path)
        {

            Stop();

            outputDevice = new WaveOutEvent();

            audioFile = new AudioFileReader(path);

            outputDevice.Init(audioFile);
            outputDevice.Play();
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

        public void Dispose() => Stop();
    }
}
