using System;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace GeekHub
{
    public sealed partial class Languages : SettingsFlyout
    {
        public Languages()
        {
            this.InitializeComponent();
            LoadSavedLanguage();
        }

        private void LoadSavedLanguage()
        {
            var saved = ApplicationData.Current.LocalSettings.Values["lang"] as string;

            if (string.IsNullOrEmpty(saved))
                saved = "en-US";

            LanguageManager.CurrentLanguage = saved;

            foreach (ComboBoxItem item in LanguageCombo.Items)
            {
                if ((string)item.Tag == saved)
                {
                    LanguageCombo.SelectedItem = item;
                    break;
                }
            }
        }

        private void LanguageCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var item = LanguageCombo.SelectedItem as ComboBoxItem;

            if (item == null) return;

            string lang = item.Tag.ToString();

            ApplicationData.Current.LocalSettings.Values["lang"] = lang;

            // REQUIRED for x:Uid to update
            Windows.Globalization.ApplicationLanguages.PrimaryLanguageOverride = lang;
        }
    }
}