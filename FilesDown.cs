using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;


internal class FilesDown
{
    public string FilePattern = "";
    private string ServerPath = "";
    public string FileType = "";
    public DateTime LastDateFromDb;
    public DateTime LastDateFromFile;
    private List<Record> ListaRegistro;

    public FilesDown(string server, string patten, string type)
    {
        this.ServerPath = server;
        this.FilePattern = patten;
        this.FileType = type;
    }

    public List<FilesDown> ReadFilesDown(OracleConnection cn)
    {
        List<FilesDown> filesDownList = new List<FilesDown>();
        OracleCommand command = cn.CreateCommand();
        try
        {
            command.CommandText = "  ";
            OracleDataReader oracleDataReader = command.ExecuteReader();
            while (oracleDataReader.Read())
            {
                FilesDown filesDown = new FilesDown(oracleDataReader["server_path"].ToString(), oracleDataReader["file_pattern"].ToString(), oracleDataReader["type"].ToString());
                filesDownList.Add(filesDown);
            }
        }
        catch (OracleException ex)
        {
            Program.LogErrorOnDisk(ex);

        }
        finally
        {
            //instance.DisposeCommand(command);
        }
        return filesDownList;
    }

    public int ReadLast15Min(DateTime fecha, OracleConnection cn)
    {

        OracleCommand command = cn.CreateCommand();
        int num = -1;
        try
        {
            command.CommandText = string.Format("select  nvl(max(intervalo),-1)  from {0} where fecha_sng=trunc(:p_fecha)", (object)this.FileType);
            command.Parameters.Add(new OracleParameter("p_fecha", (object)fecha));
            num = (int)((Decimal)command.ExecuteScalar());
        }
        catch (OracleException ex)
        {
            Program.LogErrorOnDisk(ex);
        }
        finally
        {
            // instance.DisposeCommand(command);
        }
        return num;
    }

    public double ReadFile(DateTime loadDate)
    {
        double num = 0.0;
        try
        {
            string str1 = this.FullPath(loadDate);
            string mountedPath = "/mnt/spectrum/TDM_TD30/20241205-900.TRANSFERENCIAS.TDM_TD30.csv"; // Ruta para macOS/Linux
            string windowsPath = @"\\192.168.2.120\Spectrum_Data\TDM_TD30\20241205-900.TRANSFERENCIAS.TDM_TD30.csv"; // Ruta para Windows
            //string str1;

            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                str1 = windowsPath; // Ruta de Windows
            }
            else
            {
                str1 = str1.Replace(@"\\\\192.168.2.120\\Spectrum_Data\", "/mnt/spectrum/");
                str1 = str1.Replace(@"\", "/");
                str1 = mountedPath; // Ruta montada en macOS/Linux
            }

            if (File.Exists(str1))
            {
                string str2 = Path.GetTempPath() + Guid.NewGuid().ToString() + ".txt";
                Program.LogErrorOnDisk(str2);
                File.Copy(str1, str2);
                num = this.readFile(str2, loadDate);
                File.Delete(str2);
            }
        }
        catch (Exception ex)
        {

            Program.LogErrorOnDisk(ex);
            throw ex;
        }


        return num;
    }

    private string FullPath(DateTime time)
    {
        string str = string.Format("{0}\\{1}", (object)this.ServerPath, (object)this.FilePattern).Replace("YYYYMMDD", time.ToString("yyyyMMdd"));
        Console.WriteLine("{0} Full Path", (object)str);
        return str;
    }

    private double readFile(string Path, DateTime Date)
    {
        int num1 = 0;
        Record record = (Record)null;
        List<string> stringList = this.ReadContent(Path);
        double num2 = 0.0;
        this.ListaRegistro = new List<Record>();
        try
        {
            foreach (string str in stringList)
            {
                if (num1 == 0)
                {
                    TimeSpan timeSpan = this.ReadFromFileLast15(str);
                    this.LastDateFromFile = Date + timeSpan;
                }
                else
                {
                    if (str.Length > 0)
                    {
                        double num3 = this.SumValues(str);
                        if (num3 != -1.0)
                            num2 += num3;
                        record = new Record(str, this.FileType, Date);
                        this.ListaRegistro.Add(record);
                    }
                    Console.WriteLine(record.ToString());
                }
                ++num1;
            }
        }
        catch (Exception ex)
        {
            Program.LogErrorOnDisk(ex);
            throw;
        }
        return num2;
    }

    public List<string> ReadContent(string Path)
    {
        List<string> stringList = new List<string>();
        StreamReader streamReader = new StreamReader(Path);
        while (streamReader.Peek() >= 0)
        {
            string str = streamReader.ReadLine();
            stringList.Add(str);
        }
        streamReader.Close();
        streamReader.Dispose();
        return stringList;
    }

    private TimeSpan ReadFromFileLast15(string s)
    {
        TimeSpan timeSpan = new TimeSpan(0, 0, 0);
        string str = s.Substring(s.Length - 9, 8);
        timeSpan = new TimeSpan(int.Parse(str.Substring(0, 2)), int.Parse(str.Substring(3, 2)), int.Parse(str.Substring(6, 2)));
        return timeSpan;
    }

    private double SumValues(string line)
    {
        double num1 = 0.0;
        double result = 0.0;
        string[] strArray = line.Split(',');
        int num2 = 0;
        string decimalSeparator = Thread.CurrentThread.CurrentCulture.NumberFormat.NumberDecimalSeparator;
        foreach (string str in strArray)
        {
            if (num2 > 4 && double.TryParse(str.Replace(".", decimalSeparator), out result))
                num1 += result;
            ++num2;
        }
        return num1;
    }

    private int GetInterval(DateTime fecha)
    {
        return fecha.Hour * 4 + (int)Math.Truncate((double)(fecha.Minute / 15));
    }

    public double LoadToDb(DateTime Fecha, Oracle.ManagedDataAccess.Client.OracleConnection cn)
    {

        if (this.ListaRegistro != null)
        {
            int num = this.ReadLast15Min(Fecha, cn);
            int interval = this.ListaRegistro[0].GetNumRec - 1;
            // this.GetInterval(this.LastDateFromFile);
            int numIntervalo = num != -1 ? interval - num : interval;



            foreach (Record record in this.ListaRegistro)
            {


                try
                {
                    if (record.LoadDataRcd(num + 1, numIntervalo, Fecha, cn) == -1.0)
                    {
                        Program.LogErrorOnDisk(record.HeaderString() + record.Type);
                        /* instance.RollBackTransaction();
                         return -1.0;*/
                    }
                }
                catch (Exception e)
                {
                    Program.LogErrorOnDisk(e);

                }
            }
            //  instance.CommitTransaction();
        }
        return 0.0;
    }

    public double SumDay(DateTime fecha, OracleConnection cn)
    {
        OracleCommand command = cn.CreateCommand();
        double num = -1.0;
        try
        {
            command.CommandText = string.Format("select NVL(sum(valor),0) from {0} where fecha_sng = trunc(:pfecha)", (object)this.FileType);
            command.Parameters.Add(new OracleParameter("pfecha", (object)fecha.Date));
            num = (double)((Decimal)command.ExecuteScalar());
        }
        catch (OracleException ex)
        {
            Program.LogErrorOnDisk(ex);
        }
        finally
        {
            //instance.DisposeCommand(command);
        }
        return num;
    }
}
