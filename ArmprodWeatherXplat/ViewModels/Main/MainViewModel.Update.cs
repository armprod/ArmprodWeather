using System;
using System.Globalization;
using System.Linq;
using ArmprodWeatherXplat.Helpers;
using ArmprodWeatherXplat.Models;
using ArmprodWeatherXplat.Services;

namespace ArmprodWeatherXplat.ViewModels;

public partial class MainViewModel
{
    private void UpdateLocalizedTexts()
    {
        bool isCzech = _localizationService.GetEffectiveLanguage(Settings.SelectedLanguage) == "Czech";

        LocalTimeHeader = isCzech ? "Místní čas" : "Local time";
        HourlyForecastHeader = isCzech ? "Hodinová předpověď" : "Hourly forecast";
        DailyForecastHeader = isCzech ? "7denní předpověď" : "7-day forecast";
        WindHeader = isCzech ? "💨 Vítr" : "💨 Wind";
        HumidityHeader = isCzech ? "💧 Vlhkost" : "💧 Humidity";
        ApparentTempHeader = isCzech ? "🌡️ Pocitová teplota" : "🌡️ Feels like";
        PressureHeader = isCzech ? "⏲️ Tlak vzduchu" : "⏲️ Pressure";
        UvIndexHeader = isCzech ? "☀️ Max. UV Index" : "☀️ Max UV Index";
        PrecipProbHeader = isCzech ? "🌧️ Očekávanost srážek" : "🌧️ Rain chance";
        SunriseHeader = isCzech ? "🌅 Východ slunce" : "🌅 Sunrise";
        SunsetHeader = isCzech ? "🌇 Západ slunce" : "🌇 Sunset";
        VisibilityHeader = isCzech ? "👁️ Viditelnost" : "👁️ Visibility";
        CloudCoverHeader = isCzech ? "☁️ Oblačnost" : "☁️ Cloud cover";
        AqiHeader = isCzech ? "🍃 Kvalita ovzduší" : "🍃 Air Quality";
        MoonHeader = isCzech ? "🌙 Měsíc" : "🌙 Moon";

        OfflineHeader = isCzech ? "Jste v offline režimu" : "You are offline";

        Search.SearchPlaceholder = isCzech ? "Zadejte název města..." : "Enter city name...";
        Settings.UpdateLocalizedTexts();

        var settings = _settingsService.LoadSettings();
        UpdateLastUpdatedText(settings.LastUpdated);

        if (HasError)
        {
            ErrorMessage = isCzech ? "Chybí připojení k internetu nebo se nepodařilo načíst data." : "No internet connection or failed to load data.";
        }
    }

    private void UpdateUI(WeatherResponse? weather)
    {
        if (weather?.Current == null) return;
        _lastWeather = weather;

        string language = _localizationService.GetEffectiveLanguage(Settings.SelectedLanguage);
        bool isCzech = language == "Czech";
        var culture = isCzech ? new CultureInfo("cs-CZ") : new CultureInfo("en-US");
        string tempUnit = Settings.GetEffectiveTemperatureUnit();
        string windUnit = Settings.GetEffectiveWindSpeedUnit();

        var timeFormatSetting = Settings.SelectedTimeFormat; 

        UpdateHeaderSection(weather, language, isCzech, culture, tempUnit, timeFormatSetting);

        UpdateGridCards(weather, language, isCzech, tempUnit, windUnit, timeFormatSetting);

        AnalyzeHourlyPeaks(weather, isCzech);
        UpdateHourlyForecast(weather, isCzech, tempUnit, timeFormatSetting);
        UpdateDailyForecast(weather, isCzech, culture, tempUnit);
    }

    private void UpdateHeaderSection(
    WeatherResponse weather, 
    string language, 
    bool isCzech, 
    CultureInfo culture, 
    string tempUnit, 
    TimeFormatSetting timeFormat)
    {
        var current = weather.Current!;

        DateTime cityNow = DateTime.UtcNow.AddSeconds(weather.UtcOffsetSeconds);
        string dayName = cityNow.ToString("ddd", culture);
        if (isCzech && dayName.Length > 0) dayName = char.ToUpper(dayName[0]) + dayName[1..];
        
        string timeFormatStr = WeatherMapper.GetTimeFormat(timeFormat, isCzech, includeMinutes: true);
        LocalTimeText = $"{dayName} {cityNow.ToString(timeFormatStr, culture)}";

        bool isDay = current.IsDay == 1;
        WeatherCondition = WeatherMapper.MapCodeToCondition(current.WeatherCode, language);
        WeatherIcon = WeatherMapper.MapCodeToIcon(current.WeatherCode, isDay);

        double currentTemp = WeatherMapper.ConvertTemp(current.Temperature, Settings.SelectedTemperatureUnit);
        CurrentTemperature = $"{Math.Round(currentTemp)}{tempUnit}";

        if (weather.Daily?.TempMax is { Count: > 0 } && weather.Daily?.TempMin is { Count: > 0 })
        {
            string highLabel = isCzech ? "V" : "H";
            string lowLabel = isCzech ? "N" : "L";
            double maxTemp = WeatherMapper.ConvertTemp(weather.Daily.TempMax[0], Settings.SelectedTemperatureUnit);
            double minTemp = WeatherMapper.ConvertTemp(weather.Daily.TempMin[0], Settings.SelectedTemperatureUnit);
            TempRange = $"{highLabel}: {Math.Round(maxTemp)}{tempUnit}  |  {lowLabel}: {Math.Round(minTemp)}{tempUnit}";
        }
    }

