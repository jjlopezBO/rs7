
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;


public sealed class DataRawObjectId
{
    private static object syncRoot = new object();

    private static Dictionary<string, decimal> dictionary = new Dictionary<string, decimal>();
    private static volatile DataRawObjectId instance;
    private static OracleConnection oracleconnection;

    public DataRawObjectId(OracleConnection cn)
    {
        //DataRawObjectId.hash = new Hashtable();
        oracleconnection = cn;
        this.loadData(cn);
    }

    public static DataRawObjectId Instance
    {
        get
        {
            if (DataRawObjectId.instance == null)
            {
                lock (DataRawObjectId.syncRoot)
                {
                    if (DataRawObjectId.instance == null)
                        DataRawObjectId.instance = new DataRawObjectId(oracleconnection);
                }
            }
            return DataRawObjectId.instance;
        }
    }

    private void loadData(OracleConnection cn)
    {

        OracleCommand command;
        try
        {
            command = cn.CreateCommand();
            if (command == null)
            {

                throw new Exception("cmd null");
            }
            command.CommandText = @"SELECT
    b1
    || '*'
    || b2
    || '*'
    || b3
    || '*'
    || el
    || '*'
    || tipo
    || '*'
    || type key,
    id_obj value
FROM
    data_raw_objet_sp7 a order by b1,b2,b3";

            OracleDataReader oracleDataReader = command.ExecuteReader();
            while (oracleDataReader.Read())
            {
                //DataRawObjectId.hash.Add(oracleDataReader["key"], oracleDataReader["value"]);
                DataRawObjectId.AddValue(((string)oracleDataReader["key"]).ToUpper(), (decimal)oracleDataReader["value"]); ;
            }
            int i = 5;
        }
        catch (OracleException ex)
        {
            Program.LogErrorOnDisk(ex);

        }
        finally
        {
            //    instance.DisposeCommand(command);
        }
    }
    private static bool AddValue(string Key, decimal Value)
    {
        bool rtn = false;
        if (!dictionary.ContainsKey(Key))
        {
            dictionary.Add(Key, Value);
            rtn = true;
        }
        return rtn;
    }
    public decimal getValue(string Key)
    {
        decimal result = 0;
        if (dictionary.ContainsKey(Key))
        {
            result = dictionary[Key];
        }
        else
        {

            Program.LogErrorOnDisk(new Exception("NO SE ENCONTRO EL ELEMENTO CON KEY :" + Key));
        }
        return result;
    }

    //public Decimal getValue2(string Key)
    //{
    //    Decimal result = new Decimal();
    //    object obj = DataRawObjectId.hash[(object)Key];
    //    if (obj != null)
    //        Decimal.TryParse(obj.ToString(), out result);
    //    else
    //        Program.LogErrorOnDisk(new Exception("NO SE ENCONTRO EL ELEMENTO CON KEY :" + Key));
    //    return result;
    //}
}

