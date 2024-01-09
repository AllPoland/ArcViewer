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
        if(BeatmapManager.CurrentDifficulty?.beatmapDifficulty?.customData?.bookmarks == null)
        {
            return;
        }

        BeatmapCustomBookmark[] newBookmarks = BeatmapManager.CurrentDifficulty.beatmapDifficulty.customData.bookmarks;
        foreach(BeatmapCustomBookmark bookmark in newBookmarks)
        {
            BookmarkIcon newBookmark = Instantiate(bookmarkPrefab, bookmarkParent, false);
            newBookmark.SetParentReferences(bookmarkParent, parentCanvas);
            newBookmark.SetData(
                bookmark.b,
                bookmark.n,
                bookmark.c != null ? new Color(bookmark.c[0], bookmark.c[1], bookmark.c[2]) : Color.white
            );

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


    private void OnEnable()
    {
        if(!parentCanvas) 
        {
            parentCanvas = GetComponentInParent<Canvas>();
        }
        BeatmapManager.OnBeatmapDifficultyChanged += UpdateDifficulty;
    }


    private void OnDisable()
    {
        ClearBookmarks();
    }
}