using FontAwesome.WPF;
using MusicPlayer_ovh.Model;
using NAudio.Wave;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;



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
        public static RoutedCommand PlayCommand = new RoutedCommand();
        public static RoutedCommand NextCommand = new RoutedCommand();
        public static RoutedCommand PreviousCommand = new RoutedCommand();
        public static RoutedCommand VolumeUpCommand = new RoutedCommand();
        public static RoutedCommand VolumeDownCommand = new RoutedCommand();


        private ObservableCollection<Song> songs = new ObservableCollection<Song>();
        private ObservableCollection<Song> queue = new ObservableCollection<Song>();
        private ObservableCollection<Song> history = new ObservableCollection<Song>();

        private DispatcherTimer timer;
        private bool isDragging = false;

        public AudioPlayer Player = new AudioPlayer();
        private string _STATE;
        private Song? playingSong;
        private List<Song>? songHistory;
        private List<Song>? songListRandom;
        private List<Song>? songQueue;
        private Modes _PLAYMODE;
        int randomPos = -1;
        int historyPos = 0;
        int lastPos = -1;
        float volume = 0.5f;
        string path;

        private Mixer? mixerWindow;

        public MainWindow()
        {
            InitializeComponent();

            //this.DataContext = new MusicListContext("C:\\Users\\w\\Music");
            loadPath();

            this.DataContext = new AppContext(path);
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
            volume = Properties.Settings.Default.Volume;

            setCommandBindings();

            UpdateUI();

        }

        private void setCommandBindings()
        {
            CommandBindings.Add(new CommandBinding(PlayCommand, Song_PlayPause));
            InputBindings.Add(new KeyBinding(PlayCommand, Key.Space, ModifierKeys.None));
            InputBindings.Add(new KeyBinding(PlayCommand, Key.MediaPlayPause, ModifierKeys.None));

            CommandBindings.Add(new CommandBinding(NextCommand, Song_Next));
            InputBindings.Add(new KeyBinding(NextCommand, Key.MediaNextTrack, ModifierKeys.None));

            CommandBindings.Add(new CommandBinding(PreviousCommand, Song_Previous));
            InputBindings.Add(new KeyBinding(PreviousCommand, Key.MediaPreviousTrack, ModifierKeys.None));

            CommandBindings.Add(new CommandBinding(VolumeUpCommand, (s, e) =>
            {
                setVolume(Math.Min(1.0f, volume + 0.05f));
                UpdateUI();
            }));
            InputBindings.Add(new KeyBinding(VolumeUpCommand, Key.VolumeUp, ModifierKeys.None));
            CommandBindings.Add(new CommandBinding(VolumeDownCommand, (s, e) =>
            {
                setVolume(Math.Max(0.0f, volume - 0.05f));
                UpdateUI();
            }));
            InputBindings.Add(new KeyBinding(VolumeDownCommand, Key.VolumeDown, ModifierKeys.None));
        }

        protected void HandleDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var song = ((ListViewItem)sender).Content as Song;
            if (song != null)
            {
                Player.Play(song.path);
                Player.Volume(volume);

                AddHistory(song);

                _STATE = "playing";
                playingSong = song;
                TogglePlay();
                lastPos = getSongPosition(song.path);
                //PlayFile(song.path);
                //playingSong = song;

                //lastPos = getSongPosition(song.path);
                setTimer();

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
                timer.Stop();
            }
            else if (_STATE == "paused" && playingSong != null)
            {
                Player.Resume();
                _STATE = "playing";
                timer.Start();
            }
            else if (_STATE == "paused" && playingSong == null && _songs.Count > 0)
            {
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
            Player.Volume(volume);
            _STATE = "playing";
            setTimer();
            TogglePlay();
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
            if (_STATE == "playing")
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

                lb_total_time.Content = Player.TotalSecondsStr;
                song_bar.Maximum = Player.TotalSeconds;


                MemoryStream ms = new MemoryStream(playingSong.image.Data.Data);
                ms.Seek(0, SeekOrigin.Begin);
                BitmapImage bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.StreamSource = ms;
                bitmap.EndInit();

                song_img.Source = bitmap;

            }

            volume_slider.Value = volume;


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

        // slider volume movement
        private void SliderMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is Slider slider)
            {
                e.Handled = true;

                slider.CaptureMouse();

                UpdateValueToMouse(slider, e.GetPosition(slider));
            }
        }
        private void SliderMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (sender is Slider slider && slider.IsMouseCaptured)
            {
                e.Handled = true;
                slider.ReleaseMouseCapture();
            }
        }
        private void Slider_LostMouseCapture(object sender, MouseEventArgs e)
        {

            //AppNotificationService.SendNotification("Capture Lost - Stopping Drag");

        }
        private void SliderMouseMove(object sender, MouseEventArgs e)
        {
            if (sender is Slider slider && slider.IsMouseCaptured)
            {
                e.Handled = true;
                UpdateValueToMouse(slider, e.GetPosition(slider));
            }
        }
        private void UpdateValueToMouse(Slider slider, Point mousePos)
        {
            double ratio = mousePos.X / slider.ActualWidth;

            ratio = Math.Max(0, Math.Min(1, ratio));

            slider.Value = ratio * (slider.Maximum - slider.Minimum) + slider.Minimum;

            setVolume((float)slider.Value);
            //Player.Volume((float)slider.Value);
            //volume = (float)slider.Value;
        }
        // songbar movement
        private void SongBarMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is Slider slider)
            {
                e.Handled = true;

                slider.CaptureMouse();

                UpdateValueToMouseSongBar(slider, e.GetPosition(slider));
                
            }
        }
        private void SongBarMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (sender is Slider slider && slider.IsMouseCaptured)
            {
                e.Handled = true;
                
                slider.ReleaseMouseCapture();

            }
        }
        private void SongBarMouseMove(object sender, MouseEventArgs e)
        {
            if (sender is Slider slider && slider.IsMouseCaptured)
            {
                e.Handled = true;
                UpdateValueToMouseSongBar(slider, e.GetPosition(slider));
            }
        }
        private void UpdateValueToMouseSongBar(Slider slider, Point mousePos)
        {
            double ratio = mousePos.X / slider.ActualWidth;

            ratio = Math.Max(0, Math.Min(1, ratio));

            slider.Value = ratio * (slider.Maximum - slider.Minimum) + slider.Minimum;

            Player.Seek(slider.Value);
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

        private void Button_OpenEqualizer(object sender, RoutedEventArgs e)
        {
            if (mixerWindow == null)
            {
                mixerWindow = new Mixer(this.Player);
                mixerWindow.Owner = this;
            }

            mixerWindow.Show();
            mixerWindow.Activate();
        }

        
        private void setVolume(float vol)
        {
            Player.Volume(vol);
            volume = vol;
            
        }
        // song progress bar
        public void setTimer()
        {
            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromMilliseconds(100);
            timer.Tick += (s,e)=>
            {
                if (!isDragging)
                {
                    song_bar.Value = Player.CurrentSeconds;
                    lb_current_time.Content = Player.CurrentSecondsStr;
                }
                if (Player.TotalSeconds > 0 &&
                    (Player.TotalSeconds - Player.CurrentSeconds) < 0.5)
                {
                    AppNotificationService.SendNotification("Song ended");

                    timer.Stop();
                    playNextSong();
                }
            };
            timer.Start();
        }


        protected override void OnClosing(CancelEventArgs e)
        {
            Properties.Settings.Default.Volume = volume;

            Properties.Settings.Default.Path = path;

            Properties.Settings.Default.Save();

            base.OnClosing(e);
        }

        private async Task Path()
        {
            
            if (Directory.Exists(Clipboard.GetText()))
            {
                ((AppContext)this.DataContext).LoadSongs(Clipboard.GetText());
                path = Clipboard.GetText();

                songs = ((AppContext)this.DataContext).songs;
                queue = ((AppContext)this.DataContext).queue;
                history = ((AppContext)this.DataContext).history;
            }
            await Task.CompletedTask;
        }

        private void PathButtonClick(object sender, RoutedEventArgs e)
        {;
                using var _ = Path();
        }

        private void loadPath()
        {
            if(Properties.Settings.Default.Path != null && Directory.Exists(Properties.Settings.Default.Path))
            {
                path = Properties.Settings.Default.Path;

            }
            else
            {
                path = Environment.GetFolderPath(Environment.SpecialFolder.MyMusic);
            }
        }

    }

    public enum Modes
    {
        Normal,
        Random
    }


}