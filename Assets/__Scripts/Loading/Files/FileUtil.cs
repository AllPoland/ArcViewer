using System;
using System.IO;
using UnityEngine;

public class FileUtil
{
    public static byte[] StreamToBytes(Stream sourceStream)
    {
        using MemoryStream memoryStream = new MemoryStream();
        sourceStream.CopyTo(memoryStream);
        return memoryStream.ToArray();
    }


    public static Stream ReadFileData(string path)
    {
        if(!File.Exists(path))
        {
            Debug.LogWarning($"File at path {path} doesn't exist!");
            return null;
        }

        try
        {
            return File.OpenRead(path);
        }
        catch(Exception e)
        {
            Debug.LogWarning($"Unable to load file at {path} with error: {e.Message}, {e.StackTrace}");
            return null;
        }
    }
}


public sealed class TempFile : IDisposable
{
    private string path;
    public TempFile() : this(System.IO.Path.GetTempFileName()) { }

    public TempFile(string path)
    {
        if(string.IsNullOrEmpty(path)) throw new ArgumentNullException("path");
        this.path = path;
    }
    public string Path
    {
        get
        {
            if(path == null) throw new ObjectDisposedException(GetType().Name);
            return path;
        }
    }
    ~TempFile() { Dispose(false); }
    public void Dispose() { Dispose(true); }
    private void Dispose(bool disposing)
    {
        if(disposing)
        {
            GC.SuppressFinalize(this);
        }
        if(path != null)
        {
            try { File.Delete(path); }
            catch { } // best effort
            path = null;
        }
    }
}