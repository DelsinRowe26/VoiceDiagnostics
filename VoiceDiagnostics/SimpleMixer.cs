﻿using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using CSCore;

namespace VoiceDiagnostics
{
    public class SimpleMixer : ISampleSource
    {
        [DllImport("winmm.dll")]
        public static extern int waveOutGetVolume(IntPtr hwo, out uint pdwVolume);

        [DllImport("winmm.dll")]
        public static extern int waveOutSetVolume(IntPtr hwo, uint dwVolume);

        const int WM_APPCOMMAND = 0x319;
        const int APPCOMMAND_VOLUME_MUTE = 0x80000;

        private readonly WaveFormat mWaveFormat;
        private readonly List<ISampleSource> mSampleSources = new List<ISampleSource>();
        private readonly object mLockObj = new object();
        private float[] mMixerBuffer;

        public bool FillWithZeros { get; set; }

        public bool DivideResult { get; set; }

        public bool Right { get; set; }

        public bool Left { get; set; }

        public SimpleMixer(int channelCount, int sampleRate)
        {
            if(channelCount < 1)
                throw new ArgumentOutOfRangeException("channelCount");
            if(sampleRate < 1)
                throw new ArgumentOutOfRangeException("sampleRate");

            mWaveFormat = new WaveFormat(sampleRate, 32, channelCount, AudioEncoding.IeeeFloat);
            FillWithZeros = false;
        }

        public void AddSource(ISampleSource source)
        {
            if (source == null)
                throw new ArgumentNullException("source");

            //if(source.WaveFormat.Channels != WaveFormat.Channels ||
            //   source.WaveFormat.SampleRate != WaveFormat.SampleRate)
            //    throw new ArgumentException("Invalid format.", "source");

            lock (mLockObj)
            {
                if (!Contains(source))
                    mSampleSources.Add(source);
            }
        }

        public void RemoveSource(ISampleSource source)//Удалить источник
        {
            //don't throw null ex here/не бросайте сюда null ex
            lock (mLockObj)
            {
                if (Contains(source))
                    mSampleSources.Remove(source);
            }
        }

        public bool Contains(ISampleSource source)//Содержит
        {
            if (source == null)
                return false;
            return mSampleSources.Contains(source);
        }

        public int Read(float[] buffer, int offset, int count)
        {
            int numberOfStoredSamples = 0;

            if (count > 0 && mSampleSources.Count > 0)
            {
                lock (mLockObj)
                {
                    mMixerBuffer = mMixerBuffer.CheckBuffer(count);
                    List<int> numberOfReadSamples = new List<int>();
                    for (int m = mSampleSources.Count -1; m >= 0; m--)
                    {
                        var sampleSource = mSampleSources[m];
                        int read = sampleSource.Read(mMixerBuffer, 0, count);
                        for (int i = offset, n = 0; n < read; i++, n++)
                        {
                            if (numberOfStoredSamples <= i)
                                buffer[i] = mMixerBuffer[n];
                            else
                                buffer[i] += mMixerBuffer[n];
                            
                        }
                        if (read > numberOfStoredSamples)
                            numberOfStoredSamples = read;

                        if (read > 0)
                            numberOfReadSamples.Add(read);
                        else
                        {
                            //raise event here/поднять событие здесь
                            RemoveSource(sampleSource); //remove the input to make sure that the event gets only raised once./удалите ввод, чтобы убедиться, что событие возникает только один раз.
                        }
                    }
                    if (Right)
                    {
                        uint volume;
                        waveOutGetVolume(IntPtr.Zero, out volume);
                        int rightt = (int)((volume >> 16) & 0xFFFF);
                        rightt = 65535;
                        uint vol = (uint)(rightt << 16);
                        waveOutSetVolume(IntPtr.Zero, vol);
                    }
                    if (Left)
                    {
                        uint volume;
                        waveOutGetVolume(IntPtr.Zero, out volume);
                        int left = (int)(volume & 0xFFFF);
                        left = 65535;
                        uint vol = (uint)(left);
                        waveOutSetVolume(IntPtr.Zero, vol);
                    }

                    if (DivideResult)
                    {
                        numberOfReadSamples.Sort();//количество прочитанных образцов
                        int currentOffset = offset;//текущее смещение 
                        int remainingSources = numberOfReadSamples.Count;//остальные источники 

                        foreach (var readSamples in numberOfReadSamples)
                        {
                            if (remainingSources == 0)
                                break;

                            while (currentOffset < offset + readSamples)
                            {
                                buffer[currentOffset] /= remainingSources;
                                buffer[currentOffset] = Math.Max(-1, Math.Min(1, buffer[currentOffset]));
                                currentOffset++;
                            }
                            remainingSources--;
                        }
                    }
                }
            }

            if (FillWithZeros && numberOfStoredSamples != count)
            {
                Array.Clear(
                    buffer,
                    Math.Max(offset + numberOfStoredSamples - 1, 0),
                    count - numberOfStoredSamples);

                return count;
            }

            return numberOfStoredSamples;
        }

        public bool CanSeek { get { return false; } }

        public WaveFormat WaveFormat
        {
            get { return mWaveFormat; }
        }

        public long Position
        {
            get { return 0; }
            set
            {
                throw new NotSupportedException();
            }
        }

        public long Length
        {
            get { return 0; }
        }

        public void Dispose()
        {
            lock (mLockObj)
            {
                foreach (var sampleSource in mSampleSources.ToArray())
                {
                    sampleSource.Dispose();
                    mSampleSources.Remove(sampleSource);
                }
            }
        }
    }
}
