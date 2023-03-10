#if UNITY_WEBGL && !UNITY_EDITOR
using System;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using NVorbis;

public class AudioFileHandler
{
    private static AudioUploadState uploadState;


    public static async Task<WebAudioClip> ClipFromOGGAsync(MemoryStream oggStream)
    {
        VorbisReader vorbis = new VorbisReader(oggStream, false);
        WebAudioClip newClip = null;

        try
        {
            //Create the audio clip where we'll write the audio data
            newClip = new WebAudioClip((int)vorbis.TotalSamples, vorbis.Channels, vorbis.SampleRate);
            int sampleRate = vorbis.SampleRate;

            byte[] data = oggStream.ToArray();
            newClip.SetData(data, sampleRate, ClipUploadResultCallback);
            uploadState = AudioUploadState.uploading;

            while(uploadState == AudioUploadState.uploading)
            {
                await Task.Yield();
            }
            vorbis.Dispose();

            if(uploadState == AudioUploadState.error)
            {
                //An error occurred in the upload
                newClip.Dispose();
                return null;
            }

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


    public static async Task<WebAudioClip> ClipFromWavAsync(MemoryStream wavStream)
    {
        //Read wav details using a binary reader
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

            byte[] data = wavStream.ToArray();
            newClip.SetData(data, sampleRate, ClipUploadResultCallback);
            uploadState = AudioUploadState.uploading;

            while(uploadState == AudioUploadState.uploading)
            {
                await Task.Yield();
            }
            reader.Dispose();

            if(uploadState == AudioUploadState.error)
            {
                //An error occurred in the upload
                newClip.Dispose();
                return null;
            }

            return newClip;
        }
        catch(Exception e)
        {
            Debug.LogWarning($"Failed to load wav data with error: {e.Message}, {e.StackTrace}");
            reader.Dispose();

            return null;
        }
    }



    public static void ClipUploadResultCallback(int result)
    {
        if(result < 1)
        {
            //A result below 1 means setting data failed
            ErrorHandler.Instance.DisplayPopup(ErrorType.Error, "Failed to load audio file!");
            uploadState = AudioUploadState.error;
        }
        else
        {
            uploadState = AudioUploadState.success;
        }
    }


    public enum AudioUploadState
    {
        inactive,
        uploading,
        success,
        error
    }
}
#endif