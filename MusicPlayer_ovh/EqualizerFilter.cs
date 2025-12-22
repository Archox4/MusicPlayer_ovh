using NAudio.Dsp;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace MusicPlayer_ovh
{
    public class EqualizerFilter: ISampleProvider
    {
        private readonly ISampleProvider sampleProvider;
        private readonly BiQuadFilter[] filters;
        private readonly float[] gains; // in dB
        public WaveFormat WaveFormat => sampleProvider.WaveFormat;

        public EqualizerFilter(ISampleProvider sourceProvider, float[] frequencies)
        {
            this.sampleProvider = sourceProvider;
            this.filters = new BiQuadFilter[frequencies.Length * sourceProvider.WaveFormat.Channels]; // number of frequencies times number of channels (mostly 2)
            this.gains = new float[frequencies.Length];

            for (int i = 0; i < frequencies.Length; i++)
            {
                UpdateFilter(i, frequencies[i], 0);
            }
            
        }
        public void UpdateFilter(int bandIndex, float frequency, float dbGain)
        {
            gains[bandIndex] = dbGain;
            for (int n = 0; n < sampleProvider.WaveFormat.Channels; n++)
            {
                filters[bandIndex * sampleProvider.WaveFormat.Channels + n] =
                    BiQuadFilter.PeakingEQ(sampleProvider.WaveFormat.SampleRate, frequency, 0.8f, dbGain);
            }
        }
        public int Read(float[] buffer, int offset, int count)
        {
            int samplesRead = sampleProvider.Read(buffer, offset, count);
            int channels = WaveFormat.Channels;

            for (int i = 0; i < samplesRead; i++)
            {
                // Identify if this specific sample is Left (0) or Right (1)
                int channel = i % channels;

                // Only apply filters that belong to THIS channel
                for (int band = 0; band < gains.Length; band++)
                {
                    int filterIndex = (band * channels) + channel;
                    buffer[offset + i] = filters[filterIndex].Transform(buffer[offset + i]);
                }
            }
            return samplesRead;
        }



    }
}
