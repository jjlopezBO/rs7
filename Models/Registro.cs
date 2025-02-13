using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Primitives;
using Oracle.ManagedDataAccess.Client;
using NLog;

public class Registro
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
    public string B1 { get; set; }
    public string B2 { get; set; }
    public string B3 { get; set; }
    public string EL { get; set; }
    public string IN { get; set; }

    public long IdElemento { get; set; }
    public Dictionary<TimeSpan, double> Valores { get; set; }

    public Registro(string b1, string b2, string b3, string el, string vin)
    {
        B1 = b1;
        B2 = b2;
        B3 = b3;
        EL = el;
        IN = vin;
        IdElemento = -1;

        Valores = new Dictionary<TimeSpan, double>();
    }

    static List<Registro> LeerArchivo(string ruta)
    {
        var registros = new List<Registro>();
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

                registros.Add(registro);
            }
        }
        return registros;
    }
    public bool SetIdElemento(string type)
    {
        bool rtn = true;
        if (!SetId(type))
        {
            rtn = InsertElemento(type);
        }

        return rtn;
    }
    private bool SetId(string type)
    {
        bool rtn = false;
        try
        {
            using (OracleCommand command = OracleService.Instance.GetCommand())
            {


                if (command == null)
                {

                    throw new Exception("cmd null");
                }
                command.CommandText = @"select *
  from data_raw_objet_sp7
 where b1 = :pb1
   and b2 = :pb2
   and b3 = :pb3
   and el = :pel
   and tipo = :ptipo
   and type = :ptype";
                command.Parameters.Add(new OracleParameter("pb1", B1));
                command.Parameters.Add(new OracleParameter("pb2", B2));
                command.Parameters.Add(new OracleParameter("pb3", B3));
                command.Parameters.Add(new OracleParameter("pel", EL));
                command.Parameters.Add(new OracleParameter("ptipo", IN));
                command.Parameters.Add(new OracleParameter("ptype", type));

                using (OracleDataReader reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        IdElemento = reader.GetInt64(reader.GetOrdinal("ID_OBJ"));
                        rtn = true;
                    }
                }

            }

        }
        catch (Exception ex)
        {
            Logger.Log(LogLevel.Error, ex, "Error al obtener el id del elemento.");
            rtn = false;
        }

        return rtn;

    }
    private bool InsertElemento(string type)
    {
        bool rtn = false;
        try
        {
            using (OracleCommand command = OracleService.Instance.GetCommand())
            {
                if (command == null)
                {

                    throw new Exception("cmd null");
                }
                command.CommandText = @"INSERT INTO data_raw_objet_sp7 
(id_obj, b1, b2, b3, el, tipo, type)
VALUES 
(data_raw_objet_sp7_seq.NEXTVAL,
 :pb1, :pb2, :pb3, :pel, :ptipo, :ptype)";
                command.Parameters.Add(new OracleParameter("pb1", B1));
                command.Parameters.Add(new OracleParameter("pb2", B2));
                command.Parameters.Add(new OracleParameter("pb3", B3));
                command.Parameters.Add(new OracleParameter("pel", EL));
                command.Parameters.Add(new OracleParameter("ptipo", "E"));
                command.Parameters.Add(new OracleParameter("ptype", type));

                //command.ExecuteNonQuery();
                //SetId(type);
                //Logger.Info("Elemento insertado correctamente.");

                rtn = true;
            }


        }
        catch (Exception ex)
        {
            Logger.Log(LogLevel.Error, ex, "Error al insertar el elemento.");
            rtn = false;
        }
        return rtn;

    }

}