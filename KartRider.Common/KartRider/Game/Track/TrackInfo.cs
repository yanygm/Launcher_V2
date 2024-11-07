namespace KartLibrary.Game.Track;

public class TrackInfo
{
    private string _trackID = "";

    private int _laps;

    private int _level;

    private int _difficulty;

    private string[]? _texThemes;

    private bool _isOnlyItemTrack;

    private int _f1speed;

    private bool _battle;

    private bool _choosable;

    private int _length;

    private string _bgmTheme = "";

    public string TrackID
    {
        get
        {
            return _trackID;
        }
        set
        {
            _trackID = value;
        }
    }

    public int Laps
    {
        get
        {
            return _laps;
        }
        set
        {
            _laps = value;
        }
    }

    public int Level
    {
        get
        {
            return _level;
        }
        set
        {
            _level = value;
        }
    }

    public int Difficulty
    {
        get
        {
            return _difficulty;
        }
        set
        {
            _difficulty = value;
        }
    }

    public string[]? TexThemes
    {
        get
        {
            return _texThemes;
        }
        set
        {
            _texThemes = value;
        }
    }
}