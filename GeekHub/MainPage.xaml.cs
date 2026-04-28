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
using System.Diagnostics;
using Windows.Data.Xml.Dom;
using Windows.UI.Notifications;
using Windows.UI.ApplicationSettings;
using Windows.System;

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
        private DispatcherTimer _tileTimer;
        private List<Project> _tileProjects;
        private int _tileIndex = 0;
        public ObservableCollection<Project> GridProjects { get; set; }
        public Project FeaturedProject { get; set; }
        private const int FeaturedIndex = 8;

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
            GridProjects = new ObservableCollection<Project>();
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
                        var project = new Project
                        {
                            Title = p.Title,
                            Subtitle = p.Subtitle,
                            Publisher = p.Publisher,
                            Version = p.Version,
                            Description = p.Description,
                            AccentBrush = new SolidColorBrush(HexToColor(p.AccentColor)),
                            DownloadUrl = p.DownloadUrl,
                            SourceUrl = p.SourceUrl,
                            ImagePath = p.ImagePath
                        };

                        project.LocalImage = await DownloadImageAsync(p.ImagePath, $"{p.Title}_icon.png");
                        project.LocalDetailImage = await DownloadImageAsync(p.DetailImagePath, $"{p.Title}_detail.png");

                        Projects.Add(project);
                    }

                    FeaturedProject = Projects.Count > FeaturedIndex
    ? Projects[FeaturedIndex]
    : Projects.FirstOrDefault();

                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(FeaturedProject)));

                    // =========================
                    // GRIDVIEW LIMIT (ONLY UI)
                    // =========================
                    GridProjects.Clear();

                    foreach (var p in Projects.Take(6))
                    {
                        GridProjects.Add(p);
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

        private void FeedHeader_Click(object sender, RoutedEventArgs e)
        {
            if (Projects == null || Projects.Count == 0)
                return;

            // Example: open 9th project (index 8)
            int index = 3;

            if (Projects.Count > index)
            {
                var project = Projects[index];
                Frame.Navigate(typeof(ProjectPage), project);
            }
        }


        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            FadeInStoryboard.Begin();
            LongFadeInStoryboard.Begin();
            SlideInStoryboard.Begin();
            await DeleteAllFiles(ApplicationData.Current.LocalFolder);
            CheckStorage();
            LoadLatestFeed();
            await LoadProjectsAsync();
            _tileProjects = Projects.ToList();
            StartTileUpdates();
        }

        private void UpdateTile(Project project)
        {
            string img = project.ImagePath;

            // =========================
            // MEDIUM
            // =========================
            XmlDocument mediumTileXml =
                TileUpdateManager.GetTemplateContent(
                    TileTemplateType.TileSquare150x150PeekImageAndText04);

            var mediumText = mediumTileXml.GetElementsByTagName("text");
            if (mediumText.Length > 0)
                mediumText[0].InnerText = project.Subtitle;

            var mediumImage = mediumTileXml.GetElementsByTagName("image").Item(0);
            if (mediumImage != null && !string.IsNullOrEmpty(img))
            {
                ((Windows.Data.Xml.Dom.XmlElement)mediumImage)
                    .SetAttribute("src", img);
            }


            // =========================
            // WIDE
            // =========================
            XmlDocument wideTileXml =
                TileUpdateManager.GetTemplateContent(
                    TileTemplateType.TileWide310x150SmallImageAndText02);

            var wideImage = wideTileXml.GetElementsByTagName("image").Item(0);
            if (wideImage != null && !string.IsNullOrEmpty(img))
            {
                ((Windows.Data.Xml.Dom.XmlElement)wideImage)
                    .SetAttribute("src", img);
            }

            var wideText = wideTileXml.GetElementsByTagName("text");

            if (wideText.Length > 0)
                wideText[0].InnerText = project.Title;

            if (wideText.Length > 1)
                wideText[1].InnerText = project.Subtitle;

            if (wideText.Length > 2)
                wideText[2].InnerText = "GeekHub";


            XmlDocument largeTileXml =
    TileUpdateManager.GetTemplateContent(
        TileTemplateType.TileSquare310x310SmallImageAndText01);

            // IMAGE (top-left square)
            var imageNode = largeTileXml.GetElementsByTagName("image").Item(0);
            if (imageNode != null)
            {
                ((Windows.Data.Xml.Dom.XmlElement)imageNode)
                    .SetAttribute("src", project.ImagePath);
            }

            // TEXTS
            var textNodes = largeTileXml.GetElementsByTagName("text");

            if (textNodes.Length > 0)
                textNodes[0].InnerText = project.Title;       // header

            if (textNodes.Length > 1)
                textNodes[1].InnerText = project.Description; // wrapped text


            // =========================
            // COMBINE
            // =========================
            IXmlNode visualNode =
                mediumTileXml.GetElementsByTagName("visual").Item(0);

            visualNode.AppendChild(
                mediumTileXml.ImportNode(
                    wideTileXml.GetElementsByTagName("binding").Item(0),
                    true));

            visualNode.AppendChild(
                mediumTileXml.ImportNode(
                    largeTileXml.GetElementsByTagName("binding").Item(0),
                    true));


            // =========================
            // SEND
            // =========================
            var tile = new TileNotification(mediumTileXml);
            TileUpdateManager.CreateTileUpdaterForApplication().Update(tile);
        }

        private string GetImageUri(BitmapImage img)
        {
            return img?.UriSource?.ToString() ?? "";
        }

        private void StartTileUpdates()
        {
            _tileTimer = new DispatcherTimer();
            _tileTimer.Interval = TimeSpan.FromSeconds(15);

            _tileTimer.Tick += (s, e) =>
            {
                if (_tileProjects == null || _tileProjects.Count == 0)
                    return;

                var project = _tileProjects[_tileIndex];

                UpdateTile(project);

                _tileIndex++;
                if (_tileIndex >= _tileProjects.Count)
                    _tileIndex = 0;
            };

            // 🔥 RUN IMMEDIATELY FIRST
            if (_tileProjects != null && _tileProjects.Count > 0)
            {
                _tileIndex = 0;
                UpdateTile(_tileProjects[_tileIndex]);
                _tileIndex = 1; // prepare next tick
            }

            _tileTimer.Start();
        }

        private async void CheckStorage()
        {
            double size = await GetAppStorageSizeMB();
            Debug.WriteLine($"App storage: {size:F2} MB");
        }

        private async Task<double> GetAppStorageSizeMB()
        {
            long totalBytes = 0;

            try
            {
                var folder = ApplicationData.Current.LocalFolder;
                totalBytes = await GetFolderSizeAsync(folder);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Size check failed: " + ex.Message);
            }

            return totalBytes / 1024.0 / 1024.0;
        }

        private async Task<long> GetFolderSizeAsync(StorageFolder folder)
        {
            long size = 0;

            var files = await folder.GetFilesAsync();
            foreach (var file in files)
            {
                try
                {
                    var props = await file.GetBasicPropertiesAsync();
                    size += (long)props.Size;
                }
                catch { }
            }

            var folders = await folder.GetFoldersAsync();
            foreach (var sub in folders)
            {
                size += await GetFolderSizeAsync(sub);
            }

            return size;
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

        private void FeaturedProject_Click(object sender, RoutedEventArgs e)
        {
            if (FeaturedProject == null)
                return;

            Frame.Navigate(typeof(ProjectPage), FeaturedProject);
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
            Frame.Navigate(typeof(ProjectsList), Projects);
        }

        private async Task<BitmapImage> DownloadImageAsync(string url, string fileName)
        {
            try
            {
                var folder = ApplicationData.Current.LocalFolder;

                var file = await folder.CreateFileAsync(
                    fileName,
                    CreationCollisionOption.ReplaceExisting);

                using (var client = new HttpClient())
                {
                    var buffer = await client.GetBufferAsync(new Uri(url));
                    await FileIO.WriteBufferAsync(file, buffer);
                }

                var img = new BitmapImage();
                using (var stream = await file.OpenAsync(FileAccessMode.Read))
                {
                    img.SetSource(stream);
                }

                return img;
            }
            catch
            {
                return null;
            }
        }

        private async Task DeleteAllFiles(StorageFolder folder)
        {
            foreach (var file in await folder.GetFilesAsync())
            {
                await file.DeleteAsync(StorageDeleteOption.PermanentDelete);
            }

            foreach (var subFolder in await folder.GetFoldersAsync())
            {
                await DeleteAllFiles(subFolder);
                await subFolder.DeleteAsync(StorageDeleteOption.PermanentDelete);
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

        public BitmapImage LocalImage { get; set; }
        public BitmapImage LocalDetailImage { get; set; }

        public Brush AccentBrush { get; set; }
        public string Description { get; set; }
        public string Version { get; set; }
        public string DownloadUrl { get; set; }
        public string SourceUrl { get; set; }
        public string ImagePath { get; set; }
        public string Publisher { get; set; }
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
        public string DownloadUrl { get; set; }
        public string SourceUrl { get; set; }
        public string Publisher { get; set; }
    }

    public class ProjectFeed
    {
        public List<ProjectDto> projects { get; set; }
    }
}
