using System;
using ArmprodWeatherXplat.Services;

namespace ArmprodWeatherXplat.Helpers;

public static class WeatherMapper
{
    public static string MapCodeToIcon(int code, bool isDay = true) => code switch
    {
        0 => isDay ? "☀️\uFE0F" : "🌙\uFE0F",
        1 or 2 => isDay ? "🌤️\uFE0F" : "🌙\uFE0F",
        3 => "☁️\uFE0F",
        45 or 48 => "🌫️\uFE0F",
        51 or 53 or 55 or 61 or 63 or 65 => "🌧️\uFE0F",
        71 or 73 or 75 => "❄️\uFE0F",
        95 or 96 or 99 => "🌩️\uFE0F",
        _ => "🌡️\uFE0F"
    };

    public static string MapCodeToCondition(int code, bool isCzech) => code switch
    {
        0 => isCzech ? "Jasno" : "Clear",
        1 or 2 => isCzech ? "Skoro jasno" : "Almost clear",
        3 => isCzech ? "Zataženo" : "Cloudy",
        45 or 48 => isCzech ? "Mlha" : "Fog",
        51 or 53 or 55 => isCzech ? "Mrholení" : "Drizzle",
        61 or 63 or 65 => isCzech ? "Déšť" : "Rain",
        71 or 73 or 75 => isCzech ? "Sněžení" : "Snowfall",
        95 or 96 or 99 => isCzech ? "Bouřky" : "Thunderstorm",
        _ => isCzech ? "Proměnlivo" : "Unpredictable weather"
    };

    public static string MapCodeToCondition(int code, string language) 
        => MapCodeToCondition(code, language == "Czech");

    public static string MapWindDirection(int degrees, bool isCzech)
    {
        string[] arrows = { "⬇️\uFE0F", "↙️\uFE0F", "⬅️\uFE0F", "↖️\uFE0F", "⬆️\uFE0F", "↗️\uFE0F", "➡️\uFE0F", "↘️\uFE0F" };
        string[] directions = isCzech 
            ? new[] { "S", "SV", "V", "JV", "J", "JZ", "Z", "SZ" }
            : new[] { "N", "NE", "E", "SE", "S", "SW", "W", "NW" };

        int index = (int)Math.Round((degrees % 360) / 45.0) % 8;
        return $"{arrows[index]} {directions[index]} ({degrees}°)";
    }

    public static string MapWindDirection(int degrees, string language) 
        => MapWindDirection(degrees, language == "Czech");

    public static string GetUvRiskLevel(double uvIndex, bool isCzech) => uvIndex switch
    {
        < 3 => isCzech ? "Nízký" : "Low",
        < 6 => isCzech ? "Střední" : "Moderate",
        < 8 => isCzech ? "Vysoký" : "High",
        < 11 => isCzech ? "Velmi vysoký" : "Very High",
        _ => isCzech ? "Extrémní" : "Extreme"
    };

    public static string GetUvRiskLevel(double uvIndex, string language) 
        => GetUvRiskLevel(uvIndex, language == "Czech");

    public static double ConvertTemp(double celsius, string unit) =>
        unit == "°F" ? (celsius * 1.8 + 32) : celsius;

    public static double ConvertWind(double kmh, string unit) =>
        unit == "mph" ? (kmh * 0.621371) : kmh;

    public static string FormatWindGusts(double gusts, string windUnit, bool isCzech) =>
        isCzech ? $"Nárazy {Math.Round(gusts)} {windUnit}" : $"Gusts {Math.Round(gusts)} {windUnit}";

    public static string FormatDewPoint(double dewPoint, string tempUnit, bool isCzech) =>
        isCzech ? $"Rosný bod {Math.Round(dewPoint)}{tempUnit}" : $"Dew point {Math.Round(dewPoint)}{tempUnit}";

    public static string FormatPrecipAmount(double amount, bool isCzech) =>
        isCzech ? $"{amount:F1} mm očekáváno" : $"{amount:F1} mm expected";

    public static string FormatDaylightDuration(double seconds, bool isCzech)
    {
        var duration = TimeSpan.FromSeconds(seconds);
        return isCzech ? $"Délka dne {duration.Hours}h {duration.Minutes}m" : $"Day length {duration.Hours}h {duration.Minutes}m";
    }

