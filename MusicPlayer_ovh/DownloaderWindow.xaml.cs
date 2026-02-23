using AngleSharp.Dom;
using Microsoft.Win32;
using MusicPlayer_ovh.Model;
using NAudio.Wave;
using SpotifyExplode;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics;
using System.DirectoryServices;
using System.Formats.Tar;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using TagLib;
using YoutubeDLSharp;
using YoutubeDLSharp.Options;
using YoutubeExplode;
using YoutubeExplode.Common;
using YoutubeExplode.Videos;
using YoutubeExplode.Videos.Streams;
using static System.Windows.Forms.Design.AxImporter;
using File = System.IO.File;

namespace MusicPlayer_ovh
{
    /// <summary>
    /// Logika interakcji dla klasy DownloaderWindow.xaml
    /// </summary>
    public partial class DownloaderWindow : Window, INotifyPropertyChanged
    {
        Dictionary<string, string> songsToDownload = new ();

        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                _isLoading = value;
                OnPropertyChanged(nameof(IsLoading));
            }
        }

        string? playlistName = null;
        string? playlistPath = null;
        bool isError = false;


        public event PropertyChangedEventHandler? PropertyChanged;

        public DownloaderWindow()
        {
            InitializeComponent();
            DataContext = this;

        }
        // take songs name and artists from spotify using python script and save it to songsToDownload dictionary
        private async Task getSongsFromLink(string link)
        {
            var start = new ProcessStartInfo
            {
                FileName = "Extensions/python-3.13.12-embed-amd64/python.exe",
                Arguments = $"Extensions/spotify_data_script.py {link}",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            };
            using (Process process = Process.Start(start))
            {
                string jsonResponse = process.StandardOutput.ReadToEnd();
                process.WaitForExit();

                if (process.ExitCode == 0)
                {
                    var cleanedJson = jsonResponse.Trim();
                    try
                    {
                        var playlistData = JsonSerializer.Deserialize<List<Dictionary<string, string>>>(cleanedJson);
                        if (playlistData == null || playlistData.Count == 0)
                        {
                            AppNotificationService.SendNotification("Make sure playlist is public, and not empty!");
                            isError = true;
                            return;
                        } else if (playlistData.First().Keys.First()  == "Error")
                        {
                            AppNotificationService.SendNotification($"Error: {playlistData.First().Values.First()}");
                            isError = true;
                            return;
                        }
                        songsToDownload = playlistData.SelectMany(d => d).ToDictionary(k => validateName(k.Key), v => validateName(v.Value));

                    } catch(IOException ex)
                    {
                        AppNotificationService.SendNotification($"Error parsing JSON: {ex.Message}");
                        isError = true;
                    }
                }
                else
                {
                    isError = true;
                    AppNotificationService.SendNotification($"Python Error");
                }
            }
        }
        // take path for download
        private void getPath()
        {
            OpenFolderDialog openFolderDialog = new OpenFolderDialog
            {
                Title = "Select Folder For Download",
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyMusic)
            };
            if (openFolderDialog.ShowDialog() == true)
            {
                playlistPath = openFolderDialog.FolderName;
            }
        }
        private async Task downloadSongs()
        {
            var youtubeClient = new YoutubeClient();

            foreach (var song in songsToDownload)
            {
                await Task.Delay(200);

                var query = $"{song.Key} {song.Value}";
                Dispatcher.Invoke(() => { IsLoading = true; downloadLabel.Content = $"SSDownloading: {query}"; });

                var searchResult = await youtubeClient.Search.GetVideosAsync(query).FirstAsync();
                

                var streamManifest = await youtubeClient.Videos.Streams.GetManifestAsync(searchResult.Id);
                var streamInfo = streamManifest.GetAudioOnlyStreams().GetWithHighestBitrate();

                string tempFile = ""; 
                string finalFile = "";
                await Task.Run(async () =>
                {
                    tempFile = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.tmp");
                    finalFile = $"{playlistPath}/{song.Key} - {song.Value}.mp3";

                    await youtubeClient.Videos.Streams.DownloadAsync(streamInfo, tempFile);

                    using (var reader = new MediaFoundationReader(tempFile))
                    {
                        MediaFoundationEncoder.EncodeToMp3(
                            reader,
                            Path.Combine(playlistPath, $"{song.Key} - {song.Value}.mp3"),
                            192000);
                    }
                });

                try
                {

                    // adding metadata to file
                    var thumbnailUrl = searchResult.Thumbnails.OrderByDescending(t => t.Resolution.Area)
                        .First().Url;
                    using var http = new HttpClient();

                    var imageBytes = await http.GetByteArrayAsync(thumbnailUrl);

                    var file = TagLib.File.Create(finalFile);
                    file.Tag.Title = song.Key;
                    file.Tag.Performers = new[] { song.Value };
                    var picture = new Picture
                    {
                        Type = PictureType.FrontCover,
                        Description = "Cover",
                        MimeType = "image/jpeg",
                        Data = imageBytes
                    };
                    file.Tag.Pictures = new IPicture[] { picture };

                    file.Save();

                    Dispatcher.Invoke(() => { IsLoading = false; });

                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error: {ex.Message}");
                }

                File.Delete(tempFile);
                AppNotificationService.SendNotification($"Downloaded: {song.Key} - {song.Value}");
            }
            AppNotificationService.SendNotification("All songs downloaded!");
        }
        // check if songs from playlist are already in selected folder and if they are remove them from songsToDownload dictionary
        private async Task checkDuplicates()
        {
            if(playlistPath == null)
            {
                //MessageBox.Show("Wrong path");
                playlistPath = Environment.GetFolderPath(Environment.SpecialFolder.MyMusic) + $"/{playlistName}";
            }
            if(!Directory.Exists(playlistPath))
            {
                return;
            }
            try
            {
                DirectoryInfo info = new DirectoryInfo(playlistPath);
                FileInfo[] files = info.GetFiles("*.mp3");

                int i = 0;

               foreach(var song in songsToDownload)
                {
                    if (files.Any(f => f.Name.Contains(song.Key)))
                    {
                        songsToDownload.Remove(song.Key);
                        i++;
                    }
                }
                MessageBox.Show($"{i.ToString()}/{songsToDownload.Count}");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                isError = true;
            }
        }

        // main function that will be called when user click download button, it will call all other functions in right order
        private async Task DownloadTask()
        {
            string? text = btPath.Content.ToString();
            if(text == null) { return; }
            getPath();
            if(playlistPath == null)
            {
                AppNotificationService.SendNotification("No path selected");
                return;
            }

            Dispatcher.Invoke(() => {IsLoading = true;});
            await getSongsFromLink(text);
            //IsLoading = true;
            Dispatcher.Invoke(() => {IsLoading = false;});

            if (isError)
            {
                AppNotificationService.SendNotification("error");
                return;
            }
            await checkDuplicates();
            AppNotificationService.SendNotification($"Songs to download after: {songsToDownload.Count}");
            await downloadSongs();
        }

        private async void DownloadButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _ = DownloadTask();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Błąd pobierania: {ex.Message}");
            }
        }

        private string validateName(string name)
        {
            foreach (char c in Path.GetInvalidFileNameChars())
            {
                name = name.Replace(c, ' ');
            }
            return name;
        }

        protected void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        private void btPathClick(object sender, RoutedEventArgs e)
        {
            string text = Clipboard.GetText();
            if(text.Length == 0)
            {
                AppNotificationService.SendNotification("Nothing copied!");
                return;
            }
            if (text.Contains("?"))
            {
                text = text.Split("?")[0];
            }
            AppNotificationService.SendNotification($"{text.Length}");
            if (!Regex.IsMatch(text, @"^https:\/\/open\.spotify\.com\/playlist\/\w{18,24}$"))
            {
                AppNotificationService.SendNotification("Spotify link is incorrect it should match: ");
                AppNotificationService.SendNotification("https://open.spotify.com/playlist/26LfI62GtHySdAb72jLA3f");
                return;
            }
            btPath.Content = text;
        }
    }
}
