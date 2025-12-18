using FontAwesome.WPF;
using MusicPlayer_ovh.Model;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;



namespace MusicPlayer_ovh
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    /// 
    public class IndexConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (int)value + 1;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }


    public partial class MainWindow : Window
    {

        private ObservableCollection<Song> songs = new ObservableCollection<Song>();
        private ObservableCollection<Song> queue = new ObservableCollection<Song>();
        private ObservableCollection<Song> history = new ObservableCollection<Song>();

        private AudioPlayer Player = new AudioPlayer();
        private string _STATE;
        private Song? playingSong;
        private List<Song>? songHistory;
        private List<Song>? songListRandom;
        private List<Song>? songQueue;
        private Modes _PLAYMODE;
        int randomPos = -1;
        int historyPos = 0;
        int lastPos = -1;


        public MainWindow()
        {
            InitializeComponent();

            //this.DataContext = new MusicListContext("C:\\Users\\w\\Music");
            this.DataContext = new AppContext("C:\\Users\\w\\Music");
            songs = ((AppContext)this.DataContext).songs;
            queue = ((AppContext)this.DataContext).queue;
            history = ((AppContext)this.DataContext).history;
            

            _STATE = "paused";
            _PLAYMODE = Modes.Normal;
            PlayButtonIcon.Icon = FontAwesomeIcon.Play;
            ModeIcon.Icon = FontAwesomeIcon.List;
            sidePanel.SelectedIndex = 0;

            songHistory = new ();
            songQueue = new ();
            songListRandom = new ();

        }

        protected void HandleDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var song = ((ListViewItem)sender).Content as Song;
            if (song != null)
            {
                Player.Play(song.path);
                AddHistory(song);

                _STATE = "playing";
                playingSong = song;
                TogglePlay();
                lastPos = getSongPosition(song.path);
                //PlayFile(song.path);
                //playingSong = song;

                //lastPos = getSongPosition(song.path);

                
                UpdateUI();
            }
        }
        private void Song_Previous(object sender, RoutedEventArgs e)
        {
            if(songHistory == null)
            {
                return;
            }
            if (songHistory.Count > 0)
            {
                historyPos = songHistory.Count - 1;
                Song currSong = songHistory.ElementAt(historyPos);
                Player.Play(currSong.path);
                playingSong = currSong;
                songHistory.RemoveAt(historyPos);
                ((AppContext)this.DataContext).UpdateHistory(songHistory);
                UpdateUI();
            }
        }
        private void Song_Next(object sender, RoutedEventArgs e)
        {
            playNextSong();
        }
        private void Song_PlayPause(object sender, RoutedEventArgs e)
        {
            var _songs = ((AppContext)this.DataContext).songs;
            if (_STATE == "playing")
            {
                Player.Pause();
                _STATE = "paused";
            }
            else if (_STATE == "paused" && playingSong != null)
            {
                Player.Resume();
                _STATE = "playing";
            }
            else if (_STATE == "paused" && playingSong == null && _songs.Count > 0)
            {
                //Player.Play(_songs[0].path);
                //playingSong = _songs[0];
                //AddHistory(playingSong);
                playNextSong();
                _STATE = "playing";
            }
            TogglePlay();
            
        }

        private void playNextSong()
        {
            var _songs = ((AppContext)this.DataContext).songs;

            //_STATE = "paused";
            if (playingSong != null)
            {
                AddHistory(playingSong);
            }

            // queue / 1st priority
            if (songQueue != null && _songs != null)
            {
                if (songQueue.Count != 0)
                {
                    Player.Play(songQueue.First().path);
                    playingSong = songQueue.First();

                    songQueue.RemoveAt(0);
                    ((AppContext)this.DataContext).UpdateQueue(songQueue);

                }
                // random / 2nd priority
                else
                {
                    if (_PLAYMODE == Modes.Random)
                    {
                        if(songListRandom != null)
                        {
                            if (songListRandom.Count > 0)
                            {
                                if (randomPos + 1 < songListRandom.Count)
                                {
                                    Player.Play(songListRandom.ElementAt(randomPos + 1).path);
                                    playingSong = songListRandom.ElementAt(randomPos + 1);

                                    randomPos++;
                                }
                                // if end of playlist
                                else
                                {
                                    randomPos = -1;
                                    ShuffleSongs(songListRandom);
                                    // make sure next song is different
                                    while (songListRandom.First() == playingSong)
                                    {
                                        ShuffleSongs(songListRandom);
                                    }

                                    Player.Play(songListRandom.ElementAt(randomPos + 1).path);
                                    playingSong = songListRandom.ElementAt(randomPos + 1);

                                    randomPos++;
                                }
                            }
                        }
                    }
                    // normal / 3rd priority
                    else
                    {
                        int pos = lastPos;

                        if (pos == _songs.Count - 1)
                        {
                            pos = -1;
                            lastPos = pos;
                        }

                        if (pos + 1 < _songs.Count)
                        {
                            Player.Play(_songs.ElementAt(pos + 1).path);
                            playingSong = _songs.ElementAt(pos + 1);
                        }
                        else if(lastPos == -1 && _songs.Count > 0)
                        {
                            Player.Play(_songs.First().path);
                            playingSong = _songs.First();
                        }
                        lastPos++;
                    }
                }
            }
            UpdateUI();
        }
        private int getSongPosition(string path)
        {
            var songList = ((AppContext)this.DataContext).songs;
            int pos = -1;
            for (int i = 0; i < songList.Count; i++)
            {
                if (songList[i].path == path)
                {
                    pos = i; break;
                }
            }
            return pos;
        }
        private void AddHistory(Song song)
        {
            if(songHistory != null && playingSong != null)
            {
                AppNotificationService.SendNotification("Playing: " + playingSong.name + " - " + playingSong.author);

                if(songHistory.Count == 0)
                {
                    songHistory.Add(playingSong);
                    historyPos = -1;
                    return;
                }

                if (songHistory.Count < 50 && songHistory[songHistory.Count - 1] != playingSong)
                {
                    songHistory.Add(playingSong);
                }
                else
                {
                    songHistory.RemoveAt(0);
                    songHistory.Add(playingSong);
                }
                historyPos = songHistory.Count - 1;

                ((AppContext)this.DataContext).UpdateHistory(songHistory);

            }
        }

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                this.DragMove();
            }
        }

        private void MinimizeButton(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }
        private void CloseButton(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        
        public void TogglePlay()
        {
            if (PlayButtonIcon.Icon == FontAwesomeIcon.Play)
            {
                PlayButtonIcon.Icon = FontAwesomeIcon.Pause;
            }
            else
            {
                PlayButtonIcon.Icon = FontAwesomeIcon.Play;
            }
        }
        public void ToggleMode()
        {
            if (ModeIcon.Icon == FontAwesomeIcon.List)
            {
                ModeIcon.Icon = FontAwesomeIcon.Random;
            }
            else
            {
                ModeIcon.Icon = FontAwesomeIcon.List;
            }
        }
        public void UpdateUI()
        {
            if(playingSong != null)
            {
                song_title.Content = playingSong?.name;
                MemoryStream ms = new MemoryStream(playingSong.image.Data.Data);
                ms.Seek(0, SeekOrigin.Begin);
                BitmapImage bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.StreamSource = ms;
                bitmap.EndInit();

                song_img.Source = bitmap;
            }
            
        }

        // Fisher-Yates shuffle algorithm / takes list and randomizes
        private Random _rng = new Random();

        public void ShuffleSongs(List<Song> songs)
        {
            int n = songs.Count;
            while (n > 1)
            {
                n--;
                int k = _rng.Next(n + 1);

                Song value = songs[k];
                songs[k] = songs[n];
                songs[n] = value;
            }
        }

        private void Song_Mode(object sender, RoutedEventArgs e)
        {
            if(_PLAYMODE == Modes.Normal)
            {
                _PLAYMODE = Modes.Random;
                var _songs = ((AppContext)this.DataContext).songs;
                songListRandom = new List<Song>(_songs);
                ShuffleSongs(songListRandom);

            }
            else
            {
                _PLAYMODE = Modes.Normal;
            }
            ToggleMode();
        }

        private void AddToQueue(object sender, RoutedEventArgs e)
        {
            var btn = sender as System.Windows.Controls.Button;
            if(songQueue != null)
            {
                if (btn?.DataContext is Song song)
                {
                    songQueue.Add(song);
                    ((AppContext)this.DataContext).UpdateQueue(songQueue);

                    QueueListView.Items.Refresh();

                }

            }

        }


        private void sidePanel_changeToQueue(object sender, RoutedEventArgs e)
        {
            sidePanel.SelectedIndex = 0;
        }
        private void sidePanel_changeToHistory(object sender, RoutedEventArgs e)
        {
            sidePanel.SelectedIndex = 1;
        }
    }


    public enum Modes
    {
        Normal,
        Random
    }


}