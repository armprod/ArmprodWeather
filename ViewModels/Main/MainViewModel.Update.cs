using System;
using System.Globalization;
using ArmprodWeather.Helpers;
using ArmprodWeather.Models;

namespace ArmprodWeather.ViewModels;

public partial class MainViewModel
{
    private void UpdateLocalizedTexts()
    {
        bool isCzech = _localizationService.GetEffectiveLanguage(Settings.SelectedLanguage) == "Czech";

        HourlyForecastHeader = isCzech ? "Hodinová předpověď" : "Hourly forecast";
        DailyForecastHeader = isCzech ? "7denní předpověď" : "7-day forecast";
        LocalTimeHeader = isCzech ? "Místní čas" : "Local time";
        WindHeader = isCzech ? "💨 Vítr" : "💨 Wind";
        HumidityHeader = isCzech ? "💧 Vlhkost" : "💧 Humidity";
        ApparentTempHeader = isCzech ? "🌡️ Pocitová teplota" : "🌡️ Feels like";
        PressureHeader = isCzech ? "⏲️ Tlak vzduchu" : "⏲️ Pressure";
        UvIndexHeader = isCzech ? "☀️ Max. UV Index" : "☀️ Max UV Index";
        PrecipProbHeader = isCzech ? "🌧️ Očekávanost srážek" : "🌧️ Rain chance";
        SunriseHeader = isCzech ? "🌅 Východ slunce" : "🌅 Sunrise";
        SunsetHeader = isCzech ? "🌇 Západ slunce" : "🌇 Sunset";
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
        string effectiveLanguage = _localizationService.GetEffectiveLanguage(Settings.SelectedLanguage);
        bool isCzech = effectiveLanguage == "Czech";
        var culture = isCzech ? new CultureInfo("cs-CZ") : new CultureInfo("en-US");

        string tempUnit = Settings.GetEffectiveTemperatureUnit();
        string windUnit = Settings.GetEffectiveWindSpeedUnit();

        // 1. Základní meteo hodnoty & Pocitový teplotní rozdíl
        double currentTemp = WeatherPresentationMapper.ConvertTemp(weather.Current.Temperature, Settings.SelectedTemperatureUnit);
        double apparentTemp = WeatherPresentationMapper.ConvertTemp(weather.Current.ApparentTemperature, Settings.SelectedTemperatureUnit);

        CurrentTemperature = $"{Math.Round(currentTemp)}{tempUnit}";
        ApparentTempText = $"{Math.Round(apparentTemp)}{tempUnit}";
        TempDeltaText = WeatherPresentationMapper.FormatTempDelta(currentTemp, apparentTemp, tempUnit, isCzech);

        PressureText = $"{Math.Round(weather.Current.SurfacePressure)} hPa";
        Humidity = $"{weather.Current.Humidity} %";

        // 2. Vítr
        double speed = WeatherPresentationMapper.ConvertWind(weather.Current.WindSpeed, Settings.SelectedWindSpeedUnit);
        double gusts = WeatherPresentationMapper.ConvertWind(weather.Current.WindGusts, Settings.SelectedWindSpeedUnit);
        WindSpeed = $"{Math.Round(speed)} {windUnit}";
        WindGustsText = WeatherPresentationMapper.FormatWindGusts(gusts, windUnit, isCzech);
        WindDirectionText = WeatherMapper.MapWindDirection(weather.Current.WindDirection, effectiveLanguage);

        // 3. Ikona & stav
        bool isDay = weather.Current.IsDay == 1;
        WeatherCondition = WeatherMapper.MapCodeToCondition(weather.Current.WeatherCode, effectiveLanguage);
        WeatherIcon = WeatherMapper.MapCodeToIcon(weather.Current.WeatherCode, isDay);

        // 4. Místní čas
        if (DateTime.TryParse(weather.Current.Time, out var localDt))
        {
            string dayName = localDt.ToString("ddd", culture);
            if (isCzech && dayName.Length > 0) dayName = char.ToUpper(dayName[0]) + dayName[1..];
            LocalTimeText = $"{dayName} {localDt:HH:mm}";
        }
        else LocalTimeText = "--:--";

        // 5. Doplňkové metriky
        DewPointText = WeatherPresentationMapper.FormatDewPoint(WeatherPresentationMapper.ConvertTemp(weather.Current.DewPoint, Settings.SelectedTemperatureUnit), tempUnit, isCzech);
        CloudCoverText = WeatherPresentationMapper.FormatCloudCover(weather.Current.CloudCover, isCzech);
        VisibilityText = WeatherPresentationMapper.FormatVisibility(weather.Current.Visibility, isCzech);

        // 6. Analýza hodinových špiček (Srážky & UV Index na 24h)
        AnalyzeHourlyPeaks(weather, isCzech);

        // 7. UV Index & Riziko
        if (weather.Daily?.UvIndexMax is { Count: > 0 })
        {
            double uv = weather.Daily.UvIndexMax[0];
            UvIndexText = $"{uv:F1}";
            UvRiskText = isCzech ? $"Riziko: {WeatherMapper.GetUvRiskLevel(uv, effectiveLanguage)}" : $"Risk: {WeatherMapper.GetUvRiskLevel(uv, effectiveLanguage)}";
        }

        // 8. Srážky
        PrecipProbText = weather.Daily?.PrecipitationProbabilityMax is { Count: > 0 } ? $"{weather.Daily.PrecipitationProbabilityMax[0]} %" : "-- %";
        PrecipAmountText = weather.Daily?.PrecipitationSum is { Count: > 0 } ? WeatherPresentationMapper.FormatPrecipAmount(weather.Daily.PrecipitationSum[0], isCzech) : "--";

        // 9. Astronomické údaje
        SunriseText = weather.Daily?.Sunrise is { Count: > 0 } ? WeatherPresentationMapper.FormatIsoTime(weather.Daily.Sunrise[0]) : "--:--";
        SunsetText = weather.Daily?.Sunset is { Count: > 0 } ? WeatherPresentationMapper.FormatIsoTime(weather.Daily.Sunset[0]) : "--:--";
        DaylightDurationText = weather.Daily?.DaylightDuration is { Count: > 0 } ? WeatherPresentationMapper.FormatDaylightDuration(weather.Daily.DaylightDuration[0], isCzech) : "--";
        SunshineText = weather.Daily?.SunshineDuration is { Count: > 0 } ? WeatherPresentationMapper.FormatSunshineDuration(weather.Daily.SunshineDuration[0], isCzech) : "--";

        // 10. Teplotní rozsah
        if (weather.Daily?.TempMax is { Count: > 0 } && weather.Daily?.TempMin is { Count: > 0 })
        {
            string highLabel = isCzech ? "V" : "H";
            string lowLabel = isCzech ? "N" : "L";
            double maxTemp = WeatherPresentationMapper.ConvertTemp(weather.Daily.TempMax[0], Settings.SelectedTemperatureUnit);
            double minTemp = WeatherPresentationMapper.ConvertTemp(weather.Daily.TempMin[0], Settings.SelectedTemperatureUnit);
            TempRange = $"{highLabel}: {Math.Round(maxTemp)}{tempUnit}  |  {lowLabel}: {Math.Round(minTemp)}{tempUnit}";
        }

        UpdateHourlyForecast(weather, isCzech, tempUnit);
        UpdateDailyForecast(weather, isCzech, culture, tempUnit);
    }

