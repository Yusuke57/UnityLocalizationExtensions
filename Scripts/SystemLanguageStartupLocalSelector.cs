using System;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

namespace Common.Localization
{
    [System.ComponentModel.DisplayName("System Language Local Selector")]
    [Serializable]
    public class SystemLanguageStartupLocalSelector : IStartupLocaleSelector
    {
        public Locale GetStartupLocale(ILocalesProvider availableLocales)
        {
            return Application.systemLanguage switch
            {
                SystemLanguage.Japanese => GetLocaleByCode(availableLocales, "ja"),
                SystemLanguage.English => GetLocaleByCode(availableLocales, "en"),
                SystemLanguage.ChineseSimplified => GetLocaleByCode(availableLocales, "zh-Hans"),
                _ => null
            };
        }
        
        private Locale GetLocaleByCode(ILocalesProvider availableLocales, string code)
        {
            foreach (var locale in availableLocales.Locales)
            {
                if (locale.Identifier.Code == code)
                {
                    return locale;
                }
            }

            return null;
        }
    }
}