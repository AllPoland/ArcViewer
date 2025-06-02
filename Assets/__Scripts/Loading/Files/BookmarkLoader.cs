using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class BookmarkLoader
{
    private static Bookmark ConvertBeatmapBookmark(BeatmapBookmark beatmapBookmark, Color color)
    {
        return new Bookmark
        {
            Beat = beatmapBookmark.beat,
            Label = beatmapBookmark.label,
            Text = beatmapBookmark.text,
            Color = color
        };
    }


    private static void AddBookmarksFromCustomData(BeatmapDifficulty difficulty)
    {
        if(difficulty?.CustomData?.bookmarks == null)
        {
            return;
        }

        //Convert custom bookmarks to 
        List<Bookmark> newBookmarks = new List<Bookmark>();
        foreach(BeatmapCustomBookmark customBookmark in difficulty.CustomData.bookmarks)
        {
            Color color = customBookmark.c != null && customBookmark.c.Length >= 3
                ? new Color(customBookmark.c[0], customBookmark.c[1], customBookmark.c[2])
                : Color.white;
            newBookmarks.Add(new Bookmark
            {
                Beat = customBookmark.b,
                Color = color,
                Label = customBookmark.n
            });
        }

        //Add the newly parsed bookmarks to the difficulty
        difficulty.bookmarks.AddRange(newBookmarks);
    }


    private static void AddBookmarksFromBookmarkSet(BeatmapDifficulty difficulty, BeatmapBookmarkSet bookmarkSet)
    {
        string colorString = bookmarkSet.color;
        if(!colorString.StartsWith('#'))
        {
            colorString = '#' + colorString;
        }

        Color color;
        if(!ColorUtility.TryParseHtmlString(colorString, out color))
        {
            color = Color.white;
        }

        foreach(BeatmapBookmark bookmark in bookmarkSet.bookmarks)
        {
            difficulty.bookmarks.Add(ConvertBeatmapBookmark(bookmark, color));
        }
    }


    public static void ApplyBookmarks(LoadedMapData mapData, List<BeatmapBookmarkSet> bookmarkSets)
    {
        //Add bookmarks from official bookmark files
        foreach(BeatmapBookmarkSet bookmarkSet in bookmarkSets)
        {
            DifficultyCharacteristic targetCharacteristic = BeatmapInfo.CharacteristicFromString(bookmarkSet.characteristic);
            DifficultyRank targetRank = BeatmapInfo.DifficultyRankFromString(bookmarkSet.difficulty);

            Difficulty targetDifficulty = mapData.Difficulties.FirstOrDefault(x => x.characteristic == targetCharacteristic && x.difficultyRank == targetRank);
            if(targetDifficulty == null)
            {
                //No matching difficulty for this bookmark file
                continue;
            }

            AddBookmarksFromBookmarkSet(targetDifficulty.beatmapDifficulty, bookmarkSet);
        }

        foreach(Difficulty difficulty in mapData.Difficulties)
        {
            //Include custom bookmarks if they're present
            BeatmapDifficulty beatmapDifficulty = difficulty.beatmapDifficulty;
            AddBookmarksFromCustomData(beatmapDifficulty);

            //Sort bookmarks by beat
            beatmapDifficulty.bookmarks = beatmapDifficulty.bookmarks.OrderBy(x => x.Beat).ToList();
        }
    }
}