    private void UpdateLastUpdatedText(DateTime lastUpdated)
    {
        if (lastUpdated == default)
        {
            LastUpdatedText = string.Empty;
            OfflineMessage = string.Empty;
            return;
        }

        bool isCzech = _localizationService.GetEffectiveLanguage(Settings.SelectedLanguage) == "Czech";
        var culture = isCzech ? new CultureInfo("cs-CZ") : new CultureInfo("en-US");

        string timeStr = lastUpdated.ToString("t", culture); 
        LastUpdatedText = timeStr;

        if (IsOffline)
        {
            if (_lastFailedRefreshAttempt.HasValue)
            {
                string failTime = _lastFailedRefreshAttempt.Value.ToString("t", culture);
                OfflineMessage = isCzech 
                    ? $"Obnovení v {failTime} selhalo • Data z {timeStr}" 
                    : $"Refresh at {failTime} failed • Cached {timeStr}";
            }
            else
            {
                OfflineMessage = isCzech 
                    ? $"Zobrazena neaktuální data ({timeStr})" 
                    : $"Showing cached data ({timeStr})";
            }
        }
        else
        {
            OfflineMessage = string.Empty;
        }
    }

    private void UpdateGridCards(
    WeatherResponse weather, 
    string language, 
    bool isCzech, 
    string tempUnit, 
    string windUnit, 
    TimeFormatSetting timeFormat)
    {
        var current = weather.Current!;
        var daily = weather.Daily;

        double speed = WeatherMapper.ConvertWind(current.WindSpeed, Settings.SelectedWindSpeedUnit);
        double gusts = WeatherMapper.ConvertWind(current.WindGusts, Settings.SelectedWindSpeedUnit);
        WindSpeed = $"{Math.Round(speed)} {windUnit}";
        WindGustsText = WeatherMapper.FormatWindGusts(gusts, windUnit, isCzech);
        WindDirectionText = WeatherMapper.MapWindDirection(current.WindDirection, language);

        Humidity = $"{current.Humidity} %";
        DewPointText = WeatherMapper.FormatDewPoint(WeatherMapper.ConvertTemp(current.DewPoint, Settings.SelectedTemperatureUnit), tempUnit, isCzech);

        double apparentTemp = WeatherMapper.ConvertTemp(current.ApparentTemperature, Settings.SelectedTemperatureUnit);
        double currentTemp = WeatherMapper.ConvertTemp(current.Temperature, Settings.SelectedTemperatureUnit);
        ApparentTempText = $"{Math.Round(apparentTemp)}{tempUnit}";
        TempDeltaText = WeatherMapper.FormatTempDelta(currentTemp, apparentTemp, tempUnit, isCzech);

        double? pastPressure = GetPastPressure(weather, 3);
        PressureText = WeatherMapper.FormatPressureWithTrend(current.SurfacePressure, pastPressure, isCzech);
        PressureAdviceText = WeatherMapper.GetPressureAdvice(current.SurfacePressure, pastPressure, isCzech);

        if (daily?.UvIndexMax is { Count: > 0 })
        {
            double uv = daily.UvIndexMax[0];
            UvIndexText = $"{uv:F1}";
            UvRiskText = isCzech ? $"Riziko: {WeatherMapper.GetUvRiskLevel(uv, language)}" : $"Risk: {WeatherMapper.GetUvRiskLevel(uv, language)}";
        }

        PrecipProbText = daily?.PrecipitationProbabilityMax is { Count: > 0 } ? $"{daily.PrecipitationProbabilityMax[0]} %" : "-- %";
        PrecipAmountText = daily?.PrecipitationSum is { Count: > 0 } ? WeatherMapper.FormatPrecipAmount(daily.PrecipitationSum[0], isCzech) : "--";

        var (visValue, visDesc) = WeatherMapper.FormatVisibility(current.Visibility, windUnit, isCzech);
        VisibilityText = visValue;
        VisibilityAdviceText = visDesc;

        var (cloudValue, cloudDesc) = WeatherMapper.FormatCloudCover(current.CloudCover, isCzech);
        CloudCoverText = cloudValue;
        CloudCoverAdviceText = cloudDesc;

        SunriseText = daily?.Sunrise is { Count: > 0 } 
            ? WeatherMapper.FormatTime(daily.Sunrise[0], timeFormat, isCzech) 
            : "--:--";
            
        SunsetText = daily?.Sunset is { Count: > 0 } 
            ? WeatherMapper.FormatTime(daily.Sunset[0], timeFormat, isCzech) 
            : "--:--";

        DaylightDurationText = daily?.DaylightDuration is { Count: > 0 } ? WeatherMapper.FormatDaylightDuration(daily.DaylightDuration[0], isCzech) : "--";
        SunshineText = daily?.SunshineDuration is { Count: > 0 } ? WeatherMapper.FormatSunshineDuration(daily.SunshineDuration[0], isCzech) : "--";

        if (weather.AirQuality != null)
        {
            var (aqiValue, aqiDesc) = WeatherMapper.FormatAqi(weather.AirQuality.UsAqi, isCzech);
            AqiText = aqiValue;
            AqiAdviceText = aqiDesc;
        }
        else
        {
            AqiText = "--";
            AqiAdviceText = isCzech ? "Nedostupné" : "Unavailable";
        }

        if (daily?.MoonPhase is { Count: > 0 })
        {
            string? moonrise = daily.Moonrise is { Count: > 0 } 
                ? WeatherMapper.FormatTime(daily.Moonrise[0], timeFormat, isCzech) 
                : null;
                
            string? moonset = daily.Moonset is { Count: > 0 } 
                ? WeatherMapper.FormatTime(daily.Moonset[0], timeFormat, isCzech) 
                : null;

            var (moonPhaseName, moonTimes) = WeatherMapper.GetMoonPhaseInfo(daily.MoonPhase[0], moonrise, moonset, isCzech);
            MoonPhaseText = moonPhaseName;
            MoonRiseSetText = moonTimes;
        }
    }

