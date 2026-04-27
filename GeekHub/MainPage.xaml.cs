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
            LoadProjects();
        }

        private void LoadProjects()
        {
            Projects.Add(new Project
            {
                Title = "QuoteTile",
                Subtitle = "Seek inspiration from quotes",
                ImagePath = "ms-appx:///Images/q.png",
                DetailImagePath = "ms-appx:///Images/quote.png",
                AccentBrush = new SolidColorBrush(Color.FromArgb(255, 13, 71, 161)),
                Version = "2.1.3.0",
                Description = "QuoteTile is a simple app that shows inspiring quotes. Refresh quotes anytime, copy quotes to clipboard with one single click, and share quotes anywhere! Choose your favorite quotes and store them as many as you like!"
            });

            Projects.Add(new Project
            {
                Title = "GDC Media Player",
                Subtitle = "Vibe-cooling media player",
                ImagePath = "ms-appx:///Images/mp3.png",
                DetailImagePath = "ms-appx:///Images/media.png",
                AccentBrush = new SolidColorBrush(Color.FromArgb(255, 86, 18, 105)),
                Version = "1.3.0.0",
                Description = "GDC Media Player is a lightweight and modern audio player built for music lovers and tech enthusiasts. It delivers high-quality playback with minimal resource usage, keeping your tunes at the center. Designed for the Geek Devs Comm community, it combines simplicity with smart library management for the ultimate listening experience."
            });

            Projects.Add(new Project
            {
                Title = "GDC Highlights",
                Subtitle = "Latest news from GDC",
                ImagePath = "ms-appx:///Images/gdch.png",
                DetailImagePath = "ms-appx:///Images/feed.png",
                AccentBrush = new SolidColorBrush(Color.FromArgb(255, 0, 129, 204)),
                Version = "1.2.1.1",
                Description = "GDC Highlights is a Windows 8 app that brings the latest news, updates, and community highlights directly from GDC Management, providing a central hub for everything happening in the GDC ecosystem. Hosted via Gist, the app makes the JSON feed easily accessible to everyone, allowing both casual users and developers to stay informed or integrate the updates into their own projects. Beyond general news, GDC Highlights also posts official updates related to GDC Mainline Apps, including new features, bug fixes, and performance improvements, making it the go-to source for keeping up with the latest releases, community projects, and insights from the GDC team, all in a single, convenient app. Additionally, GDC Highlights supports RSS feeds, enabling users to subscribe to their favorite updates and receive real-time news directly within the app."

            });

            Projects.Add(new Project
            {
                Title = "ChrisRLillo Music",
                Subtitle = "Chilean music in its peak",
                ImagePath = "ms-appx:///Images/chris.png",
                DetailImagePath = "ms-appx:///Images/rlillo.png",
                AccentBrush = new SolidColorBrush(Color.FromArgb(255, 0, 0, 0)),
                Version = "1.2.1.0",
                Description = "Discover the latest songs, updates, and exclusive content from ChrisRLillo. Explore, enjoy, and stay tuned for more!"
            });
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            FadeInStoryboard.Begin();
            SlideInStoryboard.Begin();
            LoadLatestFeed();
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
        public string ImagePath { get; set; }
        public string DetailImagePath { get; set; }
        public Brush AccentBrush { get; set; }
        public string Description { get; set; }
        public string Version { get; set; }
    }
}
