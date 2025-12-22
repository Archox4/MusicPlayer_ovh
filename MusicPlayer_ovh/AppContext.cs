using FontAwesome.WPF;
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
        private ObservableCollection<Song> _queue = new ObservableCollection<Song>();
        private ObservableCollection<Song> _history = new ObservableCollection<Song>();

        public ObservableCollection<Song> songs
        {
            get => _songs;
            set
            {
                _songs = value;
                OnPropertyChanged();
            }
        }
        public ObservableCollection<Song> queue
        {
            get => _queue;
            set
            {
                _queue = value;
                OnPropertyChanged();
            }
        }
        public ObservableCollection<Song> history
        {
            get => _history;
            set
            {
                _history = value;
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

        public void UpdateQueue(List<Song> queue)
        {
            _queue.Clear();
            foreach (var song in queue)
            {
                _queue.Add(song);
            }
        }
        public void UpdateHistory(List<Song> history)
        {
            _history.Clear();
            foreach (var song in history)
            {
                _history.Add(song);
            }
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

        public async void LoadSongs(string path)
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
