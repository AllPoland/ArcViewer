using System;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using NVorbis;

public class AudioFileHandler
{
    public static AudioClip ClipFromOGG(Stream oggStream)
    {
        try
        {
            using(VorbisReader vorbis = new VorbisReader(oggStream, true))
            {
                //Create the audio clip where we'll write the audio data
                AudioClip newClip = AudioClip.Create("song", (int)vorbis.TotalSamples, vorbis.Channels, vorbis.SampleRate, false);

                //Buffer to extract all samples to
                float[] audioBuffer = new float[vorbis.TotalSamples * vorbis.Channels];
                vorbis.ReadSamples(audioBuffer, 0, (int)vorbis.TotalSamples * vorbis.Channels);

                //Write the audio data to the clip
                newClip.SetData(audioBuffer, 0);

                return newClip;
            }
        }
        catch(Exception e)
        {
            Debug.LogWarning($"Failed to load ogg data with error: {e.Message}, {e.StackTrace}");
            return null;
        }
    }


    public static async Task<AudioClip> ClipFromWavAsync(Stream oggStream)
    {
        await Task.Yield();

        return null;
    }
}