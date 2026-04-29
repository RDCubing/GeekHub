using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.Resources;

namespace GeekHub
{
    public static class LanguageManager
    {
        public static string CurrentLanguage = "en-US";

        private static ResourceLoader loader = new ResourceLoader();

        public static string GetString(string key)
        {
            return loader.GetString(key);
        }
    }
}
