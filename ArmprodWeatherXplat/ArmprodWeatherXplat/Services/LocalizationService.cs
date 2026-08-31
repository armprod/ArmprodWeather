using System;
using System.Globalization;

namespace ArmprodWeatherXplat.Services;

public class LocalizationService
{
    private static readonly string SystemLanguage = 
        CultureInfo.CurrentUICulture.TwoLetterISOLanguageName.Equals("cs", StringComparison.OrdinalIgnoreCase) 
            ? "Czech" 
            : "English";

    public string GetEffectiveLanguage(string selectedLanguage)
    {
        return selectedLanguage == "System" ? SystemLanguage : selectedLanguage;
    }

    // Returns a 2-character language code (e.g. ‘cs’, ‘en’) for use in API calls.
    public string GetApiLanguageCode(string selectedLanguage)
    {
        string effective = GetEffectiveLanguage(selectedLanguage);
        return effective == "Czech" ? "cs" : "en";
    }

    public void ApplyLanguageCulture(string language)
    {
        string effective = GetEffectiveLanguage(language);
        var cultureCode = effective == "Czech" ? "cs-CZ" : "en-US";
        var culture = new CultureInfo(cultureCode);
        CultureInfo.CurrentCulture = culture;
        CultureInfo.CurrentUICulture = culture;
    }
}