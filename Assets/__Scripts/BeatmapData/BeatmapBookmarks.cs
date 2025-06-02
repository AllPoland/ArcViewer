using System;
using UnityEngine;

[Serializable]
public class BeatmapBookmarkSet
{
    public string name;
    public string characteristic;
    public string difficulty;
    public string color;
    public BeatmapBookmark[] bookmarks;
}


[Serializable]
public class BeatmapBookmark
{
    public float beat;
    public string label;
    public string text;
}


public class Bookmark
{
    public float Beat;
    public Color Color;
    public string Label;
    public string Text;
}