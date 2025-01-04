using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KartLibrary.Game.Track
{
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
            get => _trackID;
            set => _trackID = value;
        }

        public int Laps
        {
            get => _laps;
            set => _laps = value;
        }

        public int Level
        {
            get => _level;
            set => _level = value;
        }

        public int Difficulty
        {
            get => _difficulty; 
            set => _difficulty = value;
        }

        public string[]? TexThemes
        {
            get => _texThemes;
            set => _texThemes = value;
        }
    }
}