    private void AnalyzeHourlyPeaks(WeatherResponse weather, bool isCzech)
    {
        if (weather.Hourly?.Time == null) return;

        DateTime cityNow = DateTime.UtcNow.AddSeconds(weather.UtcOffsetSeconds);

        int startIdx = 0;
        for (int i = 0; i < weather.Hourly.Time.Count; i++)
        {
            if (DateTime.TryParse(weather.Hourly.Time[i], out var t) 
                && t.Date == cityNow.Date 
                && t.Hour == cityNow.Hour)
            {
                startIdx = i;
                break;
            }
        }

        int maxRainProb = 0;
        string maxRainTime = "--:--";
        double maxUv = 0;
        string maxUvTime = "--:--";

        int limit = Math.Min(startIdx + 24, weather.Hourly.Time.Count);

        for (int i = startIdx; i < limit; i++)
        {
            if (weather.Hourly.PrecipitationProbability != null && weather.Hourly.PrecipitationProbability.Count > i)
            {
                if (weather.Hourly.PrecipitationProbability[i] > maxRainProb)
                {
                    maxRainProb = weather.Hourly.PrecipitationProbability[i];
                    if (DateTime.TryParse(weather.Hourly.Time[i], out var dt)) maxRainTime = dt.ToString("HH:mm");
                }
            }

            if (weather.Hourly.UvIndex != null && weather.Hourly.UvIndex.Count > i)
            {
                if (weather.Hourly.UvIndex[i] > maxUv)
                {
                    maxUv = weather.Hourly.UvIndex[i];
                    if (DateTime.TryParse(weather.Hourly.Time[i], out var dt)) maxUvTime = dt.ToString("HH:mm");
                }
            }
        }

        HourlyPeakPrecipText = WeatherMapper.FormatPeakPrecip(maxRainProb, maxRainTime, isCzech);
        PeakUvTimeText = WeatherMapper.FormatPeakUv(maxUv, maxUvTime, isCzech);
    }

