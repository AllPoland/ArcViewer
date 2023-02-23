using System;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using NVorbis;

public class AudioFileHandler
{
    public const int baseBufferSize = 100000;

#if !UNITY_WEBGL || UNITY_EDITOR
    //Target using 1/60th of a second when loading
    public const double targetTime = 0.01666f;
#else
    //Target 1/30th on webgl (targetting a lower framerate theoretically reduces load times (not really))
    public const double targetTime = 0.03333f;
#endif

    private static System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();


    public static async Task<WebAudioClip> ClipFromOGGAsync(Stream oggStream)
    {
        VorbisReader vorbis = new VorbisReader(oggStream);
        WebAudioClip newClip = null;

        try
        {
            //Create the audio clip where we'll write the audio data
            newClip = new WebAudioClip((int)vorbis.TotalSamples, vorbis.Channels, vorbis.SampleRate);
            int totalSamples = (int)vorbis.TotalSamples * vorbis.Channels;

            vorbis.SamplePosition = 0;
            MapLoader.Progress = 0;

            int readSamples = Mathf.Min(baseBufferSize, totalSamples);

            int i = 0;
            while(i < totalSamples)
            {
                stopwatch.Reset();
                stopwatch.Start();

                float[] buffer = new float[readSamples];

                //Read only a few samples per loop before yielding
                //This is such a yucky and slow method but it's the best I can come up with for now
                vorbis.ReadSamples(buffer, 0, readSamples);
                newClip.SetData(buffer, i / vorbis.Channels);

                MapLoader.Progress = (float)i / totalSamples;
                i += readSamples;

                stopwatch.Stop();

                //Adjust how many samples to read based on time to run
                double elapsedTime = (double)stopwatch.ElapsedMilliseconds / 1000;
                double timeRatio = targetTime / elapsedTime;
                int targetSamples = (int)((double)readSamples * timeRatio);
                //Avoid trying to read samples past the end of the song
                readSamples = Mathf.Min(targetSamples, totalSamples - i);

                await Task.Yield();
            }
            MapLoader.Progress = 0;
            vorbis.Dispose();

            return newClip;
        }
        catch(Exception e)
        {
            Debug.LogWarning($"Failed to load ogg data with error: {e.Message}, {e.StackTrace}");

            vorbis.Dispose();
            if(newClip != null)
            {
                newClip.Dispose();
            }

            return null;
        }
    }


    public static async Task<WebAudioClip> ClipFromWavAsync(Stream wavStream)
    {
        //Read all wav data using a binary reader
        BinaryReader reader = new BinaryReader(wavStream);

        try
        {
            //Chunk 0
            int chinkID = reader.ReadInt32();
            int fileSize = reader.ReadInt32();
            int riffType = reader.ReadInt32();

            //Chunk 1
            int fmtID = reader.ReadInt32();
            int fmtSize = reader.ReadInt32();
            int fmtCode = reader.ReadInt16();

            int channels = reader.ReadInt16();
            int sampleRate = reader.ReadInt32();
            int byteRate = reader.ReadInt32();
            int fmtBlockAlign = reader.ReadInt16();
            int bitDepth = reader.ReadInt16();

            if(fmtSize == 18)
            {
                //Read extra data
                int fmtExtraSize = reader.ReadInt16();
                reader.ReadBytes(fmtExtraSize);
            }

            //Chunk 2
            int dataID = reader.ReadInt32();
            int audioBytes = reader.ReadInt32();

            int bytesPerSample = bitDepth / 8;
            int sampleCount = audioBytes / bytesPerSample;

            //Create the new audio clip
            WebAudioClip newClip = new WebAudioClip(sampleCount / channels, channels, sampleRate);

            MapLoader.Progress = 0;
            int readSamples = Mathf.Min(baseBufferSize, sampleCount);

            int i = 0;
            while(i < sampleCount)
            {
                stopwatch.Reset();
                stopwatch.Start();

                float[] buffer = new float[readSamples];

                //Read the audio data
                int readBytes = buffer.Length * bytesPerSample;
                byte[] audioData = reader.ReadBytes(readBytes);

                //Convert audio data to floats
                switch(bitDepth)
                {
                    case 64:
                        double[] doubleSamples = new double[readBytes];
                        Buffer.BlockCopy(audioData, 0, doubleSamples, 0, readBytes);
                        buffer = Array.ConvertAll(doubleSamples, e => (float)e);
                        break;
                    case 32:
                        buffer = new float[readBytes];
                        Buffer.BlockCopy(audioData, 0, buffer, 0, readBytes);
                        break;
                    case 16:
                        short[] shortSamples = new short[readBytes];
                        Buffer.BlockCopy(audioData, 0, shortSamples, 0, readBytes);
                        buffer = Array.ConvertAll(shortSamples, e => e / (float)(short.MaxValue + 1));
                        break;
                    default:
                        Debug.LogWarning("Unable to read bit depth from wav!");
                        return null;
                }
                newClip.SetData(buffer, i / channels);

                MapLoader.Progress = (float)i / sampleCount;
                i += readSamples;

                stopwatch.Stop();

                //Adjust how many samples to read based on time to run
                double elapsedTime = (double)stopwatch.ElapsedMilliseconds / 1000;
                double timeRatio = targetTime / elapsedTime;
                int targetSamples = (int)((double)readSamples * timeRatio);
                //Avoid trying to read samples past the end of the song
                readSamples = Mathf.Min(targetSamples, sampleCount - i);

                await Task.Yield();
            }
            MapLoader.Progress = 0;

            reader.Dispose();

            return newClip;
        }
        catch(Exception e)
        {
            Debug.LogWarning($"Failed to load wav data with error: {e.Message}, {e.StackTrace}");
            reader.Dispose();

            return null;
        }
    }
}