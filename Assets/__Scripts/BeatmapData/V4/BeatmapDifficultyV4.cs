using System;

[Serializable]
public class BeatmapDifficultyV4
{
    public string version;

    public BeatmapColorNoteV4[] colorNotes;
    public BeatmapBombNoteV4[] bombNotes;
    public BeatmapObstacleV4[] obstacles;
    public BeatmapArcV4[] arcs;
    public BeatmapChainV4[] chains;

    public BeatmapColorNoteDataV4[] colorNotesData;
    public BeatmapBombNoteDataV4[] bombNotesData;
    public BeatmapObstacleDataV4[] obstaclesData;
    public BeatmapArcDataV4[] arcsData;
    public BeatmapChainDataV4[] chainsData;

    public BeatmapElementV4[] spawnRotations;
    public BeatmapElementV4[] njsEvents;

    public BeatmapSpawnRotationDataV4[] spawnRotationsData;
    public BeatmapNjsEventDataV4[] njsEventData;

    public bool HasObjects => colorNotes.Length + bombNotes.Length
        + obstacles.Length + arcs.Length
        + chains.Length + spawnRotations.Length > 0;


    public BeatmapDifficultyV4()
    {
        version = "4.0.0";
        colorNotes = new BeatmapColorNoteV4[0];
        bombNotes = new BeatmapBombNoteV4[0];
        obstacles = new BeatmapObstacleV4[0];
        arcs = new BeatmapArcV4[0];
        chains = new BeatmapChainV4[0];
        colorNotesData = new BeatmapColorNoteDataV4[0];
        bombNotesData = new BeatmapBombNoteDataV4[0];
        obstaclesData = new BeatmapObstacleDataV4[0];
        arcsData = new BeatmapArcDataV4[0];
        chainsData = new BeatmapChainDataV4[0];
        spawnRotations = new BeatmapElementV4[0];
        spawnRotationsData = new BeatmapSpawnRotationDataV4[0];
    }


    public void AddNulls()
    {
        version ??= "4.0.0";
        colorNotes ??= new BeatmapColorNoteV4[0];
        bombNotes ??= new BeatmapBombNoteV4[0];
        obstacles ??= new BeatmapObstacleV4[0];
        arcs ??= new BeatmapArcV4[0];
        chains ??= new BeatmapChainV4[0];
        colorNotesData ??= new BeatmapColorNoteDataV4[0];
        bombNotesData ??= new BeatmapBombNoteDataV4[0];
        obstaclesData ??= new BeatmapObstacleDataV4[0];
        arcsData ??= new BeatmapArcDataV4[0];
        chainsData ??= new BeatmapChainDataV4[0];
        njsEvents ??= new BeatmapElementV4[0];
        spawnRotations ??= new BeatmapElementV4[0];
        njsEventData ??= new BeatmapNjsEventDataV4[0];
        spawnRotationsData ??= new BeatmapSpawnRotationDataV4[0];
    }
}


[Serializable]
public class BeatmapElementV4
{
    public float b;
    public int i;
}


[Serializable]
public class BeatmapColorNoteV4 : BeatmapElementV4
{
    public int r;
}


[Serializable]
public struct BeatmapColorNoteDataV4
{
    public int x;
    public int y;
    public int c;
    public int d;
    public int a;
}


[Serializable]
public class BeatmapBombNoteV4 : BeatmapElementV4
{
    public int r;
}


[Serializable]
public struct BeatmapBombNoteDataV4
{
    public int x;
    public int y;
}


[Serializable]
public class BeatmapObstacleV4 : BeatmapElementV4
{
    public int r;
}


[Serializable]
public struct BeatmapObstacleDataV4
{
    public float d;
    public int x;
    public int y;
    public int w;
    public int h;
}


[Serializable]
public class BeatmapArcV4
{
    public float hb;
    public float tb;
    public int hr;
    public int tr;
    public int hi;
    public int ti;
    public int ai;
}


[Serializable]
public struct BeatmapArcDataV4
{
    public float m;
    public float tm;
    public int a;
}


[Serializable]
public class BeatmapChainV4
{
    public float hb;
    public float tb;
    public int hr;
    public int tr;
    public int i;
    public int ci;
}


[Serializable]
public struct BeatmapChainDataV4
{
    public int tx;
    public int ty;
    public int c;
    public float s;
}


[Serializable]
public class BeatmapSpawnRotationDataV4
{
    public int t;
    public int r;
}


[Serializable]
public class BeatmapNjsEventDataV4
{
    public int p;
    public int e;
    public float d;
}