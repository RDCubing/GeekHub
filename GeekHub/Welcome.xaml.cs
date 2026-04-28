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

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace GeekHub
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class Welcome : Page
    {
        public Welcome()
        {
            this.InitializeComponent();
        }

        private void GetStarted_Click(object sender, RoutedEventArgs e)
        {
            GridFadeOutStoryboard.Completed += FadeOutStoryboard_Completed;
            GridFadeOutStoryboard.Begin();
        }

        private void FadeOutStoryboard_Completed(object sender, object e)
        {
            var frame = Window.Current.Content as Frame;
            frame?.Navigate(typeof(MainPage));
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            BackgroundAudio.Play();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            System.Diagnostics.Debug.WriteLine("WELCOME PAGE LOADED");
            GridFadeInStoryboard.Begin();
        }
    }
}