    public static string FormatSunshineDuration(double seconds, bool isCzech)
    {
        var sunshine = TimeSpan.FromSeconds(seconds);
        return isCzech ? $"Svit: {sunshine.Hours}h {sunshine.Minutes}m" : $"Sunshine: {sunshine.Hours}h {sunshine.Minutes}m";
    }

    public static string FormatTempDelta(double temp, double apparentTemp, string tempUnit, bool isCzech)
    {
        double diff = Math.Round(apparentTemp - temp);
        if (diff == 0) 
            return isCzech ? "Stejná jako reálná" : "Matches actual temp";
        
        return diff > 0 
            ? (isCzech ? $"Pocitově o +{diff}{tempUnit} tepleji" : $"Feels +{diff}{tempUnit} warmer")
            : (isCzech ? $"Pocitově o {diff}{tempUnit} chladněji" : $"Feels {diff}{tempUnit} cooler");
    }

    public static string FormatPeakPrecip(int maxProb, string peakTime, bool isCzech)
    {
        if (maxProb <= 10)
            return isCzech ? "Srážky se neočekávají" : "No rain expected";

        return isCzech 
            ? $"Max. riziko {maxProb} % kolem {peakTime}" 
            : $"Peak risk {maxProb} % around {peakTime}";
    }

    public static string FormatPeakUv(double maxUv, string peakTime, bool isCzech)
    {
        if (maxUv < 1.0)
            return isCzech ? "Nízká zátěž po celý den" : "Low intensity all day";

        return isCzech 
            ? $"Maximum kolem {peakTime}" 
            : $"Peak around {peakTime}";
    }

    public static string FormatPressureWithTrend(double currentPressure, double? pastPressure, bool isCzech)
    {
        return $"{Math.Round(currentPressure)} hPa";
    }

    public static string GetPressureAdvice(double currentPressure, double? pastPressure, bool isCzech)
    {
        if (!pastPressure.HasValue) 
            return isCzech ? "Bez data" : "No data";

        double diff = currentPressure - pastPressure.Value;

        if (diff <= -2.0)
            return isCzech ? "⚠️\uFE0F Rychlý pokles" : "⚠️\uFE0F Rapid drop";
        
        if (diff <= -0.8)
            return isCzech ? "📉\uFE0F Mírný pokles" : "📉\uFE0F Slight drop";

        if (diff >= 2.0)
            return isCzech ? "📈\uFE0F Rychlý vzestup" : "📈\uFE0F Rapid rise";

        if (diff >= 0.8)
            return isCzech ? "📈\uFE0F Mírný vzestup" : "📈\uFE0F Slight rise";

        return isCzech ? "➡️\uFE0F Stabilní" : "➡️\uFE0F Steady";
    }

    public static (string Value, string Description) FormatVisibility(double visibilityMeters, string speedOrDistanceUnit, bool isCzech)
    {
        bool isImperial = speedOrDistanceUnit.Contains("mph", StringComparison.OrdinalIgnoreCase) || 
                          speedOrDistanceUnit.Contains("mi", StringComparison.OrdinalIgnoreCase);

        double distance = isImperial ? visibilityMeters / 1609.34 : visibilityMeters / 1000.0;
        string unitSymbol = isImperial ? "mi" : "km";

        string valueText = $"{distance:F1} {unitSymbol}";
        string adviceText;

        double km = visibilityMeters / 1000.0;
        if (km >= 10)
            adviceText = isCzech ? "Vynikající viditelnost" : "Excellent visibility";
        else if (km >= 4)
            adviceText = isCzech ? "Dobrá viditelnost" : "Good visibility";
        else if (km >= 1)
            adviceText = isCzech ? "Snížená viditelnost" : "Moderate visibility";
        else
            adviceText = isCzech ? "Hustá mlha" : "Dense fog";

        return (valueText, adviceText);
    }

