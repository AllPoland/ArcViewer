using System;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using NVorbis;

public class AudioFileHandler
{
    //Higher values theoretically decrease load time, at the cost of stuttering and more memory usage
    public const int bufferSize = 100000;

    //Max amount of memory allocation in kilobytes
    //Avoids trying to load songs that are too long to fit in memory as a float[]
    //It really really sucks that this is an issue that needs to be avoided
    //This could easily be made entirely moot if WebGL wasn't fucking stupid and I could use the offsetSamples parameter
    //Or this entire loading process could be circumvented by figuring out how to use a tempfile in WebGL
    //For now though, this is the only option I have, hence this horrible situation existing
    public const int maxMemory = 1000000;


    public static async Task<AudioClip> ClipFromOGGAsync(Stream oggStream)
    {
        VorbisReader vorbis = new VorbisReader(oggStream);
        AudioClip newClip = null;

        try
        {
            int totalSamples = (int)vorbis.TotalSamples * vorbis.Channels;

            //Avoid everything blowing up when the operation will take too much memory
            int memoryUse = sizeof(float) * (totalSamples / 1000);
            if(memoryUse > maxMemory || memoryUse < 0)
            {
                Debug.LogWarning($"Song loading would use too much memory! {memoryUse / 1000}MB");
                ErrorHandler.Instance.DisplayPopup(ErrorType.Error, "Song is too long!");

                return null;
            }
            Debug.Log(memoryUse);

            //Create the audio clip where we'll write the audio data
            newClip = AudioClip.Create("song", (int)vorbis.TotalSamples, vorbis.Channels, vorbis.SampleRate, false);

            float[] samples = new float[totalSamples];

            vorbis.SamplePosition = 0;
            MapLoader.Progress = 0;

            for(int i = 0; i <= totalSamples; i += bufferSize)
            {
                //Avoids trying to read samples past the end of the song
                int readSamples = Mathf.Min(bufferSize, totalSamples - i);

                //Read only a few samples per loop before yielding
                //This is such a yucky and slow method but it's the best I can come up with for now
                vorbis.ReadSamples(samples, i, readSamples);

                MapLoader.Progress = (float)i / totalSamples;

                await Task.Yield();
            }
            MapLoader.Progress = 0;

            newClip.SetData(samples, 0);
            vorbis.Dispose();

            return newClip;
        }
        catch(Exception e)
        {
            Debug.LogWarning($"Failed to load ogg data with error: {e.Message}, {e.StackTrace}");
            ErrorHandler.Instance.DisplayPopup(ErrorType.Error, "Unable to load audio file!");

            vorbis.Dispose();
            if(newClip != null)
            {
                newClip.UnloadAudioData();
                GameObject.Destroy(newClip);
            }

            return null;
        }
    }


    public static async Task<AudioClip> ClipFromWavAsync(Stream wavStream)
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

            //Avoid everything blowing up when the operation will take too much memory
            int memoryUse = sizeof(float) * (sampleCount / 1000);
            if((ulong)memoryUse > maxMemory)
            {
                Debug.LogWarning($"Song loading would use too much memory! {memoryUse / 1000}MB");
                ErrorHandler.Instance.DisplayPopup(ErrorType.Error, "Song is too long!");

                return null;
            }

            //Create the new audio clip
            AudioClip newClip = AudioClip.Create("song", sampleCount / channels, channels, sampleRate, false);

            float[] samples = new float[sampleCount];
            float[] sampleBuffer = new float[bufferSize];

            MapLoader.Progress = 0;

            for(int i = 0; i <= sampleCount; i += bufferSize)
            {
                //Avoids trying to read samples past the end of the song
                int readSamples = Mathf.Min(bufferSize, sampleCount - i);
                if(readSamples != sampleBuffer.Length)
                {
                    sampleBuffer = new float[readSamples];
                }

                //Read the audio data
                int readBytes = sampleBuffer.Length * bytesPerSample;
                byte[] audioData = reader.ReadBytes(readBytes);

                //Convert audio data to floats
                switch(bitDepth)
                {
                    case 64:
                        double[] doubleSamples = new double[sampleBuffer.Length];
                        Buffer.BlockCopy(audioData, 0, doubleSamples, 0, readBytes);
                        sampleBuffer = Array.ConvertAll(doubleSamples, e => (float)e);
                        break;
                    case 32:
                        sampleBuffer = new float[sampleBuffer.Length];
                        Buffer.BlockCopy(audioData, 0, sampleBuffer, 0, readBytes);
                        break;
                    case 16:
                        short[] shortSamples = new short[sampleBuffer.Length];
                        Buffer.BlockCopy(audioData, 0, shortSamples, 0, readBytes);
                        sampleBuffer = Array.ConvertAll(shortSamples, e => e / (float)(short.MaxValue + 1));
                        break;
                    default:
                        Debug.LogWarning("Unable to read bit depth from wav!");
                        ErrorHandler.Instance.DisplayPopup(ErrorType.Error, "Unable to load audio file!");
                        return null;
                }

                Array.Copy(sampleBuffer, 0, samples, i, sampleBuffer.Length);
                MapLoader.Progress = (float)i / sampleCount;

                await Task.Yield();
            }
            MapLoader.Progress = 0;

            newClip.SetData(samples, 0);

            samples = null;
            sampleBuffer = null;

            reader.Dispose();

            return newClip;
        }
        catch(Exception e)
        {
            Debug.LogWarning($"Failed to load wav data with error: {e.Message}, {e.StackTrace}");
            ErrorHandler.Instance.DisplayPopup(ErrorType.Error, "Unable to load audio file!");

            reader.Dispose();

            return null;
        }
    }
}