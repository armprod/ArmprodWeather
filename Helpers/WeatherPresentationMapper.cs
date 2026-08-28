using System;

namespace ArmprodWeather.Helpers;

public static class WeatherPresentationMapper
{
    public static double ConvertTemp(double celsius, string unit) =>
        unit == "°F" ? (celsius * 1.8 + 32) : celsius;

    public static double ConvertWind(double kmh, string unit) =>
        unit == "mph" ? (kmh * 0.621371) : kmh;

    public static string FormatIsoTime(string? isoDateTime) =>
        DateTime.TryParse(isoDateTime, out var parsed) ? parsed.ToString("HH:mm") : "--:--";

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
            ? $"Meximum ({maxUv:F1}) kolem {peakTime}" 
            : $"Peak ({maxUv:F1}) around {peakTime}";
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
            return isCzech ? "⚠️ Rychlý pokles" : "⚠️ Rapid drop";
        
        if (diff <= -0.8)
            return isCzech ? "📉 Mírný pokles" : "📉 Slight drop";

        if (diff >= 2.0)
            return isCzech ? "📈 Rychlý vzestup" : "📈 Rapid rise";

        if (diff >= 0.8)
            return isCzech ? "📈 Mírný vzestup" : "📈 Slight rise";

        return isCzech ? "➡️ Stabilní" : "➡️ Steady";
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
        if (cloudPercent <= 10) desc = isCzech ? "☀️ Jasno" : "☀️ Clear sky";
        else if (cloudPercent <= 30) desc = isCzech ? "🌤️ Skoro jasno" : "🌤️ Mostly clear";
        else if (cloudPercent <= 70) desc = isCzech ? "⛅ Polojasno" : "⛅ Partly cloudy";
        else if (cloudPercent <= 90) desc = isCzech ? "🌥️ Skoro zataženo" : "🌥️ Mostly cloudy";
        else desc = isCzech ? "☁️ Zataženo" : "☁️ Overcast";

        return (valueStr, desc);
    }

    public static (string IconAndName, string Detail) GetMoonPhaseInfo(double moonPhase, string? moonrise, string? moonset, bool isCzech)
    {
        string name;
        string icon;

        if (moonPhase == 0.0 || moonPhase == 1.0) { icon = "🌑"; name = isCzech ? "Nov" : "New Moon"; }
        else if (moonPhase < 0.25) { icon = "🌒"; name = isCzech ? "Dorůstající srp" : "Waxing Crescent"; }
        else if (moonPhase == 0.25) { icon = "🌓"; name = isCzech ? "První čtvrť" : "First Quarter"; }
        else if (moonPhase < 0.50) { icon = "🌔"; name = isCzech ? "Dorůstající měsíc" : "Waxing Gibbous"; }
        else if (moonPhase == 0.50) { icon = "🌕"; name = isCzech ? "Úplněk" : "Full Moon"; }
        else if (moonPhase < 0.75) { icon = "🌖"; name = isCzech ? "Couvající měsíc" : "Waning Gibbous"; }
        else if (moonPhase == 0.75) { icon = "🌗"; name = isCzech ? "Poslední čtvrť" : "Last Quarter"; }
        else { icon = "🌘"; name = isCzech ? "Ubývající srp" : "Waning Crescent"; }

        string risesetText = (moonrise != null && moonset != null) 
            ? $"↑ {moonrise}  ↓ {moonset}" 
            : "";

        return ($"{icon} {name}", risesetText);
    }

    public static (string Value, string Description) FormatAqi(int aqiValue, bool isCzech)
    {
        string valueStr = $"{aqiValue} AQI";
        string desc;

        if (aqiValue <= 50) desc = isCzech ? "🟢 Skvělá (Čistý vzduch)" : "🟢 Good";
        else if (aqiValue <= 100) desc = isCzech ? "🟡 Střední (Akceptovatelná)" : "🟡 Moderate";
        else if (aqiValue <= 150) desc = isCzech ? "🟠 Citlivé skupiny" : "🟠 Unhealthy for Sensitive";
        else if (aqiValue <= 200) desc = isCzech ? "🔴 Nezdravá" : "🔴 Unhealthy";
        else desc = isCzech ? "🟣 Velmi špatná" : "🟣 Very Unhealthy";

        return (valueStr, desc);
    }
}