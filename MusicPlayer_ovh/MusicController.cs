using MusicPlayer_ovh.Model;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Resources;
using TagLib;

namespace MusicPlayer_ovh
{
    public sealed class MusicController
    {
        public string? DirectoryPath { get; set; }
        private DirectoryInfo? DirectoryInfo { get; set; }
        private FileInfo[]? files { get; set; }

        public static MusicController musicController = new MusicController();
        public static MediaPlayer mediaPlayer = new MediaPlayer();


        //public ObservableCollection<Song>? songs;
        public List<Song>? songs;


        private static readonly Lazy<MusicController> _instance = new(() => new MusicController());


        public async Task<List<Song>?> LoadMusicFilesAsync(string DirectoryPath)
        {
            if (isPathCorrect(DirectoryPath) == false)
            {
                return null;
            }
            return await Task.Run(() => listMusicFiles(DirectoryPath));
        }
        // get list of mp3 files in directory
        public List<Song>? listMusicFiles(string DirectoryPath)
        {
            if(isPathCorrect(DirectoryPath) == false)
            {
                return null;
            }
            try
            {
                this.DirectoryPath = DirectoryPath;
                DirectoryInfo = new DirectoryInfo(DirectoryPath);
                songs = new List<Song>();

                files = DirectoryInfo.GetFiles("*.mp3");
            } catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return null;
            }

            songs.Clear();

            // set default image if missing
            byte[] imageBytes;
            Uri uri = new Uri("pack://application:,,,/Images/missing_icon.png");
            StreamResourceInfo sri = Application.GetResourceStream(uri);
            using (var stream = sri.Stream)
            using (var ms = new MemoryStream())
            {
                stream.CopyTo(ms);
                imageBytes = ms.ToArray();
            }
            IPicture picture = new TagLib.Picture();
                picture.MimeType = "image/png";
                picture.Type = PictureType.FrontCover;
                picture.Data = imageBytes;
            // read tags from mp3 files
            foreach (FileInfo file in files){
                Console.WriteLine("Processing file: " + file.FullName);
                try
                {
                    TagLib.File tagFile = TagLib.File.Create(file.FullName);
                    string title = file.Name;
                    string author = "";
                    string length = "";
                    
                    TagLib.IPicture image = picture;

                    if (tagFile.Tag.Title != null)
                    {
                        char[] delimiters = new char[] { '-', '(' , '.', '['};
                        string[] t = tagFile.Tag.Title.Split(delimiters);
                        title = t[0]; 
                    
                        } else { continue; }
                    if (tagFile.Tag.Performers[0] != null) { author = tagFile.Tag.Performers[0]; } else { author = "unknown"; }
                    if (tagFile.Tag.Pictures[0] != null) { image = tagFile.Tag.Pictures[0]; }
                    var reader = new AudioFileReader(file.FullName);
                    TimeSpan duration = reader.TotalTime;
                    length = duration.ToString(@"m\:ss");

                    songs.Add(new Song(file.FullName, title, author, length, image));

                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    break;
                }
            }
            return songs;
        }
        // check if directory contains mp3 files
        public bool isPathCorrect(string path)
        {
            bool correct = false;
            try
            {
                DirectoryInfo tempDirInfo = new DirectoryInfo(path);
                FileInfo[] tempFiles = tempDirInfo.GetFiles("*.mp3");
                if (tempFiles.Count() > 0)
                {
                    correct = true;
                }
            }
            catch { }
            return correct;
        }
    }
}
