// Decompiled with JetBrains decompiler
// Type: TrueLore.DBUtility.AccessHelper
// Assembly: TrueLore.DBUtility, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 02232C77-1BA1-4231-9762-7CFCC0F825B3
// Assembly location: D:\Work_Market\SVN\NET\CommonDll\TrueLore.DBUtility.dll

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;

namespace TrueLore.DBUtility
{
  public static class AccessHelper
  {
    public static string connectionString = "Provider=Microsoft.Jet.OLEDB.4.0;Data Source={0};Jet OLEDB:Engine Type=5";

    public static int ExecuteInsert(string sql, string filePath, params OleDbParameter[] parameters)
    {
      using (OleDbConnection connection = new OleDbConnection(string.Format(AccessHelper.connectionString, (object) filePath)))
      {
        OleDbCommand oleDbCommand = new OleDbCommand(sql, connection);
        if (parameters != null)
          oleDbCommand.Parameters.AddRange(parameters);
        try
        {
          connection.Open();
          oleDbCommand.ExecuteNonQuery();
          oleDbCommand.CommandText = "SELECT @@IDENTITY";
          return int.Parse(oleDbCommand.ExecuteScalar().ToString());
        }
        catch (Exception ex)
        {
          throw ex;
        }
      }
    }

    public static void ExecuteDataSetInsert(string fileName, DataSet ds)
    {
      OleDbConnection selectConnection = new OleDbConnection(string.Format(AccessHelper.connectionString, (object) fileName));
      selectConnection.Open();
      for (int index1 = 0; index1 < ds.Tables.Count; ++index1)
      {
        OleDbDataAdapter adapter = new OleDbDataAdapter("SELECT * FROM " + ds.Tables[index1].TableName, selectConnection);
        try
        {
          OleDbCommandBuilder dbCommandBuilder = new OleDbCommandBuilder(adapter);
          dbCommandBuilder.SetAllValues = true;
          dbCommandBuilder.QuotePrefix = "[";
          dbCommandBuilder.QuoteSuffix = "]";
          adapter.InsertCommand = dbCommandBuilder.GetInsertCommand();
          ds.Tables[index1].BeginLoadData();
          int count = ds.Tables[index1].Rows.Count;
          for (int index2 = 0; index2 < count; ++index2)
            ds.Tables[index1].Rows[index2].SetAdded();
          ds.Tables[index1].EndLoadData();
          adapter.Update(ds, ds.Tables[index1].TableName);
          adapter.Dispose();
        }
        catch (Exception ex)
        {
          adapter.Dispose();
          selectConnection.Close();
          selectConnection.Dispose();
          throw ex;
        }
      }
      selectConnection.Close();
      selectConnection.Dispose();
    }

    public static int ExecuteNonQuery(string sql, string filePath, params OleDbParameter[] parameters)
    {
      using (OleDbConnection connection = new OleDbConnection(string.Format(AccessHelper.connectionString, (object) filePath)))
      {
        OleDbCommand oleDbCommand = new OleDbCommand(sql, connection);
        if (parameters != null)
          oleDbCommand.Parameters.AddRange(parameters);
        try
        {
          connection.Open();
          return oleDbCommand.ExecuteNonQuery();
        }
        catch (Exception ex)
        {
          throw ex;
        }
      }
    }

    public static int ExecuteScalar(string sql, string conn, params OleDbParameter[] parameters)
    {
      using (OleDbConnection connection = new OleDbConnection(conn))
      {
        OleDbCommand oleDbCommand = new OleDbCommand(sql, connection);
        if (parameters != null)
          oleDbCommand.Parameters.AddRange(parameters);
        try
        {
          connection.Open();
          return int.Parse(oleDbCommand.ExecuteScalar().ToString());
        }
        catch (Exception ex)
        {
          throw ex;
        }
      }
    }

    public static object ExecuteScalarSql(string sql, string conn, params OleDbParameter[] parameters)
    {
      using (OleDbConnection connection = new OleDbConnection(conn))
      {
        OleDbCommand oleDbCommand = new OleDbCommand(sql, connection);
        if (parameters != null)
          oleDbCommand.Parameters.AddRange(parameters);
        try
        {
          connection.Open();
          return oleDbCommand.ExecuteScalar();
        }
        catch (Exception ex)
        {
          throw ex;
        }
      }
    }

    public static DataTable ExecuteQuery(string sql, string tableName, string conn, params OleDbParameter[] parameters)
    {
      using (OleDbConnection selectConnection = new OleDbConnection(conn))
      {
        DataTable dataTable = new DataTable(tableName);
        try
        {
          selectConnection.Open();
          OleDbDataAdapter oleDbDataAdapter = new OleDbDataAdapter(sql, selectConnection);
          if (parameters != null)
            oleDbDataAdapter.SelectCommand.Parameters.AddRange(parameters);
          oleDbDataAdapter.Fill(dataTable);
        }
        catch (Exception ex)
        {
          throw ex;
        }
        return dataTable;
      }
    }

