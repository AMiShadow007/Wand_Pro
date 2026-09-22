using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Windows;

namespace WandEnhancer.Core.Services
{
    public static class LocalizationManager
    {
        public static readonly List<CultureInfo> SupportedLanguages = new List<CultureInfo>
        {
            new CultureInfo("en-US"),
            new CultureInfo("bn-BD"),
            new CultureInfo("zh-CN"),
            new CultureInfo("de-DE"),
            new CultureInfo("fr-FR"),
            new CultureInfo("es-ES"),
            new CultureInfo("it-IT"),
            new CultureInfo("pt-BR"),
            new CultureInfo("pl-PL"),
            new CultureInfo("ru-RU"),
            new CultureInfo("uk-UA"),
            new CultureInfo("ja-JP"),
            new CultureInfo("tr-TR")
        };

        private static CultureInfo _currentLanguage;
        private static ResourceDictionary _englishBaseDictionary;
        private static ResourceDictionary _activeLocaleDictionary;

        public static string Get(string key)
        {
            return Application.Current?.TryFindResource(key) as string ?? key;
        }

        public static string Format(string key, params object[] args)
        {
            return string.Format(Get(key), args);
        }

        public static CultureInfo CurrentLanguage
        {
            get => _currentLanguage;
            set => SetLanguage(value);
        }

        public static void Initialize()
        {
            _englishBaseDictionary = new ResourceDictionary
            {
                Source = new Uri("Locale/lang.en-US.xaml", UriKind.Relative)
            };

            var savedLanguage = SettingsManager.LoadSettings()?.Language;
            CultureInfo targetCulture = null;

            if (!string.IsNullOrEmpty(savedLanguage))
            {
                targetCulture = SupportedLanguages.FirstOrDefault(c =>
                    string.Equals(c.Name, savedLanguage, StringComparison.OrdinalIgnoreCase));
            }

            if (targetCulture == null)
            {
                var systemCulture = Thread.CurrentThread.CurrentUICulture;
                targetCulture = SupportedLanguages.FirstOrDefault(c =>
                    c.Name == systemCulture.Name ||
                    c.TwoLetterISOLanguageName == systemCulture.TwoLetterISOLanguageName);
            }

            SetLanguage(targetCulture ?? SupportedLanguages[0], saveSettings: false);
        }

        private static void SetLanguage(CultureInfo culture, bool saveSettings = true)
        {
            if (culture == null)
                throw new ArgumentNullException(nameof(culture));

            var supportedCulture = SupportedLanguages.FirstOrDefault(c => c.Name == culture.Name)
                ?? SupportedLanguages[0];

            if (Equals(supportedCulture, _currentLanguage))
                return;

            _currentLanguage = supportedCulture;
            Thread.CurrentThread.CurrentUICulture = supportedCulture;

            var localeDict = new ResourceDictionary();
            if (_englishBaseDictionary != null && supportedCulture.Name != SupportedLanguages[0].Name)
            {
                foreach (var key in _englishBaseDictionary.Keys)
                    localeDict[key] = _englishBaseDictionary[key];
            }

            var targetDict = new ResourceDictionary
            {
                Source = new Uri($"Locale/lang.{supportedCulture.Name}.xaml", UriKind.Relative)
            };

            foreach (DictionaryEntry entry in targetDict)
                localeDict[entry.Key] = entry.Value;

            var merged = Application.Current.Resources.MergedDictionaries;
            if (_activeLocaleDictionary != null && merged.Contains(_activeLocaleDictionary))
                merged[merged.IndexOf(_activeLocaleDictionary)] = localeDict;
            else
                merged.Add(localeDict);

            _activeLocaleDictionary = localeDict;

            if (saveSettings)
            {
                var settings = SettingsManager.LoadSettings() ?? new AppSettings();
                settings.Language = supportedCulture.Name;
                SettingsManager.SaveSettings(settings);
            }
        }

        public static string GetLanguageDisplayName(CultureInfo culture)
        {
            try
            {
                var dict = new ResourceDictionary
                {
                    Source = new Uri($"Locale/lang.{culture.Name}.xaml", UriKind.Relative)
                };

                if (dict.Contains("language_display_name"))
                    return dict["language_display_name"] as string ?? culture.NativeName;
            }
            catch
            {
                // Fall back to the native name if a locale resource is unavailable.
            }

            return culture.NativeName;
        }
    }
}
