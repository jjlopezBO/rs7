using Microsoft.Extensions.Configuration;
using System.IO;

public class AppSettings
{
    private static IConfiguration _configuration;

    static AppSettings()
    {
        var builder = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

        _configuration = builder.Build();
    }

    public static string GetConnectionString(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("El nombre de la cadena de conexión no puede estar vacío o ser nulo.", nameof(name));
        }

        var connectionString = _configuration.GetConnectionString(name);

        if (string.IsNullOrEmpty(connectionString))
        {
            throw new InvalidOperationException($"No se encontró una cadena de conexión para '{name}'.");
        }

        return connectionString;
    }

    public static string GetSetting(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("La clave de configuración no puede estar vacía o ser nula.", nameof(key));
        }

        var value = _configuration[$"ApplicationSettings:{key}"];

        if (string.IsNullOrEmpty(value))
        {
            throw new InvalidOperationException($"No se encontró un valor para la clave de configuración '{key}'.");
        }

        return value;
    }
}