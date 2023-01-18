using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

public class ZipReader
{
    public static async Task<(BeatmapInfo, List<Difficulty>, TempFile)> GetMapFromZipPathAsync(string zipPath)
    {
        try
        {
            using(ZipArchive archive = await Task.Run(() => ZipFile.OpenRead(zipPath)))
            {
                return await GetMapFromZipArchiveAsync(archive);
            }
        }
        catch(Exception err)
        {
            Debug.LogWarning($"Unhandled exception loading zip: {err.Message}, {err.StackTrace}.");
            return(null, new List<Difficulty>(), null);
        }
    }


    public static async Task<(BeatmapInfo, List<Difficulty>, TempFile)> GetMapFromZipArchiveAsync(ZipArchive archive)
    {
        BeatmapInfo info = null;
        List<Difficulty> difficulties = new List<Difficulty>();
        TempFile songFile = null;

        Stream infoData = null;
        foreach(ZipArchiveEntry entry in archive.Entries)
        {
            if(entry.FullName.Equals("Info.dat", System.StringComparison.OrdinalIgnoreCase))
            {
                infoData = entry.Open();
                break;
            }
        }

        if(infoData == null)
        {
            Debug.LogWarning("Zip file has no Info.dat!");
            return (info, difficulties, songFile);
        }

        string infoJson = System.Text.Encoding.ASCII.GetString(StreamToBytes(infoData));
        info = JsonUtility.FromJson<BeatmapInfo>(infoJson);

        if(info._difficultyBeatmapSets.Length < 1)
        {
            Debug.LogWarning("Info lists no difficulties!");
        }

        foreach(DifficultyBeatmapSet set in info._difficultyBeatmapSets)
        {
            if(set._difficultyBeatmaps.Length == 0)
            {
                continue;
            }

            string characteristicName = set._beatmapCharacteristicName;
            DifficultyCharacteristic setCharacteristic = BeatmapInfo.CharacteristicFromString(characteristicName);

            List<Difficulty> newDifficulties = new List<Difficulty>();
            foreach(DifficultyBeatmap beatmap in set._difficultyBeatmaps)
            {
                Debug.Log($"Loading {beatmap._beatmapFilename}");
                Difficulty diff = await Task.Run(() => GetDiff(beatmap, archive));
                
                diff.characteristic = setCharacteristic;
                newDifficulties.Add(diff);
            }

            difficulties.AddRange(newDifficulties);
            Debug.Log($"Finished loading {newDifficulties.Count} difficulties in characteristic {characteristicName}.");
        }

        string songFilename = info._songFilename;

        foreach(ZipArchiveEntry entry in archive.Entries)
        {
            if(entry.FullName.Equals(songFilename))
            {
                songFile = await GetAudioFileFromEntryAsync(entry);
            }
        }

        if(songFile == null)
        {
            Debug.LogWarning($"Didn't find audio file {songFilename}!");
        }

        return (info, difficulties, songFile);
    }


    private static Difficulty GetDiff(DifficultyBeatmap beatmap, ZipArchive archive)
    {
        Difficulty output = new Difficulty
        {
            difficultyRank = BeatmapLoader.DiffValueFromString[beatmap._difficulty],
            NoteJumpSpeed = beatmap._noteJumpMovementSpeed,
            SpawnOffset = beatmap._noteJumpStartBeatOffset
        };

        string filename = beatmap._beatmapFilename;
        Debug.Log($"Loading json from {filename}");

        Stream diffData = null;
        foreach(ZipArchiveEntry entry in archive.Entries)
        {
            if(entry.FullName.Equals(filename))
            {
                diffData = entry.Open();
                break;
            }
        }

        if(diffData == null)
        {
            Debug.LogWarning("Difficulty file not found!");
            return Difficulty.Empty;
        }

        Debug.Log($"Parsing {filename}");
        string diffJson = System.Text.Encoding.ASCII.GetString(StreamToBytes(diffData));
        output.beatmapDifficulty = JsonReader.ParseBeatmapFromJson(diffJson);

        return output;
    }


    private static async Task<TempFile> GetAudioFileFromEntryAsync(ZipArchiveEntry entry)
    {
        TempFile tempFile = new TempFile();
        await Task.Run(() => entry.ExtractToFile(tempFile.Path, true));

        return tempFile;
    }


    public static byte[] StreamToBytes(Stream sourceStream)
    {
        using(var memoryStream = new MemoryStream())
        {
            sourceStream.CopyTo(memoryStream);
            return memoryStream.ToArray();
        }
    }
}