    public static (string Value, string Description) FormatCloudCover(int cloudPercent, bool isCzech)
    {
        string valueStr = $"{cloudPercent} %";

        string desc;
        if (cloudPercent <= 10) desc = isCzech ? "☀️\uFE0F Jasno" : "☀️\uFE0F Clear sky";
        else if (cloudPercent <= 30) desc = isCzech ? "🌤️\uFE0F Skoro jasno" : "🌤️\uFE0F Mostly clear";
        else if (cloudPercent <= 70) desc = isCzech ? "⛅\uFE0F Polojasno" : "⛅\uFE0F Partly cloudy";
        else if (cloudPercent <= 90) desc = isCzech ? "🌥️\uFE0F Skoro zataženo" : "🌥️\uFE0F Mostly cloudy";
        else desc = isCzech ? "☁️\uFE0F Zataženo" : "☁️\uFE0F Overcast";

        return (valueStr, desc);
    }

    public static (string IconAndName, string Detail) GetMoonPhaseInfo(double moonPhase, string? moonrise, string? moonset, bool isCzech)
    {
        string name;
        string icon;

        if (moonPhase == 0.0 || moonPhase == 1.0) { icon = "🌑\uFE0F"; name = isCzech ? "Nov" : "New Moon"; }
        else if (moonPhase < 0.25) { icon = "🌒\uFE0F"; name = isCzech ? "Dorůstající srp" : "Waxing Crescent"; }
        else if (moonPhase == 0.25) { icon = "🌓\uFE0F"; name = isCzech ? "První čtvrť" : "First Quarter"; }
        else if (moonPhase < 0.50) { icon = "🌔\uFE0F"; name = isCzech ? "Dorůstající měsíc" : "Waxing Gibbous"; }
        else if (moonPhase == 0.50) { icon = "🌕\uFE0F"; name = isCzech ? "Úplněk" : "Full Moon"; }
        else if (moonPhase < 0.75) { icon = "🌖\uFE0F"; name = isCzech ? "Couvající měsíc" : "Waning Gibbous"; }
        else if (moonPhase == 0.75) { icon = "🌗\uFE0F"; name = isCzech ? "Poslední čtvrť" : "Last Quarter"; }
        else { icon = "🌘\uFE0F"; name = isCzech ? "Ubývající srp" : "Waning Crescent"; }

        string risesetText = (moonrise != null && moonset != null) 
            ? $"↑ {moonrise}  ↓ {moonset}" 
            : "";

        return ($"{name} {icon}", risesetText);
    }

    public static (string Value, string Description) FormatAqi(int aqiValue, bool isCzech)
    {
        string valueStr = $"{aqiValue} AQI";
        string desc;

        if (aqiValue <= 50) desc = isCzech ? "🟢\uFE0F Skvělá (Čistý vzduch)" : "🟢\uFE0F Good";
        else if (aqiValue <= 100) desc = isCzech ? "🟡\uFE0F Střední (Akceptovatelná)" : "🟡\uFE0F Moderate";
        else if (aqiValue <= 150) desc = isCzech ? "🟠\uFE0F Citlivé skupiny" : "🟠\uFE0F Unhealthy for Sensitive";
        else if (aqiValue <= 200) desc = isCzech ? "🔴\uFE0F Nezdravá" : "🔴\uFE0F Unhealthy";
        else desc = isCzech ? "🟣\uFE0F Velmi špatná" : "🟣\uFE0F Very Unhealthy";

        return (valueStr, desc);
    }

    public static string GetTimeFormat(TimeFormatSetting setting, bool isCzech, bool includeMinutes = true)
    {
        return setting switch
        {
            TimeFormatSetting.TwentyFourHour => includeMinutes ? "HH:mm" : "HH:00",
            TimeFormatSetting.TwelveHour => includeMinutes ? "h:mm tt" : "h tt",
            _ => includeMinutes 
                ? (isCzech ? "HH:mm" : "h:mm tt") 
                : (isCzech ? "HH:00" : "h tt")
        };
    }

    public static string FormatTime(string? isoTimeStr, TimeFormatSetting timeFormat, bool isCzech)
    {
        if (string.IsNullOrEmpty(isoTimeStr) || !DateTime.TryParse(isoTimeStr, out var dt))
            return "--:--";

        var culture = isCzech ? new System.Globalization.CultureInfo("cs-CZ") : new System.Globalization.CultureInfo("en-US");
        string format = GetTimeFormat(timeFormat, isCzech, includeMinutes: true);

        return dt.ToString(format, culture);
    }
}