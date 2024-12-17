using NLog;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;


internal class Record
{
    private string b1;
    private string b2;
    private string b3;
    private string el;
    private string in_;
    private string type;
    private Decimal id;
    private DateTime date;
    private double[] datos;
    private string raw;
    private int records_count;

    public Record(string data, string type_rec, DateTime workday)
    {
        this.raw = data.Substring(0, data.LastIndexOf(','));
        this.date = workday;
        this.type = type_rec;
        this.Parse();
    }
    public string Type
    {
        get
        { return type; }

    }
    public string HeaderString()
    {
        return string.Format("{0}-{1}-{2}-{3}-{4}", (object)this.b1, (object)this.b2, (object)this.b3, (object)this.el, (object)this.in_);
    }

    public override string ToString()
    {
        string str = string.Format("{0}-{1}-{2}-{3}-{4}-{5}", (object)this.id, (object)this.b1, (object)this.b2, (object)this.b3, (object)this.el, (object)this.in_);
        foreach (double dato in this.datos)
            str += string.Format("{0}-", (object)dato);
        return str;
    }

    public bool IsValid
    {
        get
        {
            return this.b1.IndexOf('[') < 0;
        }
    }

    private int IntervalNum(DateTime date)
    {
        return date.Hour * 4 + (int)Math.Truncate((double)(date.Minute / 15));
    }

    private double sumValues()
    {
        double num = 0.0;
        foreach (double dato in this.datos)
            num += dato;
        return num;
    }

    public double LoadDataRcd(int intInicio, int numIntervalo, DateTime Datav, OracleConnection cn)
    {
        if (this.id == Decimal.Zero)
            return -1.0;
        double num = this.sumValues();
        if (numIntervalo == 0)
            return num;
        ;
        OracleCommand command = cn.CreateCommand();
        command.CommandText = string.Format(" insert into {0} (id, fecha,valor, fecha_sng,intervalo ,type) values (:vid, :vfecha, :valor, :vfecha_sng,:vintervalo,:vtpye )  ", (object)this.type);
        Decimal[] numArray1 = new Decimal[numIntervalo];
        DateTime[] dateTimeArray1 = new DateTime[numIntervalo];
        DateTime[] dateTimeArray2 = new DateTime[numIntervalo];
        int[] numArray2 = new int[numIntervalo];
        double[] numArray3 = new double[numIntervalo];
        string[] strArray = new string[numIntervalo];
        for (int index = 0; index < numIntervalo; ++index)
        {
            numArray1[index] = this.id;
            dateTimeArray1[index] = Datav.Date.AddMinutes((double)((intInicio + index) * 15));
            dateTimeArray2[index] = Datav.Date;
            numArray2[index] = intInicio + index;
            double dato = 0;
            /*if (this.datos[index + intInicio] > 0)
            {*/
            dato = this.datos[index + intInicio];
            /*}                */
            numArray3[index] = dato;
            strArray[index] = this.el;
            Datav = Datav.AddMinutes(15.0);
        }
        command.ArrayBindCount = numIntervalo;
        OracleParameter oracleParameter1 = new OracleParameter("vid", OracleDbType.Decimal);
        oracleParameter1.Value = (object)numArray1;
        OracleParameter oracleParameter2 = new OracleParameter("vfecha", OracleDbType.Date);
        oracleParameter2.Value = (object)dateTimeArray1;
        OracleParameter oracleParameter3 = new OracleParameter("valor", OracleDbType.Double);
        oracleParameter3.Value = (object)numArray3;
        OracleParameter oracleParameter4 = new OracleParameter("vfecha_sng", OracleDbType.Date);
        oracleParameter4.Value = (object)dateTimeArray2;
        OracleParameter oracleParameter5 = new OracleParameter("vintervalo", OracleDbType.Int16);
        oracleParameter5.Value = (object)numArray2;
        OracleParameter oracleParameter6 = new OracleParameter("vtpye", OracleDbType.Varchar2);
        oracleParameter6.Value = (object)strArray;
        try
        {
            command.Parameters.Add(oracleParameter1);
            command.Parameters.Add(oracleParameter2);
            command.Parameters.Add(oracleParameter3);
            command.Parameters.Add(oracleParameter4);
            command.Parameters.Add(oracleParameter5);
            command.Parameters.Add(oracleParameter6);
            int i = command.ExecuteNonQuery();
            Program.LogErrorOnDisk(string.Format("Registros insertados:{0}", i));
        }
        catch (OracleException ex)
        {
            Program.LogErrorOnDisk(string.Format("ID: {0} - {1:dd.MM.yy HH.mi}", (object)this.id, (object)Datav.Date));
            Program.LogErrorOnDisk(string.Format("SQL: {0}", (object)command.CommandText));
            for (int index = 0; index < numIntervalo; ++index)
                Program.LogErrorOnDisk(string.Format("{0:0}\t{1:dd.MM.yy}\t{2:dd.MM.yy hh.mm}\t{3:0.0 }\t{4 :0.0#}", (object)numArray1[index], (object)dateTimeArray1[index], (object)dateTimeArray2[index], (object)numArray2[index], (object)numArray3[index]));


            Program.LogErrorOnDisk(ex);

            num = -1.0;
        }
        finally
        {
            //  OracleI.Instance.DisposeCommand(command);
        }
        return num;
    }

    private Decimal GetId()
    {

        if (this.b3 == "")
            this.b3 = "-";
        string value = this.b1 + "*" + this.b2 + "*" + this.b3 + "*" + this.el + "*" + this.in_ + "*" + this.type;
        return DataRawObjectId.Instance.getValue(value.ToUpper());
    }
    public int GetNumRec
    {
        get
        {
            return this.datos.Length;
        }
    }
    private bool Parse()
    {
        bool flag = false;
        string[] strArray = this.raw.Split(',');
        string decimalSeparator = Thread.CurrentThread.CurrentCulture.NumberFormat.NumberDecimalSeparator;
        if (strArray.Length >= 7)
        {
            this.b1 = strArray[0];
            this.b2 = strArray[1];
            this.b3 = strArray[2];
            this.el = strArray[3];
            this.in_ = strArray[4];
            this.records_count = strArray.Length - 5;
            this.datos = new double[this.records_count];
            int index = 0;
            this.id = this.GetId();
            if (this.id == Decimal.Zero && this.b1.IndexOf("[") == -1)
            {
                var logger = LogManager.GetCurrentClassLogger();
                logger.Error(new Exception(string.Format("b1:{0} b2:{1} b3:{2}", (object)this.b1, (object)this.b2, (object)this.b3)));
            }

            for (; index < this.records_count; ++index)
            {
                double result;
                this.datos[index] = !double.TryParse(strArray[5 + index].Replace(".", decimalSeparator), out result) ? 0.0 : result;
            }
        }
        return flag;
    }
}