    private void UpdateHourlyForecast(
    WeatherResponse weather, 
    bool isCzech, 
    string tempUnit, 
    TimeFormatSetting timeFormat)
    {
        HourlyForecast.Clear();
        if (weather.Hourly?.Time == null || weather.Hourly.WeatherCode == null || weather.Hourly.Temperature == null) return;

        int availableCount = Math.Min(weather.Hourly.Time.Count, Math.Min(weather.Hourly.WeatherCode.Count, weather.Hourly.Temperature.Count));
        DateTime cityNow = DateTime.UtcNow.AddSeconds(weather.UtcOffsetSeconds);

        int startIdx = 0;
        for (int i = 0; i < availableCount; i++)
        {
            if (DateTime.TryParse(weather.Hourly.Time[i], out var t) 
                && t.Date == cityNow.Date 
                && t.Hour == cityNow.Hour)
            {
                startIdx = i;
                break;
            }
        }

        var culture = isCzech ? new System.Globalization.CultureInfo("cs-CZ") : new System.Globalization.CultureInfo("en-US");
        string hourlyTimeFormat = WeatherMapper.GetTimeFormat(timeFormat, isCzech, includeMinutes: false);

        int maxItems = Math.Min(startIdx + 24, availableCount);
        for (int i = startIdx; i < maxItems; i++)
        {
            if (!DateTime.TryParse(weather.Hourly.Time[i], out var dt)) continue;

            string timeLabel = (i == startIdx) 
                ? (isCzech ? "Teď" : "Now") 
                : dt.ToString(hourlyTimeFormat, culture);

            double hourlyTemp = WeatherMapper.ConvertTemp(weather.Hourly.Temperature[i], Settings.SelectedTemperatureUnit);
            double hourlyApparent = weather.Hourly.ApparentTemperature != null && weather.Hourly.ApparentTemperature.Count > i
                ? WeatherMapper.ConvertTemp(weather.Hourly.ApparentTemperature[i], Settings.SelectedTemperatureUnit)
                : hourlyTemp;

            int rainProb = weather.Hourly.PrecipitationProbability != null && weather.Hourly.PrecipitationProbability.Count > i 
                ? weather.Hourly.PrecipitationProbability[i] 
                : 0;

            bool isHourlyDaytime = weather.Hourly.IsDay != null && weather.Hourly.IsDay.Count > i 
                ? weather.Hourly.IsDay[i] == 1 
                : (dt.Hour >= 6 && dt.Hour < 20);

            HourlyForecast.Add(new HourlyItem(
                timeLabel, 
                WeatherMapper.MapCodeToIcon(weather.Hourly.WeatherCode[i], isHourlyDaytime),
                $"{Math.Round(hourlyTemp)}{tempUnit}",
                $"{Math.Round(hourlyApparent)}{tempUnit}",
                $"{rainProb} %",
                rainProb > 15
            ));
        }
    }

    private void UpdateDailyForecast(WeatherResponse weather, bool isCzech, CultureInfo culture, string tempUnit)
    {
        DailyForecast.Clear();
        if (weather.Daily?.Time == null || weather.Daily.WeatherCode == null || weather.Daily.TempMin == null || weather.Daily.TempMax == null) return;

        int availableCount = Math.Min(weather.Daily.Time.Count, Math.Min(weather.Daily.WeatherCode.Count, Math.Min(weather.Daily.TempMin.Count, weather.Daily.TempMax.Count)));

        for (int i = 0; i < availableCount; i++)
        {
            if (!DateTime.TryParse(weather.Daily.Time[i], out var dt)) continue;

            string dayName = (i == 0) ? (isCzech ? "Dnes" : "Today") : dt.ToString("ddd", culture);
            if (isCzech && dayName is { Length: > 0 }) dayName = char.ToUpper(dayName[0]) + dayName[1..];

            double minDaily = WeatherMapper.ConvertTemp(weather.Daily.TempMin[i], Settings.SelectedTemperatureUnit);
            double maxDaily = WeatherMapper.ConvertTemp(weather.Daily.TempMax[i], Settings.SelectedTemperatureUnit);

            DailyForecast.Add(new DailyItem(
                dayName, 
                WeatherMapper.MapCodeToIcon(weather.Daily.WeatherCode[i], true),
                $"{Math.Round(minDaily)}{tempUnit} / {Math.Round(maxDaily)}{tempUnit}"
            ));
        }
    }

    private double? GetPastPressure(WeatherResponse weather, int hoursAgo)
    {
        if (weather.Hourly?.SurfacePressure == null || weather.Hourly.Time == null) return null;

        DateTime cityNow = DateTime.UtcNow.AddSeconds(weather.UtcOffsetSeconds);

        int currentIdx = weather.Hourly.Time.FindIndex(t => 
            DateTime.TryParse(t, out var dt) && dt.Hour == cityNow.Hour && dt.Date == cityNow.Date);

        if (currentIdx >= hoursAgo && weather.Hourly.SurfacePressure.Count > currentIdx - hoursAgo)
        {
            return weather.Hourly.SurfacePressure[currentIdx - hoursAgo];
        }

        return null;
    }
}