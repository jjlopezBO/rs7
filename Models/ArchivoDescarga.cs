
using System;
using Oracle.ManagedDataAccess.Client;
using NLog;
using System.Globalization;
using System.Runtime.InteropServices;

public class ArchivoDescarga
{
    private static readonly Logger logger = LogManager.GetCurrentClassLogger();

    public int Id { get; set; }
    public string FilePattern { get; set; }
    public string ServerPath { get; set; }
    public string Tabla { get; set; }
    public string TablaV { get; set; }
    public string ProcLimpeza { get; set; }
    public string ProcCarga { get; set; }
    public int Activo { get; set; }
    public string Tipo { get; set; }


    public List<Registro> Registros { get; set; }
    // Constructor que inicializa la clase a partir de un OracleDataReader
    public ArchivoDescarga(OracleDataReader reader)
    {
        Id = Convert.IsDBNull(reader["ID"]) ? 0 : Convert.ToInt32(reader["ID"]);
        FilePattern = Convert.IsDBNull(reader["FILE_PATTERN"]) ? string.Empty : reader["FILE_PATTERN"]?.ToString() ?? string.Empty;
        ServerPath = Convert.IsDBNull(reader["SERVER_PATH"]) ? string.Empty : reader["SERVER_PATH"]?.ToString() ?? string.Empty;
        Tabla = Convert.IsDBNull(reader["TABLA"]) ? string.Empty : reader["TABLA"]?.ToString() ?? string.Empty;
        TablaV = Convert.IsDBNull(reader["TABLA_V"]) ? string.Empty : reader["TABLA_V"]?.ToString() ?? string.Empty;
        ProcLimpeza = Convert.IsDBNull(reader["PROC_LIMPIEZA"]) ? string.Empty : reader["PROC_LIMPIEZA"]?.ToString() ?? string.Empty;
        ProcCarga = Convert.IsDBNull(reader["PROC_CARGA"]) ? string.Empty : reader["PROC_CARGA"]?.ToString() ?? string.Empty;
        Activo = Convert.IsDBNull(reader["ACTIVO"]) ? 0 : Convert.ToInt32(reader["ACTIVO"]);
        Tipo = Convert.IsDBNull(reader["TIPO"]) ? string.Empty : reader["TIPO"]?.ToString() ?? string.Empty;
        Registros = new List<Registro>();
    }
    public void ProcesarAsync(DateOnly fecha)
    {
        //return Task.Run(() =>
        {
            try
            {
                if (CargarArchivo(fecha, Tabla))
                {
                    List<Validador> validador = Validador.GetStatus(Tipo);
                    foreach (Registro registro in Registros)
                    {

                        Validador validadorEncontrado = validador.Find(v => v.IdElemento == registro.IdElemento && v.Tipo == registro.EL);

                        if (validadorEncontrado != null)
                        {
                            Console.WriteLine($"Elemento encontrado: IdElemento={validadorEncontrado.IdElemento}, Tipo={validadorEncontrado.Tipo}");
                        }
                        else
                        {
                            Console.WriteLine("No se encontró ningún elemento con los criterios especificados.");
                        }




                        // Limpieza
                        // Carga
                    } // Limpieza
                    // Carga
                }
            }
            catch (Exception ex)
            {
                logger.Log(LogLevel.Error, ex, "Error al procesar el archivo.");
            }
        }
        //);
    }

    public bool CargarArchivo(DateOnly fecha, string tabla)
    {
        bool rtn = true;
        try
        {
            string path = $"{ServerPath}\\{FilePattern.Replace("YYYYMMDD", fecha.ToString("yyyyMMdd"))}";
            path = path.Replace(@"\", "/");
            logger.Log(LogLevel.Info, $"Cargando archivo: {path}");
            System.Console.WriteLine($"Cargando archivo: {path}");

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {

            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                path = path.Replace("//192.168.2.120/", "/Volumes/");
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                Console.WriteLine("La aplicación se está ejecutando en Linux.");
            }
            else
            {
                Console.WriteLine("No se pudo determinar el sistema operativo.");
            }

            Registros = LeerArchivo(path, tabla);


        }
        catch (Exception ex)
        {
            logger.Log(LogLevel.Error, ex, "Error al cargar el archivo.");
            rtn = false;
        }
        finally
        {

        }



        return rtn;
    }
    static List<Registro> LeerArchivo(string ruta, string tabla)
    {
        var registros = new List<Registro>();

        try
        {
            using (var reader = new StreamReader(ruta))
            {
                string? linea;
                string[] encabezados = reader.ReadLine()?.Split(','); // Leer la cabecera
                if (encabezados == null) return registros;

                while ((linea = reader.ReadLine()) != null)
                {
                    var columnas = linea.Split(',');

                    var registro = new Registro(columnas[0], columnas[1], columnas[2], columnas[3], columnas[4]);

                    for (int i = 5; i < columnas.Length; i++)
                    {
                        if (TimeSpan.TryParse(encabezados[i], out TimeSpan hora))
                        {
                            if (double.TryParse(columnas[i], NumberStyles.Any, CultureInfo.InvariantCulture, out double valor))
                            {
                                registro.Valores[hora] = valor;
                            }
                        }
                    }
                    registro.SetIdElemento(tabla);
                    registros.Add(registro);
                }
            }
        }
        catch (Exception ex)
        {
            logger.Log(LogLevel.Error, ex, "Error al cargar el archivo.");

        }
        finally
        {

        }


        return registros;
    }

    static void CompletarValoresFaltantes(List<Registro> registros, int maxValores)
    {
        foreach (var registro in registros)
        {
            if (registro.Valores.Count < maxValores)
            {
                var ultValor = registro.Valores.Values.LastOrDefault();
                var ultHora = registro.Valores.Keys.LastOrDefault();

                while (registro.Valores.Count < maxValores)
                {
                    ultHora = ultHora.Add(TimeSpan.FromMinutes(15)); // Asumiendo intervalo de 15 minutos
                    registro.Valores[ultHora] = ultValor;
                }
            }
        }
    }
    public static List<ArchivoDescarga> GetArchivos()
    {
        List<ArchivoDescarga> archivos = new List<ArchivoDescarga>();
        OracleCommand command;
        try
        {
            command = OracleService.Instance.GetCommand();

            if (command == null)
            {

                throw new Exception("cmd null");
            }
            command.CommandText = @"SELECT
    id,
    file_pattern,
    server_path,
    tabla,
    tabla_v,
    proc_limpieza,
    proc_carga,
    activo,
    tipo
FROM
    tdm_archivos_sp7
WHERE
    activo = 1";

            OracleDataReader oracleDataReader = command.ExecuteReader();
            while (oracleDataReader.Read())
            {
                archivos.Add(new ArchivoDescarga(oracleDataReader));
            }

        }
        catch (OracleException ex)
        {
            logger.Log(LogLevel.Error, ex, "Error al obtener los archivos.");

        }
        finally
        {
            //    instance.DisposeCommand(command);
        }
        return archivos;
    }
}