    // Pomocná metoda pro výpočet špiček srážek a UV v příštích 24h
    private void AnalyzeHourlyPeaks(WeatherResponse weather, bool isCzech)
    {
        if (weather.Hourly?.Time == null) return;

        int maxRainProb = 0;
        string maxRainTime = "--:--";
        double maxUv = 0;
        string maxUvTime = "--:--";

        int limit = Math.Min(24, weather.Hourly.Time.Count);

        for (int i = 0; i < limit; i++)
        {
            // Srážky
            if (weather.Hourly.PrecipitationProbability != null && weather.Hourly.PrecipitationProbability.Count > i)
            {
                if (weather.Hourly.PrecipitationProbability[i] > maxRainProb)
                {
                    maxRainProb = weather.Hourly.PrecipitationProbability[i];
                    if (DateTime.TryParse(weather.Hourly.Time[i], out var dt)) maxRainTime = dt.ToString("HH:mm");
                }
            }

            // UV Index
            if (weather.Hourly.UvIndex != null && weather.Hourly.UvIndex.Count > i)
            {
                if (weather.Hourly.UvIndex[i] > maxUv)
                {
                    maxUv = weather.Hourly.UvIndex[i];
                    if (DateTime.TryParse(weather.Hourly.Time[i], out var dt)) maxUvTime = dt.ToString("HH:mm");
                }
            }
        }

        HourlyPeakPrecipText = WeatherPresentationMapper.FormatPeakPrecip(maxRainProb, maxRainTime, isCzech);
        PeakUvTimeText = WeatherPresentationMapper.FormatPeakUv(maxUv, maxUvTime, isCzech);
    }

    private void UpdateHourlyForecast(WeatherResponse weather, bool isCzech, string tempUnit)
    {
        HourlyForecast.Clear();
        if (weather.Hourly?.Time == null || weather.Hourly.WeatherCode == null || weather.Hourly.Temperature == null) return;

        int availableCount = Math.Min(weather.Hourly.Time.Count, Math.Min(weather.Hourly.WeatherCode.Count, weather.Hourly.Temperature.Count));
        int startIdx = 0;
        DateTime now = DateTime.Now;

        for (int i = 0; i < availableCount; i++)
        {
            if (DateTime.TryParse(weather.Hourly.Time[i], out var t) && t.Hour == now.Hour && t.Date == now.Date)
            {
                startIdx = i;
                break;
            }
        }

        int maxItems = Math.Min(startIdx + 24, availableCount);
        for (int i = startIdx; i < maxItems; i++)
        {
            if (!DateTime.TryParse(weather.Hourly.Time[i], out var dt)) continue;

            string timeLabel = (i == startIdx) ? (isCzech ? "Teď" : "Now") : dt.ToString("HH:mm");
            double hourlyTemp = WeatherPresentationMapper.ConvertTemp(weather.Hourly.Temperature[i], Settings.SelectedTemperatureUnit);
            double hourlyApparent = weather.Hourly.ApparentTemperature != null && weather.Hourly.ApparentTemperature.Count > i
                ? WeatherPresentationMapper.ConvertTemp(weather.Hourly.ApparentTemperature[i], Settings.SelectedTemperatureUnit)
                : hourlyTemp;
            
            int rainProb = weather.Hourly.PrecipitationProbability != null && weather.Hourly.PrecipitationProbability.Count > i 
                ? weather.Hourly.PrecipitationProbability[i] 
                : 0;

            bool isHourlyDaytime = dt.Hour >= 6 && dt.Hour < 22;

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

            double minDaily = WeatherPresentationMapper.ConvertTemp(weather.Daily.TempMin[i], Settings.SelectedTemperatureUnit);
            double maxDaily = WeatherPresentationMapper.ConvertTemp(weather.Daily.TempMax[i], Settings.SelectedTemperatureUnit);

            DailyForecast.Add(new DailyItem(
                dayName, 
                WeatherMapper.MapCodeToIcon(weather.Daily.WeatherCode[i], true),
                $"{Math.Round(minDaily)}{tempUnit} / {Math.Round(maxDaily)}{tempUnit}"
            ));
        }
    }
}