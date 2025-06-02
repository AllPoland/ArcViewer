using System.Collections.Generic;
using UnityEngine;

public class BookmarkHandler : MonoBehaviour
{
    [SerializeField] private BookmarkIcon bookmarkPrefab;
    [SerializeField] private RectTransform bookmarkParent;

    private List<BookmarkIcon> bookmarks = new List<BookmarkIcon>();
    private Canvas parentCanvas;


    private void GenerateBookmarks()
    {
        ClearBookmarks();
        if(!SettingsManager.GetBool("showbookmarks"))
        {
            return;
        }

        List<Bookmark> newBookmarks = BeatmapManager.CurrentDifficulty?.beatmapDifficulty?.bookmarks;
        if(newBookmarks == null)
        {
            return;
        }

        foreach(Bookmark bookmark in newBookmarks)
        {
            BookmarkIcon newBookmark = Instantiate(bookmarkPrefab, bookmarkParent, false);
            newBookmark.SetParentReferences(bookmarkParent, parentCanvas);
            newBookmark.SetData(bookmark.Beat, bookmark.Label, bookmark.Color);

            bookmarks.Add(newBookmark);
        }
    }


    private void ClearBookmarks()
    {
        for(int i = bookmarks.Count - 1; i >= 0; i--)
        {
            bookmarks[i].gameObject.SetActive(false);
            Destroy(bookmarks[i].gameObject);
            bookmarks.Remove(bookmarks[i]);
        }
    }


    private void UpdateDifficulty(Difficulty newDifficulty) => GenerateBookmarks();


    private void UpdateSettings(string setting)
    {
        if(setting == "all" || setting == "showbookmarks")
        {
            GenerateBookmarks();
        }
    }


    private void OnEnable()
    {
        if(!parentCanvas) 
        {
            parentCanvas = GetComponentInParent<Canvas>();
        }

        TimeManager.OnDifficultyBpmEventsLoaded += UpdateDifficulty;
        SettingsManager.OnSettingsUpdated += UpdateSettings;
    }


    private void OnDisable()
    {
        ClearBookmarks();
    }
}