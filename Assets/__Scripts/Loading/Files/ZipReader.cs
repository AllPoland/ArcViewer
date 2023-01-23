using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using UnityEngine;

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
            ErrorHandler.Instance?.QueuePopup(ErrorType.Error, "Unable to load Info.dat!");
            Debug.LogWarning("Zip file has no Info.dat!");
            return (info, difficulties, songFile);
        }

        string infoJson = System.Text.Encoding.ASCII.GetString(StreamToBytes(infoData));
        info = JsonUtility.FromJson<BeatmapInfo>(infoJson);

        if(info._difficultyBeatmapSets.Length < 1)
        {
            ErrorHandler.Instance?.QueuePopup(ErrorType.Warning, "The map lists no difficulty sets!");
            Debug.LogWarning("Info lists no difficulty sets!");
        }

        foreach(DifficultyBeatmapSet set in info._difficultyBeatmapSets)
        {
            string characteristicName = set._beatmapCharacteristicName;
            
            if(set._difficultyBeatmaps.Length == 0)
            {
                ErrorHandler.Instance?.QueuePopup(ErrorType.Warning, $"Characteristic {characteristicName} lists no difficulties!");
                Debug.LogWarning($"{characteristicName} lists no difficulties!");
                continue;
            }

            DifficultyCharacteristic setCharacteristic = BeatmapInfo.CharacteristicFromString(characteristicName);

            List<Difficulty> newDifficulties = new List<Difficulty>();
            foreach(DifficultyBeatmap beatmap in set._difficultyBeatmaps)
            {
                Debug.Log($"Loading {beatmap._beatmapFilename}");
                Difficulty diff = await Task.Run(() => GetDiff(beatmap, archive));
                if(diff == null)
                {
                    continue;
                }
                
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
            ErrorHandler.Instance?.QueuePopup(ErrorType.Error, "Song file not found!");
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
            ErrorHandler.Instance?.QueuePopup(ErrorType.Warning, $"Unable to load {filename}!");
            Debug.LogWarning("Difficulty file not found!");
            return null;
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