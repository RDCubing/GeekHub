using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Newtonsoft.Json;
using System.Threading.Tasks;
using Windows.Web.Http;
using System.Collections.ObjectModel;
using System.ComponentModel;
using Windows.UI;
using Windows.Storage;
using Windows.UI.Xaml.Media.Imaging;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace GeekHub
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page, INotifyPropertyChanged
    {
        private DispatcherTimer _feedTimer;
        private List<FeedItem> _cachedItems;
        private int _currentIndex = 0;
        public ObservableCollection<Project> Projects { get; set; }
        public event PropertyChangedEventHandler PropertyChanged;
        private FeedItem _currentFeedItem;

        public FeedItem CurrentFeedItem
        {
            get
            {
                return _currentFeedItem;
            }
            set
            {
                _currentFeedItem = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CurrentFeedItem)));
            }
        }
        public MainPage()
        {
            this.InitializeComponent();
            Projects = new ObservableCollection<Project>();
            // IMPORTANT: set DataContext
            this.DataContext = this;
        }

        private async Task LoadProjectsAsync()
        {
            try
            {
                var baseUrl = "https://raw.githubusercontent.com/RDCubing/GeekHub/master/GeekHub/projects.json";
                var url = $"{baseUrl}?t={DateTime.UtcNow.Ticks}";

                using (var client = new HttpClient())
                {
                    var json = await client.GetStringAsync(new Uri(url));
                    var projectFeed = JsonConvert.DeserializeObject<ProjectFeed>(json);

                    if (projectFeed?.projects == null)
                        return;

                    Projects.Clear();

                    foreach (var p in projectFeed.projects)
                    {
                        var imageFileName = $"{p.Title}_icon.png";
                        var detailFileName = $"{p.Title}_detail.png";

                        var localImage = await DownloadImageAsync(p.ImagePath, imageFileName);
                        var localDetail = await DownloadImageAsync(p.DetailImagePath, detailFileName);

                        Projects.Add(new Project
                        {
                            Title = p.Title,
                            Subtitle = p.Subtitle,

                            ImagePath = p.ImagePath,
                            DetailImagePath = p.DetailImagePath,

                            LocalImagePath = localImage,
                            LocalDetailImagePath = localDetail,

                            Version = p.Version,
                            Description = p.Description,
                            AccentBrush = new SolidColorBrush(HexToColor(p.AccentColor))
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.ToString());
            }
        }

        private Color HexToColor(string hex)
        {
            if (string.IsNullOrEmpty(hex))
                return Colors.Transparent;

            hex = hex.Replace("#", "");

            byte a = 255;
            byte r = Convert.ToByte(hex.Substring(0, 2), 16);
            byte g = Convert.ToByte(hex.Substring(2, 2), 16);
            byte b = Convert.ToByte(hex.Substring(4, 2), 16);

            return Color.FromArgb(a, r, g, b);
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            FadeInStoryboard.Begin();
            SlideInStoryboard.Begin();
            LoadLatestFeed();
            await LoadProjectsAsync();
        }

        private async Task<Feed> GetJsonFeedAsync()
        {
            var baseUrl = "https://gist.githubusercontent.com/RDCubing/44671b4e680aba470e4b96cff5eb5840/raw/highlights.json";
            var timestamp = DateTime.UtcNow.Ticks;
            var url = $"{baseUrl}?t={timestamp}";

            using (var client = new HttpClient())
            {
                var json = await client.GetStringAsync(new Uri(url));
                Feed feed = JsonConvert.DeserializeObject<Feed>(json);
                return feed;
            }
        }

        private async void LoadLatestFeed()
        {
            await RefreshFeed();
            StartFeedAutoRefresh();
        }

        private async Task RefreshFeed()
        {
            var feed = await GetJsonFeedAsync();

            _cachedItems = feed.items
                .OrderByDescending(x => DateTime.Parse(x.date_added))
                .ToList();

            if (_cachedItems == null || _cachedItems.Count == 0)
                return;

            // cycle through items
            var item = _cachedItems[_currentIndex];

            CurrentFeedItem = item;

            FadeInContentStoryboard.Begin();

            _currentIndex++;

            if (_currentIndex >= _cachedItems.Count)
                _currentIndex = 0;
        }

        private void StartFeedAutoRefresh()
        {
            _feedTimer = new DispatcherTimer();
            _feedTimer.Interval = TimeSpan.FromSeconds(15);
            _feedTimer.Tick += async (s, e) =>
            {
                await RefreshFeed();
            };

            _feedTimer.Start();
        }

        private void ItemView_ItemClick(object sender, ItemClickEventArgs e)
        {
            var project = e.ClickedItem as Project;
            Frame.Navigate(typeof(ProjectPage), project);
        }

        private void Header_Click(object sender, RoutedEventArgs e)
        {
            // Example placeholder behavior
            var button = sender as Button;
            var group = button?.DataContext;

            // TODO: expand group / navigate / etc.
        }

        private void ProjectsHeader_Click(object sender, RoutedEventArgs e)
        {
            // Example action
            Frame.Navigate(typeof(ProjectPage)); // or open menu, refresh, etc.
        }

        private async Task<string> DownloadImageAsync(string url, string fileName)
        {
            try
            {
                var folder = Windows.Storage.ApplicationData.Current.LocalFolder;

                var file = await folder.CreateFileAsync(
                    fileName,
                    Windows.Storage.CreationCollisionOption.ReplaceExisting);

                using (var client = new HttpClient())
                {
                    var buffer = await client.GetBufferAsync(new Uri(url));
                    await Windows.Storage.FileIO.WriteBufferAsync(file, buffer);
                }

                // ✅ IMPORTANT FIX
                return "ms-appdata:///local/" + fileName;
            }
            catch
            {
                return null;
            }
        }

        private async Task DeleteAllFiles(StorageFolder folder)
        {
            var files = await folder.GetFilesAsync();
            foreach (var file in files)
            {
                await file.DeleteAsync();
            }

            var folders = await folder.GetFoldersAsync();
            foreach (var sub in folders)
            {
                await sub.DeleteAsync();
            }
        }
    }
    public class Feed
    {
        public string feed_title { get; set; }
        public List<FeedItem> items { get; set; }
    }

    public class FeedItem
    {
        public string title { get; set; }
        public string author { get; set; }
        public string content { get; set; }
        public string date_added { get; set; }
        public string date_modified { get; set; }
    }

    public class Project
    {
        public string Title { get; set; }
        public string Subtitle { get; set; }

        public string ImagePath { get; set; }          // remote (from JSON)
        public string DetailImagePath { get; set; }    // remote

        public string LocalImagePath { get; set; }     // NEW
        public string LocalDetailImagePath { get; set; } // NEW

        public Brush AccentBrush { get; set; }
        public string Description { get; set; }
        public string Version { get; set; }
    }

    public class ProjectDto
    {
        public string Title { get; set; }
        public string Subtitle { get; set; }
        public string ImagePath { get; set; }
        public string DetailImagePath { get; set; }
        public string AccentColor { get; set; }
        public string Description { get; set; }
        public string Version { get; set; }
    }

    public class ProjectFeed
    {
        public List<ProjectDto> projects { get; set; }
    }
}
