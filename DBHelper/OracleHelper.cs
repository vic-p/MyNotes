// Decompiled with JetBrains decompiler
// Type: TrueLore.DBUtility.OracleHelper
// Assembly: TrueLore.DBUtility, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 02232C77-1BA1-4231-9762-7CFCC0F825B3
// Assembly location: D:\Work_Market\SVN\NET\CommonDll\TrueLore.DBUtility.dll

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OracleClient;
using System.Text;

namespace TrueLore.DBUtility
{
  public static class OracleHelper
  {
    private static void PrepareCommand(OracleCommand cmd, OracleConnection conn, OracleTransaction trans, string cmdText, CommandType commandType, OracleParameter[] cmdParms)
    {
      if (conn.State != ConnectionState.Open)
        conn.Open();
      cmd.Connection = conn;
      cmd.CommandText = cmdText;
      if (trans != null)
        cmd.Transaction = trans;
      cmd.CommandType = commandType;
      if (cmdParms == null)
        return;
      foreach (OracleParameter cmdParm in cmdParms)
      {
        if ((cmdParm.Direction == ParameterDirection.InputOutput || cmdParm.Direction == ParameterDirection.Input) && cmdParm.Value == null)
          cmdParm.Value = (object) DBNull.Value;
        cmd.Parameters.Add(cmdParm);
      }
    }

    private static DataSet Query(string connectionString, string sql, CommandType commandType, params OracleParameter[] parameters)
    {
      using (OracleConnection conn = new OracleConnection(connectionString))
      {
        DataSet dataSet = new DataSet();
        try
        {
          OracleCommand oracleCommand = new OracleCommand();
          OracleHelper.PrepareCommand(oracleCommand, conn, (OracleTransaction) null, sql, commandType, parameters);
          new OracleDataAdapter(oracleCommand).Fill(dataSet, "ds");
        }
        catch (OracleException ex)
        {
          throw new Exception(ex.Message);
        }
        finally
        {
          conn.Close();
        }
        return dataSet;
      }
    }

    public static DataTable ExecuteDataTableSql(string connectionString, string sql, params OracleParameter[] parameters)
    {
      DataSet dataSet = OracleHelper.Query(connectionString, sql, CommandType.Text, parameters);
      if (dataSet.Tables.Count > 0)
        return dataSet.Tables[0];
      return (DataTable) null;
    }

    public static DataTable ExecuteDataTableStoredProcedure(string connectionString, string storedProcedureName, params OracleParameter[] parameters)
    {
      DataSet dataSet = OracleHelper.Query(connectionString, storedProcedureName, CommandType.StoredProcedure, parameters);
      if (dataSet.Tables.Count > 0)
        return dataSet.Tables[0];
      return (DataTable) null;
    }

    public static DataSet ExecuteDataSetStoredProcedure(string connectionString, string storedProcedureName, params OracleParameter[] parameters)
    {
      return OracleHelper.Query(connectionString, storedProcedureName, CommandType.StoredProcedure, parameters);
    }

    public static int ExecuteNonQuerySql(string connectionString, string sql, params OracleParameter[] parameters)
    {
      OracleCommand cmd = new OracleCommand();
      using (OracleConnection conn = new OracleConnection(connectionString))
      {
        OracleHelper.PrepareCommand(cmd, conn, (OracleTransaction) null, sql, CommandType.Text, parameters);
        int num = cmd.ExecuteNonQuery();
        conn.Close();
        cmd.Parameters.Clear();
        return num;
      }
    }

    public static int ExecuteNonQueryStoredProcedure(string connectionString, string storedProcedureName, int commandTimeout, params OracleParameter[] parameters)
    {
      OracleCommand cmd = new OracleCommand();
      using (OracleConnection conn = new OracleConnection(connectionString))
      {
        OracleHelper.PrepareCommand(cmd, conn, (OracleTransaction) null, storedProcedureName, CommandType.StoredProcedure, parameters);
        int num = cmd.ExecuteNonQuery();
        conn.Close();
        cmd.Parameters.Clear();
        return num;
      }
    }

    public static int ExecuteNonQueryStoredProcedure(string connectionString, string storedProcedureName, params OracleParameter[] parameters)
    {
      return OracleHelper.ExecuteNonQueryStoredProcedure(connectionString, storedProcedureName, 1800, parameters);
    }

