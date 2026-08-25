namespace ArmprodWeather.Helpers;

public static class WeatherMapper
{
    public static string MapCodeToIcon(int code) => code switch
    {
        0 => "☀️", 1 or 2 => "🌤️", 3 => "☁️", 45 or 48 => "🌫️",
        51 or 53 or 55 or 61 or 63 or 65 => "🌧️", 71 or 73 or 75 => "❄️",
        95 or 96 or 99 => "🌩️", _ => "🌡️"
    };

    public static string MapCodeToCondition(int code, string language)
    {
        bool isCzech = language == "Czech";

        return code switch
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
    }
}