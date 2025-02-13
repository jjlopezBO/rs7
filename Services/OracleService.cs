using System.Runtime.InteropServices;
using Oracle.ManagedDataAccess.Client;
using Microsoft.Extensions.Configuration;

public class OracleService
{
    private static OracleService? _instance;
    private static IConfiguration? _configuration;

    private static readonly object _lock = new object();
    private OracleConnection _connection;

    private OracleService()
    {

        var builder = new ConfigurationBuilder()
          .SetBasePath(Directory.GetCurrentDirectory()) // Directorio actual
          .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

        _configuration = builder.Build();

        // Obtener la cadena de conexión
        string connectionString = _configuration.GetConnectionString("OracleDB") ??
                                  throw new InvalidOperationException("Cadena de conexión no encontrada.");

        Console.WriteLine($"Cadena de conexión: {connectionString}");
        _connection = new OracleConnection(connectionString);
        _connection.Open();
    }

    public static OracleService Instance
    {
        get
        {
            lock (_lock)
            {
                return _instance ??= new OracleService();
            }
        }
    }

    public OracleConnection GetConnection()
    {
        return _connection;
    }
    public OracleCommand GetCommand()
    {

        return _connection.CreateCommand();
    }
}