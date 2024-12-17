using NLog;
using System;
using System.Text;
using System.Globalization;
using Oracle.ManagedDataAccess.Client;
using System.Net;
class Program
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
    private static bool ayer;
    private static DateOnly finicio;
    private static DateOnly ffin;

    private static List<FilesDown> lista;
    private static DataRawObjectId rawObj;

    private static void ParseArgs(string[] args)
    {
        finicio = DateOnly.FromDateTime(DateTime.Now.Date);
        ffin = DateOnly.FromDateTime(DateTime.Now.Date);
        ayer = false;

        if (args == null || args.Length == 0)
        {
            // Si no hay parámetros, se usa la fecha actual
            Program.LogErrorOnDisk("Se considera la fecha actual.");
        }
        else
        {
            // Procesar los argumentos
            for (int i = 0; i < args.Length; i++)
            {
                string arg = args[i];

                // Comprobar si el parámetro es -ayer
                if (arg.Equals("-ayer", StringComparison.OrdinalIgnoreCase))
                {
                    ayer = true;
                    break;  // Si se encuentra -ayer, no procesamos -fi ni -ff
                }
            }

            if (ayer)
            {
                // Si es -ayer, se ajustan las fechas a "ayer"
                Program.LogErrorOnDisk("Se considera la fecha de ayer.");
                finicio = DateOnly.FromDateTime(DateTime.Now.AddDays(-1).Date);
                ffin = DateOnly.FromDateTime(DateTime.Now.AddDays(-1).Date);
            }
            else if (args.Length == 2)
            {
                // Si se reciben dos parámetros (por ejemplo, -fi y -ff)
                try
                {
                    IFormatProvider provider = new CultureInfo("es-BO", true);
                    Program.LogErrorOnDisk(string.Format("Parametros {0}-{1} ", args[0], args[1]));

                    // Parsear las fechas con el formato dd.MM.yyyy
                    finicio = DateOnly.FromDateTime(DateTime.ParseExact(args[0], "dd.MM.yyyy", provider));
                    ffin = DateOnly.FromDateTime(DateTime.ParseExact(args[1], "dd.MM.yyyy", provider));
                }
                catch (FormatException e)
                {
                    Program.LogErrorOnDisk(e);
                }
            }
            else
            {
                Program.LogErrorOnDisk("Parametros incorrectos. Se considera la fecha actual.");
                finicio = DateOnly.FromDateTime(DateTime.Now.Date);
                ffin = DateOnly.FromDateTime(DateTime.Now.Date);
            }
        }
    }

    static void Main(string[] args)
    {


        Program.ParseArgs(args);

        Logger.Info("Se ejecuta la carga de los dias {0} al {1}", finicio.ToString(), ffin.ToString());
        Oracle.ManagedDataAccess.Client.OracleConnection cn = new Oracle.ManagedDataAccess.Client.OracleConnection("USER ID=spectrum;DATA SOURCE=(DESCRIPTION=(ADDRESS_LIST=(ADDRESS=(PROTOCOL=TCP)(HOST=192.168.2.13)(PORT=1521)))(CONNECT_DATA=(SERVER=DEDICATED)(SERVICE_NAME=orcl.cndc.bo)));PASSWORD=spectrum;PERSIST SECURITY INFO=true;");
        cn.Open();
        try
        {
            Program.rawObj = new DataRawObjectId(cn);
            Program.lista = new FilesDown("", "", "").ReadFilesDown(cn);

            while (finicio <= ffin)
            {
                Program.LogErrorOnDisk(string.Format("Fecha proceso:{0} ", finicio));
                foreach (FilesDown fd in Program.lista)
                {
                    try
                    {
                        Program.LogErrorOnDisk(string.Format("Procesar:{0}", fd.FilePattern));
                        Program.ProcessDay(fd, finicio.ToDateTime(new TimeOnly(0)), cn);

                    }
                    catch (Exception e)
                    {

                        LogErrorOnDisk(e);
                    }

                }



                finicio = finicio.AddDays(1);
            }
        }
        catch (Exception ex)
        {
            //   int num = (int)MessageBox.Show("se ha presentado un error por favor contactese con el adminsitrador");
            LogErrorOnDisk(ex);
        }

    }
    public static void LogErrorOnDisk(Exception e)
    {
        Logger.Error(e.ToString());

    }

    public static void LogErrorOnDisk(string e)
    {
        Logger.Info(e.ToString());

    }
    private static void ProcessDay(FilesDown fd, DateTime fecha, OracleConnection cn)
    {
        try
        {
            double num1 = fd.ReadFile(fecha.Date);
            fd.LoadToDb(fecha.Date, cn);

        }
        catch (Exception e)
        {
            Program.LogErrorOnDisk(e);
        }


    }
}