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
using System.Windows.Threading;

namespace MusicPlayer_ovh
{

    class AppContext: INotifyPropertyChanged
    {
        public ObservableCollection<string> ActiveNotifications { get; set; } = new ObservableCollection<string>();
        private ObservableCollection<Song> _songs = new ObservableCollection<Song>();

        public ObservableCollection<Song> songs
        {

            get => _songs;
            set
            {
                _songs = value;
                OnPropertyChanged();
            }

        }

        public AppContext(string path)
        {
            AppNotificationService.OnMessageReceived += HandleNotification;
            if (Directory.Exists(path) == false)
            {
                songs = new ObservableCollection<Song>();
                return;
            }
            LoadSongs(path);

        }

        private void HandleNotification(string message)
        {
            App.Current.Dispatcher.Invoke(async () =>
            {
                ActiveNotifications.Add(message);
                await Task.Delay(3000);
                ActiveNotifications.Remove(message);
            });
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
                AppNotificationService.SendNotification($"Loaded {songs.Count} songs");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                songs = new ObservableCollection<Song>();

            }
        }
    
    }
}