    public static bool ExistsTable(OleDbConnection oleconn, string ATableName)
    {
      bool flag = false;
      OleDbConnection oleDbConnection = oleconn;
      Guid tables = OleDbSchemaGuid.Tables;
      object[] objArray = new object[4];
      objArray[3] = (object) "table";
      object[] restrictions = objArray;
      DataTable oleDbSchemaTable = oleDbConnection.GetOleDbSchemaTable(tables, restrictions);
      for (int index = 0; index < oleDbSchemaTable.Rows.Count - 1; ++index)
      {
        if (oleDbSchemaTable.Rows[index][2].ToString() == ATableName)
        {
          flag = true;
          break;
        }
      }
      return flag;
    }

    public static bool ExistsField(OleDbConnection oleconn, string ATableName, string AFieldName)
    {
      bool flag = false;
      OleDbConnection oleDbConnection = oleconn;
      Guid columns = OleDbSchemaGuid.Columns;
      object[] objArray = new object[4];
      objArray[2] = (object) ATableName;
      object[] restrictions = objArray;
      foreach (DataRow row in (InternalDataCollectionBase) oleDbConnection.GetOleDbSchemaTable(columns, restrictions).Rows)
      {
        if (AFieldName.ToUpper() == row["COLUMN_NAME"].ToString().ToUpper())
        {
          flag = true;
          break;
        }
      }
      return flag;
    }

    public static bool ExistsField(OleDbDataReader reader, string fieldName)
    {
      fieldName = fieldName.ToUpper();
      for (int ordinal = 0; ordinal < reader.FieldCount; ++ordinal)
      {
        if (reader.GetName(ordinal).ToUpper() == fieldName)
          return true;
      }
      return false;
    }

    public static int ExistsFieldPosition(OleDbConnection oleconn, string ATableName, string AFieldName)
    {
      OleDbConnection oleDbConnection = oleconn;
      Guid columns = OleDbSchemaGuid.Columns;
      object[] objArray = new object[4];
      objArray[2] = (object) ATableName;
      object[] restrictions = objArray;
      foreach (DataRow row in (InternalDataCollectionBase) oleDbConnection.GetOleDbSchemaTable(columns, restrictions).Rows)
      {
        if (AFieldName.ToUpper() == row["COLUMN_NAME"].ToString().ToUpper())
          return Convert.ToInt32(row["ORDINAL_POSITION"]);
      }
      return -1;
    }

    public static void ExistsFieldPosition(OleDbConnection oleconn, string ATableName, Dictionary<string, int> dicFielsPosition)
    {
      if (dicFielsPosition == null || dicFielsPosition.Count == 0)
        return;
      Dictionary<string, int> dictionary = new Dictionary<string, int>();
      OleDbConnection oleDbConnection = oleconn;
      Guid columns = OleDbSchemaGuid.Columns;
      object[] objArray = new object[4];
      objArray[2] = (object) ATableName;
      object[] restrictions = objArray;
      DataTable oleDbSchemaTable = oleDbConnection.GetOleDbSchemaTable(columns, restrictions);
      foreach (KeyValuePair<string, int> keyValuePair in dicFielsPosition)
      {
        bool flag = false;
        foreach (DataRow row in (InternalDataCollectionBase) oleDbSchemaTable.Rows)
        {
          if (keyValuePair.Key.ToUpper() == row["COLUMN_NAME"].ToString().ToUpper())
          {
            dictionary.Add(keyValuePair.Key, Convert.ToInt32(row["ORDINAL_POSITION"]));
            flag = true;
            break;
          }
        }
        if (!flag)
          dictionary.Add(keyValuePair.Key, -1);
      }
      foreach (KeyValuePair<string, int> keyValuePair in dictionary)
        dicFielsPosition[keyValuePair.Key] = keyValuePair.Value;
    }

    public static int ExistsFieldPosition(OleDbDataReader reader, string fieldName)
    {
      fieldName = fieldName.ToUpper();
      for (int ordinal = 0; ordinal < reader.FieldCount; ++ordinal)
      {
        if (reader.GetName(ordinal).ToUpper() == fieldName)
          return ordinal;
      }
      return -1;
    }

    public static void ExistsFieldPosition(OleDbDataReader reader, Dictionary<string, int> dicFielsPosition)
    {
      if (dicFielsPosition == null || dicFielsPosition.Count == 0)
        return;
      Dictionary<string, int> dictionary = new Dictionary<string, int>();
      foreach (KeyValuePair<string, int> keyValuePair in dicFielsPosition)
      {
        bool flag = false;
        for (int ordinal = 0; ordinal < reader.FieldCount; ++ordinal)
        {
          if (keyValuePair.Key.ToUpper() == reader.GetName(ordinal).ToUpper())
          {
            dictionary.Add(keyValuePair.Key, ordinal);
            flag = true;
            break;
          }
        }
        if (!flag)
          dictionary.Add(keyValuePair.Key, -1);
      }
      foreach (KeyValuePair<string, int> keyValuePair in dictionary)
        dicFielsPosition[keyValuePair.Key] = keyValuePair.Value;
    }
  }
}
