using MusicPlayer_ovh.Model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace MusicPlayer_ovh
{
    public class MusicListContext : INotifyPropertyChanged
    {

        private ObservableCollection<Song> _songs = new ObservableCollection<Song>();

        public ObservableCollection<Song> songs {
        
            get => _songs;
            set
            {
                _songs = value;
                OnPropertyChanged();
            }

        }

        public MusicListContext(string path)
        {
            if (Directory.Exists(path) == false)
            {
                songs = new ObservableCollection<Song>();
                return;
            }
            LoadSongs(path);

        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        private async void LoadSongs(string path)
        {
            try
            {
                var list = await MusicController.musicController.LoadMusicFilesAsync(path);
                if (list == null)
                {
                    songs = new ObservableCollection<Song>();
                    return;
                }
                songs = new ObservableCollection<Song>(list ?? new List<Song>());
                AppNotificationService.SendNotification($"Loaded {songs.Count} songs from {path}");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                songs = new ObservableCollection<Song>();

            }
        }
    }
}