    public static void ExecuteNonQueryTransSql(string connectionString, List<string> lstSql)
    {
      OracleCommand oracleCommand = new OracleCommand();
      using (OracleConnection oracleConnection = new OracleConnection(connectionString))
      {
        oracleConnection.Open();
        oracleCommand.Connection = oracleConnection;
        oracleCommand.CommandType = CommandType.Text;
        OracleTransaction oracleTransaction = oracleConnection.BeginTransaction(IsolationLevel.ReadCommitted);
        oracleCommand.Transaction = oracleTransaction;
        string str1 = string.Empty;
        try
        {
          foreach (string str2 in lstSql)
          {
            str1 = str2;
            oracleCommand.CommandText = str2;
            oracleCommand.ExecuteNonQuery();
          }
          oracleTransaction.Commit();
        }
        catch (Exception ex)
        {
          oracleTransaction.Rollback();
          StringBuilder stringBuilder = new StringBuilder();
          stringBuilder.AppendLine("ExecuteNonQueryTransSql 异常:" + ex.Message);
          stringBuilder.AppendLine("CurrentSQL:" + str1);
          stringBuilder.AppendLine("AllSQL:");
          foreach (string str2 in lstSql)
            stringBuilder.AppendLine(str2);
          throw new Exception(stringBuilder.ToString(), ex);
        }
        oracleConnection.Close();
      }
    }

    public static void ExecuteNonQueryTransSql(string connectionString, List<OracleHelper.OracleStringObj> lstSql)
    {
      OracleCommand oracleCommand = new OracleCommand();
      using (OracleConnection oracleConnection = new OracleConnection(connectionString))
      {
        oracleConnection.Open();
        oracleCommand.Connection = oracleConnection;
        oracleCommand.CommandType = CommandType.Text;
        OracleTransaction oracleTransaction = oracleConnection.BeginTransaction(IsolationLevel.ReadCommitted);
        oracleCommand.Transaction = oracleTransaction;
        OracleParameter[] oracleParameterArray = (OracleParameter[]) null;
        try
        {
          foreach (OracleHelper.OracleStringObj oracleStringObj in lstSql)
          {
            oracleParameterArray = oracleStringObj.parms;
            oracleCommand.CommandText = oracleStringObj.Sql;
            if (oracleStringObj.parms != null)
              oracleCommand.Parameters.AddRange(oracleStringObj.parms);
            oracleCommand.ExecuteNonQuery();
            oracleCommand.Parameters.Clear();
          }
          oracleTransaction.Commit();
        }
        catch (Exception ex)
        {
          oracleTransaction.Rollback();
          StringBuilder stringBuilder = new StringBuilder();
          stringBuilder.AppendLine("ExecuteNonQueryTransSql 异常:" + ex.Message);
          stringBuilder.AppendLine("CurrentSQL:" + oracleCommand.CommandText);
          stringBuilder.AppendLine("AllParameter:");
          if (oracleParameterArray != null)
          {
            foreach (OracleParameter oracleParameter in oracleParameterArray)
              stringBuilder.AppendLine(oracleParameter.ParameterName + ":" + oracleParameter.Value);
          }
          throw new Exception(stringBuilder.ToString(), ex);
        }
        oracleConnection.Close();
      }
    }

    public static object ExecuteScalarSql(string connectionString, string sql, params OracleParameter[] parameters)
    {
      OracleCommand cmd = new OracleCommand();
      using (OracleConnection conn = new OracleConnection(connectionString))
      {
        OracleHelper.PrepareCommand(cmd, conn, (OracleTransaction) null, sql, CommandType.Text, parameters);
        object obj = cmd.ExecuteOracleScalar();
        conn.Close();
        cmd.Parameters.Clear();
        return obj;
      }
    }

    public static DataRow GetRowBySQL(string connectionString, string sql)
    {
      DataTable dataTable = OracleHelper.ExecuteDataTableSql(connectionString, sql);
      if (dataTable.Rows.Count == 0)
        return (DataRow) null;
      return dataTable.Rows[0];
    }

    public struct OracleStringObj
    {
      public string Sql;
      public OracleParameter[] parms;
    }
  }
}
