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

    public static string FormatCloudCover(int percentage, bool isCzech) =>
        isCzech ? $"Oblačnost {percentage} %" : $"Cloud cover {percentage} %";

    public static string FormatVisibility(double visibilityMeters, bool isCzech) =>
        isCzech ? $"Viditelnost {Math.Round(visibilityMeters / 1000.0, 1)} km" : $"Visibility {Math.Round(visibilityMeters / 1000.0, 1)} km";

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
}