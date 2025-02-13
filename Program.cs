using NLog;
using System;
using System.Globalization;
using Oracle.ManagedDataAccess.Client;
using System.Threading.Tasks;

class Program
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
    private static DateOnly finicio;
    private static DateOnly ffin;
    private static bool ayer;


    private static void ParseArgs(string[] args)
    {
        finicio = DateOnly.FromDateTime(DateTime.Today);
        ffin = DateOnly.FromDateTime(DateTime.Today);
        ayer = args?.Contains("-ayer", StringComparer.OrdinalIgnoreCase) ?? false;

        if (ayer)
        {
            LogMessage("Se considera la fecha de ayer.");
            finicio = ffin = DateOnly.FromDateTime(DateTime.Today.AddDays(-1));
        }
        else if (args?.Length == 2 && TryParseDate(args[0], out finicio) && TryParseDate(args[1], out ffin))
        {
            LogMessage($"Fechas establecidas: {finicio} - {ffin}");
        }
        else
        {
            LogMessage("No se proporcionaron argumentos válidos. Se considera la fecha actual.");
        }
    }

    private static bool TryParseDate(string input, out DateOnly date)
    {
        return DateOnly.TryParseExact(input, "dd.MM.yyyy", new CultureInfo("es-BO"), DateTimeStyles.None, out date);
    }

    static async Task Main(string[] args)
    {
        ParseArgs(args);

        List<ArchivoDescarga> archivos = ArchivoDescarga.GetArchivos();

        List<Task> tareas = new List<Task>();

        foreach (ArchivoDescarga archivo in archivos)
        {
            tareas.Add(Task.Run(async () =>
            {
                LogMessage($"Archivo: {archivo.Id} - {archivo.FilePattern}");

                DateOnly fecha = finicio;
                while (fecha <= ffin)
                {
                    LogMessage($"Fecha: {fecha}");

                    try
                    {
                        //await 
                        archivo.ProcesarAsync(fecha);
                    }
                    catch (Exception e)
                    {
                        LogError(e);
                    }

                    fecha = fecha.AddDays(1);
                }
            }));
        }

        await Task.WhenAll(tareas);

        LogMessage("Procesamiento finalizado");
    }


    /* private static async Task RecargarElementosAsync()
     {
         elementos = await ElementoService.GetElementosAsync();
     }*/

    public static void LogMessage(string message)
    {
        Logger.Info(message);
    }

    public static void LogError(Exception e)
    {
        Logger.Error(e, e.Message);
    }
}
