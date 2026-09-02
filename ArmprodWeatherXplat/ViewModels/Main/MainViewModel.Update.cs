using System;
using System.Collections.Generic;
using System.Globalization;
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

    private void UpdateUI(WeatherResponse weather)
    {
        if (weather == null) return;

        string language = _localizationService.GetEffectiveLanguage(Settings.SelectedLanguage);
        bool isCzech = language == "Czech";
        
        var culture = isCzech ? new CultureInfo("cs-CZ") : new CultureInfo("en-US");

        string tempUnit = Settings.GetEffectiveTemperatureUnit();
        string windUnit = Settings.GetEffectiveWindSpeedUnit();
        var timeFormat = Settings.GetEffectiveTimeFormat();

        UpdateHeaderSection(weather, language, isCzech, culture, tempUnit, timeFormat);
        UpdateGridCards(weather, language, isCzech, tempUnit, windUnit, timeFormat);
        AnalyzeHourlyPeaks(weather, isCzech);

        if (weather.Hourly?.Time != null)
        {
            var hourlyData = new List<(string Time, string Icon, string Temp, string ApparentTemp, string PrecipProb, bool HasRain)>();
            int limit = Math.Min(weather.Hourly.Time.Count, 24);

            for (int i = 0; i < limit; i++)
            {
                string rawTime = weather.Hourly.Time[i];
                double temp = weather.Hourly.Temperature?[i] ?? 0;
                double appTemp = weather.Hourly.ApparentTemperature?[i] ?? 0;
                int precip = weather.Hourly.PrecipitationProbability?[i] ?? 0;
                int weatherCode = weather.Hourly.WeatherCode?[i] ?? 0;
                bool isDay = weather.Hourly.IsDay?[i] == 1;

                string formattedTime = DateTime.TryParse(rawTime, out var dt)
                    ? dt.ToString(WeatherMapper.GetTimeFormat(timeFormat, isCzech, includeMinutes: false), culture)
                    : rawTime;

                double convertedTemp = WeatherMapper.ConvertTemp(temp, Settings.SelectedTemperatureUnit);
                double convertedAppTemp = WeatherMapper.ConvertTemp(appTemp, Settings.SelectedTemperatureUnit);

                hourlyData.Add((
                    Time: formattedTime,
                    Icon: WeatherMapper.MapCodeToIcon(weatherCode, isDay),
                    Temp: $"{Math.Round(convertedTemp)}{tempUnit}",
                    ApparentTemp: $"{Math.Round(convertedAppTemp)}{tempUnit}",
                    PrecipProb: $"{precip} %",
                    HasRain: precip > 20
                ));
            }
            UpdateHourlyForecastItems(hourlyData);
        }

        if (weather.Daily?.Time != null)
        {
            var dailyData = new List<(string Day, string Icon, string TempRange)>();
            for (int i = 0; i < weather.Daily.Time.Count; i++)
            {
                string rawDate = weather.Daily.Time[i];
                double max = weather.Daily.TempMax?[i] ?? 0;
                double min = weather.Daily.TempMin?[i] ?? 0;
                int weatherCode = weather.Daily.WeatherCode?[i] ?? 0;

                string dayName = "--";
                if (DateTime.TryParse(rawDate, out var dt))
                {
                    dayName = dt.ToString("ddd d.M.", culture);
                    if (isCzech && dayName.Length > 0) dayName = char.ToUpper(dayName[0]) + dayName[1..];
                }

                double convertedMax = WeatherMapper.ConvertTemp(max, Settings.SelectedTemperatureUnit);
                double convertedMin = WeatherMapper.ConvertTemp(min, Settings.SelectedTemperatureUnit);

                dailyData.Add((
                    Day: dayName,
                    Icon: WeatherMapper.MapCodeToIcon(weatherCode, isDay: true),
                    TempRange: $"{Math.Round(convertedMax)}{tempUnit} / {Math.Round(convertedMin)}{tempUnit}"
                ));
            }
            UpdateDailyForecastItems(dailyData);
        }
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

    private void UpdateHourlyForecastItems(List<(string Time, string Icon, string Temp, string ApparentTemp, string PrecipProb, bool HasRain)> newData)
    {
        while (HourlyForecast.Count < newData.Count)
        {
            HourlyForecast.Add(new HourlyItem());
        }
        while (HourlyForecast.Count > newData.Count)
        {
            HourlyForecast.RemoveAt(HourlyForecast.Count - 1);
        }

        for (int i = 0; i < newData.Count; i++)
        {
            var item = HourlyForecast[i];
            var data = newData[i];

            item.Time = data.Time;
            item.Icon = data.Icon;
            item.Temp = data.Temp;
            item.ApparentTemp = data.ApparentTemp;
            item.PrecipProb = data.PrecipProb;
            item.HasRain = data.HasRain;
        }
    }

    private void UpdateDailyForecastItems(List<(string Day, string Icon, string TempRange)> newData)
    {
        while (DailyForecast.Count < newData.Count)
        {
            DailyForecast.Add(new DailyItem());
        }
        while (DailyForecast.Count > newData.Count)
        {
            DailyForecast.RemoveAt(DailyForecast.Count - 1);
        }

        for (int i = 0; i < newData.Count; i++)
        {
            var item = DailyForecast[i];
            var data = newData[i];

            item.Day = data.Day;
            item.Icon = data.Icon;
            item.TempRange = data.TempRange;
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