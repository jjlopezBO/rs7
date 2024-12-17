using NLog;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using Oracle.ManagedDataAccess.Client;



public sealed class OracleI
{
    private OracleConnection connection = null;
    private OracleTransaction transaction = null;
    private string connectionString = string.Empty;

    private OracleI()
    {
        Console.Write("ORACLE. ACCESO CONTROLADO MODULO N. 2.00.00");
    }




    public string ConnectionString
    {
        get
        {
            return connectionString;
        }
        set
        {
            connectionString = value;
            StartConnection();
        }
    }

    public void DisposeCommand(OracleCommand command)
    {
        if (command == null)
            return;
        command.Dispose();
        command = null;
    }

    public void EndConnection()
    {
        if (transaction != null)
        {
            transaction.Dispose();
            //  transaction = ()null;
        }
        if (connection == null || connection.State != ConnectionState.Open)
            return;
        connection.Close();
        connection.Dispose();
        connection = null;
    }

    public void StartConnection()
    {
        var logger = LogManager.GetCurrentClassLogger();

        if (connection != null && connection.State == ConnectionState.Open)
            return;
        if (connectionString == "")
        {


            throw new Exception("No se ha definido cadena de conexión");
        }
        try
        {




            logger.Info("Iniciando Conexión");
            connection = new OracleConnection(connectionString);
            connection.Open();
            logger.Info("Conexión abierta");
        }
        catch (Exception ex)
        {
            logger.Error(ex);
        }
    }

    public void StartTransaction()
    {
        var logger = LogManager.GetCurrentClassLogger();

        try
        {
            logger.Info("Iniciando Transacción");

            transaction = connection.BeginTransaction();
            logger.Info("Transacción Iniciada");

        }
        catch (Exception ex)
        {

            logger.Error(ex);
        }
    }

    public void RollBackTransaction()
    {
        if (transaction == null)
            return;
        transaction.Rollback();
    }

    public void CommitTransaction()
    {
        if (transaction == null)
            return;
        transaction.Commit();
    }
}
