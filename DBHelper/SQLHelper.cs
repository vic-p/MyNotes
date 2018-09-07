// Decompiled with JetBrains decompiler
// Type: TrueLore.DBUtility.SQLHelper
// Assembly: TrueLore.DBUtility, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 02232C77-1BA1-4231-9762-7CFCC0F825B3
// Assembly location: D:\Work_Market\SVN\NET\CommonDll\TrueLore.DBUtility.dll

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using TrueLore.Common;
using TrueLore.TLUtility;

namespace TrueLore.DBUtility
{
  public static class SQLHelper
  {
    private static readonly Regex regInsert = new Regex("^\\s*INSERT\\s+INTO\\s+\\[?([^\\]\\s\\(]+)\\]?\\s*\\(([^\\)]+)\\)\\s*((VALUES\\s*\\(((.|\\s)+)\\)\\s*)|(SELECT(.|\\s)+))$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex regUpdate = new Regex("^\\s*UPDATE\\s+\\[?([^\\]\\s]+)\\]?\\s+SET\\s+((\\s*,?\\s*\\[?([^'=,\\s\\[\\]]+)\\]?\\s*=\\s*((('([^']|(''))*')|(\\([^\\)]+\\))|([^,'](?!WHERE|FROM)))+))+)\\s+((WHERE(.|\\s)+)|(FROM(.|\\s)+))?$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex regDelete = new Regex("^\\s*DELETE\\s+(FROM\\s+)?\\[?([^\\]\\s]+)\\]?\\s*((WHERE(.|\\s)+)|(FROM(.|\\s)+))?$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex regFieldName = new Regex("^\\s*([^'=,\\s\\[\\]\\.]+\\.)*\\[?([^'=,\\[\\]]+)\\]?\\s*$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex regFieldNameAndFieldValue = new Regex("\\s*,?\\s*\\[?([^'=,\\s\\[\\]]+)\\]?\\s*=\\s*((('([^']|(''))*')|(\\([^\\)]+\\))|([^,'](?!WHERE|FROM)))+)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex regValues = new Regex("^VALUES", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex regWhere = new Regex("^WHERE", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex regFrom = new Regex("^FROM", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex regEventLogID = new Regex("@EventLogID", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex regEmpty = new Regex("^\\s*$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly string dataSetName = "EventLog";

    private static int ExecuteNonQueryTransSqlAndWriteEventLog(byte eventLogType, string userName, string eventLogAdditionalInformation, string connectionString, List<string> lstSQL, SqlParameter[] parameters)
    {
      List<string> stringList1 = new List<string>();
      string computerName = CommonFunction.GetComputerName();
      string macSn = CommonFunction.GetMacSN();
      string eventSource = SQLHelper.GetEventSource();
      string empty1 = string.Empty;
      string empty2 = string.Empty;
      bool flag = false;
      int num1 = -1;
      string identityColumnName = string.Empty;
      int num2 = 0;
      SqlConnection sqlConnection = new SqlConnection(connectionString);
      SqlTransaction sqlTransaction = (SqlTransaction) null;
      SqlCommand sqlCommand = new SqlCommand();
      try
      {
        sqlConnection.Open();
        sqlTransaction = sqlConnection.BeginTransaction(IsolationLevel.ReadCommitted);
        sqlCommand.Connection = sqlConnection;
        sqlCommand.CommandType = CommandType.Text;
        sqlCommand.CommandTimeout = 1800;
        sqlCommand.Transaction = sqlTransaction;
        if (parameters != null && parameters.Length > 0)
          sqlCommand.Parameters.AddRange(parameters);
        foreach (string input in lstSQL)
        {
          if (!SQLHelper.regEmpty.IsMatch(input))
          {
            switch (eventLogType)
            {
              case 0:
              case 1:
                if (SQLHelper.regInsert.IsMatch(input))
                {
                  GroupCollection groups = SQLHelper.regInsert.Match(input).Groups;
                  flag = SQLHelper.CheckTableIdentityColumnExists(sqlCommand, groups[1].Value, out identityColumnName);
                }
                sqlCommand.CommandText = input;
                num2 = sqlCommand.ExecuteNonQuery();
                if (flag)
                {
                  sqlCommand.CommandText = "SELECT @@IDENTITY";
                  object obj = sqlCommand.ExecuteScalar();
                  if (parameters != null && parameters.Length > 0 && (parameters[parameters.Length - 1] != null && parameters[parameters.Length - 1].DbType == DbType.Int32))
                    parameters[parameters.Length - 1].Value = (object) Convert.ToInt32(obj);
                }
                if ((int) eventLogType == 1 && stringList1.Count == 0)
                {
                  stringList1.Add(string.Format("INSERT INTO Sys_EventLog (UserName, MachineName, MAC, EventSource, AdditionalInformation, CreateTime)\r\n                                VALUES ('{0}', '{1}', '{2}', '{3}', '{4}', GETDATE())\r\n                                SELECT SCOPE_IDENTITY()", (object) userName, (object) computerName, (object) macSn, (object) eventSource, (object) eventLogAdditionalInformation));
                  continue;
                }
                continue;
              case 2:
                if (SQLHelper.regInsert.IsMatch(input))
                {
                  GroupCollection groups = SQLHelper.regInsert.Match(input).Groups;
                  flag = SQLHelper.CheckTableIdentityColumnExists(sqlCommand, groups[1].Value, out identityColumnName);
                  if (flag)
                  {
                    sqlCommand.CommandText = input;
                    num2 = sqlCommand.ExecuteNonQuery();
                    sqlCommand.CommandText = "SELECT @@IDENTITY";
                    object obj = sqlCommand.ExecuteScalar();
                    if (parameters != null && parameters.Length > 0 && (parameters[parameters.Length - 1] != null && parameters[parameters.Length - 1].DbType == DbType.Int32))
                      parameters[parameters.Length - 1].Value = (object) Convert.ToInt32(obj);
                    sqlCommand.CommandText = string.Format("SELECT TOP {0} * FROM [{1}] ORDER BY [{2}] DESC", (object) num2, (object) groups[1].Value, (object) identityColumnName);
                  }
                  else if (SQLHelper.regValues.IsMatch(groups[3].Value))
                    sqlCommand.CommandText = string.Format("SELECT {0}", (object) groups[5].Value);
                  else
                    sqlCommand.CommandText = groups[3].Value;
                  SqlDataAdapter sqlDataAdapter = new SqlDataAdapter(sqlCommand);
                  DataTable dataTable = new DataTable();
                  sqlDataAdapter.Fill(dataTable);
                  if (!flag)
                  {
                    string[] strArray = groups[2].Value.Split(',');
                    for (int index = 0; index < strArray.Length; ++index)
                      dataTable.Columns[index].ColumnName = SQLHelper.regFieldName.Match(strArray[index]).Groups[2].Value;
                  }
                  DataSet dataSet = new DataSet();
                  dataSet.DataSetName = SQLHelper.dataSetName;
                  dataTable.TableName = groups[1].Value;
                  dataSet.Tables.Add(dataTable);
                  StringWriter stringWriter = new StringWriter();
                  dataSet.WriteXml((TextWriter) stringWriter);
                  if (stringList1.Count == 0)
                    stringList1.Add(string.Format("INSERT INTO Sys_EventLog (UserName, MachineName, MAC, EventSource, AdditionalInformation, CreateTime)\r\n                                            VALUES ('{0}', '{1}', '{2}', '{3}', '{4}', GETDATE())\r\n                                            SELECT SCOPE_IDENTITY()", (object) userName, (object) computerName, (object) macSn, (object) eventSource, (object) eventLogAdditionalInformation));
                  stringList1.Add(string.Format("INSERT INTO Sys_EventLogDetail (EventLogID, OldContent, NewContent)\r\n                                        VALUES (@EventLogID, '', '{0}')", (object) stringWriter.ToString().Replace("'", "''")));
                  stringWriter.Close();
                  stringWriter.Dispose();
                  if (!flag)
                  {
                    sqlCommand.CommandText = input;
                    num2 = sqlCommand.ExecuteNonQuery();
                    continue;
                  }
                  continue;
                }
                if (SQLHelper.regUpdate.IsMatch(input))
                {
                  GroupCollection groups1 = SQLHelper.regUpdate.Match(input).Groups;
                  Regex regex = new Regex(string.Format("{0}\\s+(AS\\s+)?([^,\\s]+)", (object) groups1[1].Value), RegexOptions.IgnoreCase);
                  string str1 = !regex.IsMatch(groups1[12].Value) ? groups1[1].Value + "." : regex.Match(groups1[12].Value).Groups[2].Value + ".";
                  sqlCommand.CommandText = string.Format("SELECT TOP 0 * FROM {0}", (object) groups1[1].Value);
                  SqlDataAdapter sqlDataAdapter1 = new SqlDataAdapter(sqlCommand);
                  DataTable dataTable1 = new DataTable();
                  sqlDataAdapter1.MissingSchemaAction = MissingSchemaAction.AddWithKey;
                  sqlDataAdapter1.Fill(dataTable1);
                  List<string> stringList2 = new List<string>();
                  foreach (DataColumn dataColumn in dataTable1.PrimaryKey)
                    stringList2.Add(dataColumn.ColumnName);
                  StringBuilder stringBuilder1 = new StringBuilder();
                  StringBuilder stringBuilder2 = new StringBuilder();
                  foreach (Match match in SQLHelper.regFieldNameAndFieldValue.Matches(groups1[2].Value))
                  {
                    GroupCollection groups2 = match.Groups;
                    if (stringList2.Contains(groups2[1].Value))
                      stringList2.Remove(groups2[1].Value);
                    stringBuilder1.AppendFormat("{0}[{1}],", (object) str1, (object) groups2[1].Value);
                    stringBuilder2.AppendFormat("{0},", (object) groups2[2].Value);
                  }
                  stringBuilder1.Remove(stringBuilder1.Length - 1, 1);
                  stringBuilder2.Remove(stringBuilder2.Length - 1, 1);
                  foreach (string str2 in stringList2)
                  {
                    stringBuilder1.Insert(0, string.Format("{0}[{1}],", (object) str1, (object) str2));
                    stringBuilder2.Insert(0, string.Format("{0}[{1}],", (object) str1, (object) str2));
                  }
                  string str3;
                  if (SQLHelper.regWhere.IsMatch(groups1[12].Value))
                  {
                    sqlCommand.CommandText = string.Format("SELECT {0} FROM {1} {2}", (object) stringBuilder1, (object) groups1[1].Value, (object) groups1[12].Value);
                    str3 = string.Format("SELECT {0} FROM {1} {2}", (object) stringBuilder2, (object) groups1[1].Value, (object) groups1[12].Value);
                  }
                  else if (SQLHelper.regFrom.IsMatch(groups1[12].Value))
                  {
                    sqlCommand.CommandText = string.Format("SELECT {0} {1}", (object) stringBuilder1, (object) groups1[12].Value);
                    str3 = string.Format("SELECT {0} {1}", (object) stringBuilder2, (object) groups1[12].Value);
                  }
                  else
                  {
                    sqlCommand.CommandText = string.Format("SELECT {0} FROM [{1}]", (object) stringBuilder1, (object) groups1[1].Value);
                    str3 = string.Format("SELECT {0} FROM [{1}]", (object) stringBuilder2, (object) groups1[1].Value);
                  }
                  SqlDataAdapter sqlDataAdapter2 = new SqlDataAdapter(sqlCommand);
                  DataTable dataTable2 = new DataTable();
                  sqlDataAdapter2.Fill(dataTable2);
                  sqlCommand.CommandText = str3;
                  SqlDataAdapter sqlDataAdapter3 = new SqlDataAdapter(sqlCommand);
                  DataTable dataTable3 = new DataTable();
                  sqlDataAdapter3.Fill(dataTable3);
                  string[] strArray = stringBuilder1.ToString().Split(',');
                  for (int index = 0; index < strArray.Length; ++index)
                  {
                    dataTable2.Columns[index].ColumnName = SQLHelper.regFieldName.Match(strArray[index]).Groups[2].Value;
                    dataTable3.Columns[index].ColumnName = SQLHelper.regFieldName.Match(strArray[index]).Groups[2].Value;
                  }
                  DataSet dataSet1 = new DataSet();
                  dataSet1.DataSetName = SQLHelper.dataSetName;
                  dataTable2.TableName = groups1[1].Value;
                  dataSet1.Tables.Add(dataTable2);
                  StringWriter stringWriter1 = new StringWriter();
                  dataSet1.WriteXml((TextWriter) stringWriter1);
                  DataSet dataSet2 = new DataSet();
                  dataSet2.DataSetName = SQLHelper.dataSetName;
                  dataTable3.TableName = groups1[1].Value;
                  dataSet2.Tables.Add(dataTable3);
                  StringWriter stringWriter2 = new StringWriter();
                  dataSet2.WriteXml((TextWriter) stringWriter2);
                  if (stringList1.Count == 0)
                    stringList1.Add(string.Format("INSERT INTO Sys_EventLog (UserName, MachineName, MAC, EventSource, AdditionalInformation, CreateTime)\r\n                                            VALUES ('{0}', '{1}', '{2}', '{3}', '{4}', GETDATE())\r\n                                            SELECT SCOPE_IDENTITY()", (object) userName, (object) computerName, (object) macSn, (object) eventSource, (object) eventLogAdditionalInformation));
                  stringList1.Add(string.Format("INSERT INTO Sys_EventLogDetail (EventLogID, OldContent, NewContent)\r\n                                        VALUES (@EventLogID, '{0}', '{1}')", (object) stringWriter1.ToString().Replace("'", "''"), (object) stringWriter2.ToString().Replace("'", "''")));
                  stringWriter1.Close();
                  stringWriter1.Dispose();
                  stringWriter2.Close();
                  stringWriter2.Dispose();
                  sqlCommand.CommandText = input;
                  num2 = sqlCommand.ExecuteNonQuery();
                  continue;
                }
                if (SQLHelper.regDelete.IsMatch(input))
                {
                  GroupCollection groups = SQLHelper.regDelete.Match(input).Groups;
                  if (SQLHelper.regWhere.IsMatch(groups[3].Value))
                    sqlCommand.CommandText = string.Format("SELECT * FROM [{0}] {1}", (object) groups[2].Value, (object) groups[3].Value);
                  else if (SQLHelper.regFrom.IsMatch(groups[3].Value))
                  {
                    Regex regex = new Regex(string.Format("{0}\\s+(AS\\s+)?([^,\\s]+)", (object) groups[2].Value), RegexOptions.IgnoreCase);
                    string str = !regex.IsMatch(groups[3].Value) ? groups[2].Value + "." : regex.Match(groups[3].Value).Groups[2].Value + ".";
                    sqlCommand.CommandText = string.Format("SELECT {0}* {1}", (object) str, (object) groups[3].Value);
                  }
                  else
                    sqlCommand.CommandText = string.Format("SELECT * FROM [{0}]", (object) groups[2].Value);
                  SqlDataAdapter sqlDataAdapter = new SqlDataAdapter(sqlCommand);
                  DataTable dataTable = new DataTable();
                  sqlDataAdapter.Fill(dataTable);
                  DataSet dataSet = new DataSet();
                  dataSet.DataSetName = SQLHelper.dataSetName;
                  dataTable.TableName = groups[2].Value;
                  dataSet.Tables.Add(dataTable);
                  StringWriter stringWriter = new StringWriter();
                  dataSet.WriteXml((TextWriter) stringWriter);
                  if (stringList1.Count == 0)
                    stringList1.Add(string.Format("INSERT INTO Sys_EventLog (UserName, MachineName, MAC, EventSource, AdditionalInformation, CreateTime)\r\n                                            VALUES ('{0}', '{1}', '{2}', '{3}', '{4}', GETDATE())\r\n                                            SELECT SCOPE_IDENTITY()", (object) userName, (object) computerName, (object) macSn, (object) eventSource, (object) eventLogAdditionalInformation));
                  stringList1.Add(string.Format("INSERT INTO Sys_EventLogDetail (EventLogID, OldContent, NewContent)\r\n                                        VALUES (@EventLogID, '{0}', '')", (object) stringWriter.ToString().Replace("'", "''")));
                  stringWriter.Close();
                  stringWriter.Dispose();
                  sqlCommand.CommandText = input;
                  num2 = sqlCommand.ExecuteNonQuery();
                  continue;
                }
                sqlCommand.CommandText = input;
                num2 = sqlCommand.ExecuteNonQuery();
                if (stringList1.Count == 0)
                {
                  stringList1.Add(string.Format("INSERT INTO Sys_EventLog (UserName, MachineName, MAC, EventSource, AdditionalInformation, CreateTime)\r\n                                            VALUES ('{0}', '{1}', '{2}', '{3}', '{4}', GETDATE())\r\n                                            SELECT SCOPE_IDENTITY()", (object) userName, (object) computerName, (object) macSn, (object) eventSource, (object) eventLogAdditionalInformation));
                  continue;
                }
                continue;
              default:
                continue;
            }
          }
        }
        foreach (string input in stringList1)
        {
          if (num1 == -1)
          {
            sqlCommand.CommandText = input;
            num1 = Convert.ToInt32(sqlCommand.ExecuteScalar());
          }
          else
          {
            sqlCommand.CommandText = SQLHelper.regEventLogID.Replace(input, num1.ToString());
            sqlCommand.ExecuteNonQuery();
          }
        }
        sqlTransaction.Commit();
      }
      catch (Exception ex)
      {
        try
        {
          if (sqlTransaction != null)
            sqlTransaction.Rollback();
        }
        catch
        {
        }
        throw ex;
      }
      finally
      {
        sqlCommand.Dispose();
        sqlConnection.Close();
      }
      return num2;
    }

    private static string GetEventSource()
    {
      string empty = string.Empty;
      Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
      Assembly assembly1 = (Assembly) null;
      Assembly assembly2 = (Assembly) null;
      foreach (Assembly assembly3 in assemblies)
      {
        if (assembly3.FullName.StartsWith("System.Windows.Forms"))
        {
          assembly1 = assembly3;
          break;
        }
        if (assembly3.FullName.StartsWith("System.Web.UI"))
        {
          assembly2 = assembly3;
          break;
        }
      }
      StackFrame[] frames = new StackTrace().GetFrames();
      foreach (StackFrame stackFrame in frames)
      {
        MethodBase method = stackFrame.GetMethod();
        foreach (object customAttribute in method.GetCustomAttributes(false))
        {
          if (customAttribute.ToString() == "TrueLore.Ajax.AjaxMethodAttribute")
            return string.Format("{0}.{1}", (object) method.ReflectedType.FullName, (object) method.Name);
        }
        ParameterInfo[] parameters = method.GetParameters();
        if (parameters.Length == 2 && (assembly1 == null || method.ReflectedType.IsSubclassOf(assembly1.GetType("System.Windows.Forms.Form"))) && ((assembly2 == null || method.ReflectedType.IsSubclassOf(assembly2.GetType("System.Web.UI.Page"))) && parameters[0].ParameterType == typeof (object)) && (parameters[1].ParameterType == typeof (EventArgs) || parameters[1].ParameterType.IsSubclassOf(typeof (EventArgs)) || parameters[1].ParameterType.Name == "IZKFPEngXEvents_OnCaptureEvent"))
          return string.Format("{0}.{1}", (object) method.ReflectedType.FullName, (object) method.Name);
      }
      return empty;
    }

    private static bool CheckTableIdentityColumnExists(SqlCommand cmd, string tableName, out string identityColumnName)
    {
      identityColumnName = string.Empty;
      cmd.CommandText = string.Format("SELECT TOP 0 * FROM {0}", (object) tableName);
      SqlDataAdapter sqlDataAdapter = new SqlDataAdapter(cmd);
      DataTable dataTable = new DataTable();
      sqlDataAdapter.Fill(dataTable);
      bool flag = false;
      foreach (DataColumn column in (InternalDataCollectionBase) dataTable.Columns)
      {
        cmd.CommandText = string.Format("SELECT COLUMNPROPERTY(OBJECT_ID('{0}'), '{1}', 'IsIdentity')", (object) tableName, (object) column.ColumnName);
        flag = Convert.ToBoolean(cmd.ExecuteScalar());
        if (flag)
        {
          identityColumnName = column.ColumnName;
          break;
        }
      }
      return flag;
    }

    public static DataTable GetTableSchema(string connectionString, string tableName)
    {
      using (SqlConnection connection = new SqlConnection(connectionString))
      {
        connection.Open();
        SqlDataAdapter sqlDataAdapter = new SqlDataAdapter(new SqlCommand(string.Format("SELECT TOP 0 * FROM [{0}]", (object) tableName), connection));
        DataTable dataTable = new DataTable();
        sqlDataAdapter.Fill(dataTable);
        dataTable.TableName = "TableName";
        return dataTable;
      }
    }

    public static DataTable GetByCondition(string connectionString, string tableName, string condition)
    {
      using (SqlConnection connection = new SqlConnection(connectionString))
      {
        connection.Open();
        string cmdText = string.Format("SELECT * FROM [{0}]", (object) tableName);
        if (!string.IsNullOrEmpty(condition))
          cmdText += string.Format(" WHERE {0}", (object) condition);
        SqlDataAdapter sqlDataAdapter = new SqlDataAdapter(new SqlCommand(cmdText, connection));
        DataTable dataTable = new DataTable();
        sqlDataAdapter.Fill(dataTable);
        dataTable.TableName = "TableName";
        return dataTable;
      }
    }

    public static int ExecuteNonQueryStoredProcedure(string connectionString, string storedProcedureName, params SqlParameter[] parameters)
    {
      using (SqlConnection connection = new SqlConnection(connectionString))
      {
        SqlCommand sqlCommand = new SqlCommand(storedProcedureName, connection);
        sqlCommand.CommandType = CommandType.StoredProcedure;
        sqlCommand.CommandTimeout = 1800;
        if (parameters != null)
          sqlCommand.Parameters.AddRange(parameters);
        try
        {
          connection.Open();
          int num = sqlCommand.ExecuteNonQuery();
          sqlCommand.Parameters.Clear();
          return num;
        }
        catch (Exception ex)
        {
          throw ex;
        }
        finally
        {
          sqlCommand.Dispose();
          connection.Close();
        }
      }
    }

    public static int ExecuteNonQueryStoredProcedure(string connectionString, string storedProcedureName, int commandTimeout, params SqlParameter[] parameters)
    {
      using (SqlConnection connection = new SqlConnection(connectionString))
      {
        SqlCommand sqlCommand = new SqlCommand(storedProcedureName, connection);
        sqlCommand.CommandType = CommandType.StoredProcedure;
        sqlCommand.CommandTimeout = commandTimeout;
        if (parameters != null)
          sqlCommand.Parameters.AddRange(parameters);
        try
        {
          connection.Open();
          int num = sqlCommand.ExecuteNonQuery();
          sqlCommand.Parameters.Clear();
          return num;
        }
        catch (Exception ex)
        {
          throw ex;
        }
        finally
        {
          sqlCommand.Dispose();
          connection.Close();
        }
      }
    }

    public static int ExecuteNonQueryStoredProcedureAndWriteEventLog(byte eventLogType, string userName, string eventLogAdditionalInformation, string connectionString, string storedProcedureName, SqlParameter[] parameters)
    {
      string computerName = CommonFunction.GetComputerName();
      string macSn = CommonFunction.GetMacSN();
      string eventSource = SQLHelper.GetEventSource();
      using (SqlConnection connection = new SqlConnection(connectionString))
      {
        SqlCommand sqlCommand = new SqlCommand(storedProcedureName, connection);
        sqlCommand.CommandType = CommandType.StoredProcedure;
        sqlCommand.CommandTimeout = 1800;
        if (parameters != null)
          sqlCommand.Parameters.AddRange(parameters);
        try
        {
          connection.Open();
          int num = sqlCommand.ExecuteNonQuery();
          sqlCommand.Parameters.Clear();
          if ((int) eventLogType >= 1)
          {
            sqlCommand.CommandType = CommandType.Text;
            sqlCommand.CommandText = string.Format("INSERT INTO Sys_EventLog (UserName, MachineName, MAC, EventSource, AdditionalInformation, CreateTime)\r\n                                VALUES ('{0}', '{1}', '{2}', '{3}', '{4}', GETDATE())", (object) userName, (object) computerName, (object) macSn, (object) eventSource, (object) eventLogAdditionalInformation);
            sqlCommand.ExecuteNonQuery();
          }
          return num;
        }
        catch (Exception ex)
        {
          throw ex;
        }
        finally
        {
          sqlCommand.Dispose();
          connection.Close();
        }
      }
    }

    public static SqlDataReader ExecuteReaderStoredProcedure(string connectionString, string storedProcedureName, params SqlParameter[] parameters)
    {
      SqlConnection connection = new SqlConnection(connectionString);
      SqlCommand sqlCommand = new SqlCommand(storedProcedureName, connection);
      sqlCommand.CommandTimeout = 1800;
      sqlCommand.CommandType = CommandType.StoredProcedure;
      if (parameters != null)
        sqlCommand.Parameters.AddRange(parameters);
      try
      {
        connection.Open();
        return sqlCommand.ExecuteReader(CommandBehavior.CloseConnection);
      }
      catch (Exception ex)
      {
        sqlCommand.Dispose();
        connection.Close();
        throw ex;
      }
    }

    public static object ExecuteScalarStoredProcedure(string connectionString, string storedProcedureName, params SqlParameter[] parameters)
    {
      using (SqlConnection connection = new SqlConnection(connectionString))
      {
        SqlCommand sqlCommand = new SqlCommand(storedProcedureName, connection);
        sqlCommand.CommandType = CommandType.StoredProcedure;
        sqlCommand.CommandTimeout = 1800;
        if (parameters != null)
          sqlCommand.Parameters.AddRange(parameters);
        try
        {
          connection.Open();
          object obj = sqlCommand.ExecuteScalar();
          sqlCommand.Parameters.Clear();
          return obj;
        }
        catch (Exception ex)
        {
          throw ex;
        }
        finally
        {
          sqlCommand.Dispose();
          connection.Close();
        }
      }
    }

    public static object ExecuteScalarSql(string connectionString, string sql, params SqlParameter[] parameters)
    {
      using (SqlConnection connection = new SqlConnection(connectionString))
      {
        SqlCommand sqlCommand = new SqlCommand(sql, connection);
        sqlCommand.CommandType = CommandType.Text;
        sqlCommand.CommandTimeout = 1800;
        if (parameters != null)
          sqlCommand.Parameters.AddRange(parameters);
        try
        {
          connection.Open();
          object obj = sqlCommand.ExecuteScalar();
          sqlCommand.Parameters.Clear();
          return obj;
        }
        catch (Exception ex)
        {
          throw ex;
        }
        finally
        {
          sqlCommand.Dispose();
          connection.Close();
        }
      }
    }

    public static DataTable ExecuteDataTableStoredProcedure(string connectionString, string storedProcedureName, params SqlParameter[] parameters)
    {
      SqlConnection connection = new SqlConnection(connectionString);
      SqlCommand selectCommand = new SqlCommand(storedProcedureName, connection);
      selectCommand.CommandTimeout = 1800;
      selectCommand.CommandType = CommandType.StoredProcedure;
      SqlDataAdapter sqlDataAdapter = new SqlDataAdapter(selectCommand);
      if (parameters != null)
        selectCommand.Parameters.AddRange(parameters);
      try
      {
        connection.Open();
        DataTable dataTable = new DataTable();
        sqlDataAdapter.Fill(dataTable);
        dataTable.TableName = "TableName";
        return dataTable;
      }
      catch (Exception ex)
      {
        throw ex;
      }
      finally
      {
        selectCommand.Dispose();
        connection.Close();
        sqlDataAdapter.Dispose();
      }
    }

    public static DataSet ExecuteDataSetStoredProcedure(string connectionString, string storedProcedureName, params SqlParameter[] parameters)
    {
      SqlConnection connection = new SqlConnection(connectionString);
      SqlCommand selectCommand = new SqlCommand(storedProcedureName, connection);
      selectCommand.CommandTimeout = 1800;
      selectCommand.CommandType = CommandType.StoredProcedure;
      SqlDataAdapter sqlDataAdapter = new SqlDataAdapter(selectCommand);
      if (parameters != null)
        selectCommand.Parameters.AddRange(parameters);
      try
      {
        connection.Open();
        DataSet dataSet = new DataSet();
        sqlDataAdapter.Fill(dataSet);
        return dataSet;
      }
      catch (Exception ex)
      {
        throw ex;
      }
      finally
      {
        selectCommand.Dispose();
        connection.Close();
        sqlDataAdapter.Dispose();
      }
    }

    public static DataRow GetRowBySQL(string connectionString, string sql)
    {
      DataTable dataTable = SQLHelper.ExecuteDataTableSql(connectionString, sql);
      if (dataTable.Rows.Count == 0)
        return (DataRow) null;
      return dataTable.Rows[0];
    }

    public static DataTable ExecuteDataTableSql(string connectionString, string sql, params SqlParameter[] parameters)
    {
      SqlConnection connection = new SqlConnection(connectionString);
      SqlCommand selectCommand = new SqlCommand(sql, connection);
      selectCommand.CommandTimeout = 1800;
      selectCommand.CommandType = CommandType.Text;
      SqlDataAdapter sqlDataAdapter = new SqlDataAdapter(selectCommand);
      if (parameters != null)
        selectCommand.Parameters.AddRange(parameters);
      try
      {
        connection.Open();
        DataTable dataTable = new DataTable();
        sqlDataAdapter.Fill(dataTable);
        dataTable.TableName = "TableName";
        return dataTable;
      }
      catch (Exception ex)
      {
        throw ex;
      }
      finally
      {
        selectCommand.Dispose();
        connection.Close();
        sqlDataAdapter.Dispose();
      }
    }

    public static DataTable ExecuteDataTableSql(string connectionString, string sql, string tableName, params SqlParameter[] parameters)
    {
      SqlConnection connection = new SqlConnection(connectionString);
      SqlCommand selectCommand = new SqlCommand(sql, connection);
      selectCommand.CommandType = CommandType.Text;
      selectCommand.CommandTimeout = 1800;
      SqlDataAdapter sqlDataAdapter = new SqlDataAdapter(selectCommand);
      if (parameters != null)
        selectCommand.Parameters.AddRange(parameters);
      try
      {
        connection.Open();
        DataTable dataTable = new DataTable(tableName);
        sqlDataAdapter.Fill(dataTable);
        dataTable.TableName = "TableName";
        return dataTable;
      }
      catch (Exception ex)
      {
        throw ex;
      }
      finally
      {
        selectCommand.Dispose();
        connection.Close();
        sqlDataAdapter.Dispose();
      }
    }

    public static SqlDataReader ExecuteReaderSql(string connectionString, string sql, params SqlParameter[] parameters)
    {
      SqlConnection connection = new SqlConnection(connectionString);
      SqlCommand sqlCommand = new SqlCommand(sql, connection);
      sqlCommand.CommandType = CommandType.Text;
      sqlCommand.CommandTimeout = 1800;
      if (parameters != null)
        sqlCommand.Parameters.AddRange(parameters);
      try
      {
        connection.Open();
        return sqlCommand.ExecuteReader(CommandBehavior.CloseConnection);
      }
      catch (Exception ex)
      {
        sqlCommand.Dispose();
        connection.Close();
        throw ex;
      }
    }

    public static int ExecuteNonQuerySql(string connectionString, string sql, params SqlParameter[] parameters)
    {
      using (SqlConnection connection = new SqlConnection(connectionString))
      {
        SqlCommand sqlCommand = new SqlCommand(sql, connection);
        sqlCommand.CommandType = CommandType.Text;
        sqlCommand.CommandTimeout = 1800;
        if (parameters != null)
          sqlCommand.Parameters.AddRange(parameters);
        try
        {
          connection.Open();
          int num = sqlCommand.ExecuteNonQuery();
          sqlCommand.Parameters.Clear();
          return num;
        }
        catch (Exception ex)
        {
          throw ex;
        }
        finally
        {
          sqlCommand.Dispose();
          connection.Close();
        }
      }
    }

    public static int ExecuteNonQuerySqlAndWriteEventLog(byte eventLogType, string userName, string eventLogAdditionalInformation, string connectionString, string sql, params SqlParameter[] parameters)
    {
      return SQLHelper.ExecuteNonQueryTransSqlAndWriteEventLog(eventLogType, userName, eventLogAdditionalInformation, connectionString, new List<string>() { sql }, parameters);
    }

    public static void ExecuteNonQueryTransSql(string connectionString, List<string> lstSql)
    {
      SqlConnection sqlConnection = new SqlConnection(connectionString);
      SqlTransaction sqlTransaction = (SqlTransaction) null;
      SqlCommand sqlCommand = new SqlCommand();
      string str1 = string.Empty;
      try
      {
        sqlConnection.Open();
        sqlTransaction = sqlConnection.BeginTransaction(IsolationLevel.ReadCommitted);
        sqlCommand.Connection = sqlConnection;
        sqlCommand.CommandType = CommandType.Text;
        sqlCommand.CommandTimeout = 1800;
        sqlCommand.Transaction = sqlTransaction;
        foreach (string str2 in lstSql)
        {
          str1 = str2;
          sqlCommand.CommandText = str2;
          sqlCommand.ExecuteNonQuery();
        }
        sqlTransaction.Commit();
      }
      catch (Exception ex)
      {
        try
        {
          if (sqlTransaction != null)
            sqlTransaction.Rollback();
        }
        catch
        {
        }
        StringBuilder stringBuilder = new StringBuilder();
        stringBuilder.AppendLine("ExecuteNonQueryTransSql 异常:" + ex.Message);
        stringBuilder.AppendLine("CurrentSQL:" + str1);
        stringBuilder.AppendLine("AllSQL:");
        foreach (string str2 in lstSql)
          stringBuilder.AppendLine(str2);
        throw new Exception(stringBuilder.ToString(), ex);
      }
      finally
      {
        sqlCommand.Dispose();
        sqlConnection.Close();
      }
    }

    public static int ExecuteNonQueryTransSqlAndWriteEventLog(byte eventLogType, string userName, string eventLogAdditionalInformation, string connectionString, List<string> lstSQL)
    {
      return SQLHelper.ExecuteNonQueryTransSqlAndWriteEventLog(eventLogType, userName, eventLogAdditionalInformation, connectionString, lstSQL, (SqlParameter[]) null);
    }

    public static int ExecuteNonQueryTransSqlAndWriteEventLog(byte eventLogType, string userName, string eventLogAdditionalInformation, string connectionString, List<SQLHelper.SqlStringObj> lstSQL)
    {
      List<string> stringList1 = new List<string>();
      string computerName = CommonFunction.GetComputerName();
      string macSn = CommonFunction.GetMacSN();
      string eventSource = SQLHelper.GetEventSource();
      GroupCollection groupCollection = (GroupCollection) null;
      string empty1 = string.Empty;
      string empty2 = string.Empty;
      int num1 = -1;
      string empty3 = string.Empty;
      int num2 = 0;
      SqlConnection sqlConnection = new SqlConnection(connectionString);
      SqlTransaction sqlTransaction = (SqlTransaction) null;
      SqlCommand selectCommand = new SqlCommand();
      try
      {
        sqlConnection.Open();
        sqlTransaction = sqlConnection.BeginTransaction(IsolationLevel.ReadCommitted);
        selectCommand.Connection = sqlConnection;
        selectCommand.CommandType = CommandType.Text;
        selectCommand.CommandTimeout = 1800;
        selectCommand.Transaction = sqlTransaction;
        foreach (SQLHelper.SqlStringObj sqlStringObj in lstSQL)
        {
          if (!SQLHelper.regEmpty.IsMatch(sqlStringObj.Sql))
          {
            switch (eventLogType)
            {
              case 0:
              case 1:
                if (SQLHelper.regInsert.IsMatch(sqlStringObj.Sql))
                  groupCollection = SQLHelper.regInsert.Match(sqlStringObj.Sql).Groups;
                selectCommand.CommandText = sqlStringObj.Sql;
                if (sqlStringObj.parms != null)
                  selectCommand.Parameters.AddRange(sqlStringObj.parms);
                num2 = selectCommand.ExecuteNonQuery();
                selectCommand.Parameters.Clear();
                if ((int) eventLogType == 1 && stringList1.Count == 0)
                {
                  stringList1.Add(string.Format("INSERT INTO Sys_EventLog (UserName, MachineName, MAC, EventSource, AdditionalInformation, CreateTime)\r\n                                VALUES ('{0}', '{1}', '{2}', '{3}', '{4}', GETDATE())\r\n                                SELECT SCOPE_IDENTITY()", (object) userName, (object) computerName, (object) macSn, (object) eventSource, (object) eventLogAdditionalInformation));
                  continue;
                }
                continue;
              case 2:
                if (SQLHelper.regInsert.IsMatch(sqlStringObj.Sql))
                {
                  GroupCollection groups = SQLHelper.regInsert.Match(sqlStringObj.Sql).Groups;
                  if (SQLHelper.regValues.IsMatch(groups[3].Value))
                    selectCommand.CommandText = string.Format("SELECT {0}", (object) groups[5].Value);
                  else
                    selectCommand.CommandText = groups[3].Value;
                  if (sqlStringObj.parms != null)
                    selectCommand.Parameters.AddRange(sqlStringObj.parms);
                  SqlDataAdapter sqlDataAdapter = new SqlDataAdapter(selectCommand);
                  DataTable dataTable = new DataTable();
                  sqlDataAdapter.Fill(dataTable);
                  selectCommand.Parameters.Clear();
                  string[] strArray = groups[2].Value.Split(',');
                  for (int index = 0; index < strArray.Length; ++index)
                    dataTable.Columns[index].ColumnName = SQLHelper.regFieldName.Match(strArray[index]).Groups[2].Value;
                  DataSet dataSet = new DataSet();
                  dataSet.DataSetName = SQLHelper.dataSetName;
                  dataTable.TableName = groups[1].Value;
                  dataSet.Tables.Add(dataTable);
                  StringWriter stringWriter = new StringWriter();
                  dataSet.WriteXml((TextWriter) stringWriter);
                  if (stringList1.Count == 0)
                    stringList1.Add(string.Format("INSERT INTO Sys_EventLog (UserName, MachineName, MAC, EventSource, AdditionalInformation, CreateTime)\r\n                                            VALUES ('{0}', '{1}', '{2}', '{3}', '{4}', GETDATE())\r\n                                            SELECT SCOPE_IDENTITY()", (object) userName, (object) computerName, (object) macSn, (object) eventSource, (object) eventLogAdditionalInformation));
                  stringList1.Add(string.Format("INSERT INTO Sys_EventLogDetail (EventLogID, OldContent, NewContent)\r\n                                        VALUES (@EventLogID, '', '{0}')", (object) stringWriter.ToString().Replace("'", "''")));
                  stringWriter.Close();
                  stringWriter.Dispose();
                  selectCommand.CommandText = sqlStringObj.Sql;
                  if (sqlStringObj.parms != null)
                    selectCommand.Parameters.AddRange(sqlStringObj.parms);
                  num2 = selectCommand.ExecuteNonQuery();
                  selectCommand.Parameters.Clear();
                  continue;
                }
                if (SQLHelper.regUpdate.IsMatch(sqlStringObj.Sql))
                {
                  GroupCollection groups1 = SQLHelper.regUpdate.Match(sqlStringObj.Sql).Groups;
                  Regex regex = new Regex(string.Format("{0}\\s+(AS\\s+)?([^,\\s]+)", (object) groups1[1].Value), RegexOptions.IgnoreCase);
                  string str1 = !regex.IsMatch(groups1[12].Value) ? groups1[1].Value + "." : regex.Match(groups1[12].Value).Groups[2].Value + ".";
                  selectCommand.CommandText = string.Format("SELECT TOP 0 * FROM {0}", (object) groups1[1].Value);
                  SqlDataAdapter sqlDataAdapter1 = new SqlDataAdapter(selectCommand);
                  DataTable dataTable1 = new DataTable();
                  sqlDataAdapter1.MissingSchemaAction = MissingSchemaAction.AddWithKey;
                  sqlDataAdapter1.Fill(dataTable1);
                  List<string> stringList2 = new List<string>();
                  foreach (DataColumn dataColumn in dataTable1.PrimaryKey)
                    stringList2.Add(dataColumn.ColumnName);
                  StringBuilder stringBuilder1 = new StringBuilder();
                  StringBuilder stringBuilder2 = new StringBuilder();
                  foreach (Match match in SQLHelper.regFieldNameAndFieldValue.Matches(groups1[2].Value))
                  {
                    GroupCollection groups2 = match.Groups;
                    if (stringList2.Contains(groups2[1].Value))
                      stringList2.Remove(groups2[1].Value);
                    stringBuilder1.AppendFormat("{0}[{1}],", (object) str1, (object) groups2[1].Value);
                    stringBuilder2.AppendFormat("{0},", (object) groups2[2].Value);
                  }
                  stringBuilder1.Remove(stringBuilder1.Length - 1, 1);
                  stringBuilder2.Remove(stringBuilder2.Length - 1, 1);
                  foreach (string str2 in stringList2)
                  {
                    stringBuilder1.Insert(0, string.Format("{0}[{1}],", (object) str1, (object) str2));
                    stringBuilder2.Insert(0, string.Format("{0}[{1}],", (object) str1, (object) str2));
                  }
                  string str3;
                  if (SQLHelper.regWhere.IsMatch(groups1[12].Value))
                  {
                    selectCommand.CommandText = string.Format("SELECT {0} FROM {1} {2}", (object) stringBuilder1, (object) groups1[1].Value, (object) groups1[12].Value);
                    str3 = string.Format("SELECT {0} FROM {1} {2}", (object) stringBuilder2, (object) groups1[1].Value, (object) groups1[12].Value);
                  }
                  else if (SQLHelper.regFrom.IsMatch(groups1[12].Value))
                  {
                    selectCommand.CommandText = string.Format("SELECT {0} {1}", (object) stringBuilder1, (object) groups1[12].Value);
                    str3 = string.Format("SELECT {0} {1}", (object) stringBuilder2, (object) groups1[12].Value);
                  }
                  else
                  {
                    selectCommand.CommandText = string.Format("SELECT {0} FROM [{1}]", (object) stringBuilder1, (object) groups1[1].Value);
                    str3 = string.Format("SELECT {0} FROM [{1}]", (object) stringBuilder2, (object) groups1[1].Value);
                  }
                  if (sqlStringObj.parms != null)
                    selectCommand.Parameters.AddRange(sqlStringObj.parms);
                  SqlDataAdapter sqlDataAdapter2 = new SqlDataAdapter(selectCommand);
                  DataTable dataTable2 = new DataTable();
                  sqlDataAdapter2.Fill(dataTable2);
                  selectCommand.Parameters.Clear();
                  if (sqlStringObj.parms != null)
                    selectCommand.Parameters.AddRange(sqlStringObj.parms);
                  selectCommand.CommandText = str3;
                  SqlDataAdapter sqlDataAdapter3 = new SqlDataAdapter(selectCommand);
                  DataTable dataTable3 = new DataTable();
                  sqlDataAdapter3.Fill(dataTable3);
                  selectCommand.Parameters.Clear();
                  string[] strArray = stringBuilder1.ToString().Split(',');
                  for (int index = 0; index < strArray.Length; ++index)
                  {
                    dataTable2.Columns[index].ColumnName = SQLHelper.regFieldName.Match(strArray[index]).Groups[2].Value;
                    dataTable3.Columns[index].ColumnName = SQLHelper.regFieldName.Match(strArray[index]).Groups[2].Value;
                  }
                  DataSet dataSet1 = new DataSet();
                  dataSet1.DataSetName = SQLHelper.dataSetName;
                  dataTable2.TableName = groups1[1].Value;
                  dataSet1.Tables.Add(dataTable2);
                  StringWriter stringWriter1 = new StringWriter();
                  dataSet1.WriteXml((TextWriter) stringWriter1);
                  DataSet dataSet2 = new DataSet();
                  dataSet2.DataSetName = SQLHelper.dataSetName;
                  dataTable3.TableName = groups1[1].Value;
                  dataSet2.Tables.Add(dataTable3);
                  StringWriter stringWriter2 = new StringWriter();
                  dataSet2.WriteXml((TextWriter) stringWriter2);
                  if (stringList1.Count == 0)
                    stringList1.Add(string.Format("INSERT INTO Sys_EventLog (UserName, MachineName, MAC, EventSource, AdditionalInformation, CreateTime)\r\n                                            VALUES ('{0}', '{1}', '{2}', '{3}', '{4}', GETDATE())\r\n                                            SELECT SCOPE_IDENTITY()", (object) userName, (object) computerName, (object) macSn, (object) eventSource, (object) eventLogAdditionalInformation));
                  stringList1.Add(string.Format("INSERT INTO Sys_EventLogDetail (EventLogID, OldContent, NewContent)\r\n                                        VALUES (@EventLogID, '{0}', '{1}')", (object) stringWriter1.ToString().Replace("'", "''"), (object) stringWriter2.ToString().Replace("'", "''")));
                  stringWriter1.Close();
                  stringWriter1.Dispose();
                  stringWriter2.Close();
                  stringWriter2.Dispose();
                  selectCommand.CommandText = sqlStringObj.Sql;
                  if (sqlStringObj.parms != null)
                    selectCommand.Parameters.AddRange(sqlStringObj.parms);
                  num2 = selectCommand.ExecuteNonQuery();
                  selectCommand.Parameters.Clear();
                  continue;
                }
                if (SQLHelper.regDelete.IsMatch(sqlStringObj.Sql))
                {
                  GroupCollection groups = SQLHelper.regDelete.Match(sqlStringObj.Sql).Groups;
                  if (SQLHelper.regWhere.IsMatch(groups[3].Value))
                    selectCommand.CommandText = string.Format("SELECT * FROM [{0}] {1}", (object) groups[2].Value, (object) groups[3].Value);
                  else if (SQLHelper.regFrom.IsMatch(groups[3].Value))
                  {
                    Regex regex = new Regex(string.Format("{0}\\s+(AS\\s+)?([^,\\s]+)", (object) groups[2].Value), RegexOptions.IgnoreCase);
                    string str = !regex.IsMatch(groups[3].Value) ? groups[2].Value + "." : regex.Match(groups[3].Value).Groups[2].Value + ".";
                    selectCommand.CommandText = string.Format("SELECT {0}* {1}", (object) str, (object) groups[3].Value);
                  }
                  else
                    selectCommand.CommandText = string.Format("SELECT * FROM [{0}]", (object) groups[2].Value);
                  if (sqlStringObj.parms != null)
                    selectCommand.Parameters.AddRange(sqlStringObj.parms);
                  SqlDataAdapter sqlDataAdapter = new SqlDataAdapter(selectCommand);
                  DataTable dataTable = new DataTable();
                  sqlDataAdapter.Fill(dataTable);
                  selectCommand.Parameters.Clear();
                  DataSet dataSet = new DataSet();
                  dataSet.DataSetName = SQLHelper.dataSetName;
                  dataTable.TableName = groups[2].Value;
                  dataSet.Tables.Add(dataTable);
                  StringWriter stringWriter = new StringWriter();
                  dataSet.WriteXml((TextWriter) stringWriter);
                  if (stringList1.Count == 0)
                    stringList1.Add(string.Format("INSERT INTO Sys_EventLog (UserName, MachineName, MAC, EventSource, AdditionalInformation, CreateTime)\r\n                                            VALUES ('{0}', '{1}', '{2}', '{3}', '{4}', GETDATE())\r\n                                            SELECT SCOPE_IDENTITY()", (object) userName, (object) computerName, (object) macSn, (object) eventSource, (object) eventLogAdditionalInformation));
                  stringList1.Add(string.Format("INSERT INTO Sys_EventLogDetail (EventLogID, OldContent, NewContent)\r\n                                        VALUES (@EventLogID, '{0}', '')", (object) stringWriter.ToString().Replace("'", "''")));
                  stringWriter.Close();
                  stringWriter.Dispose();
                  selectCommand.CommandText = sqlStringObj.Sql;
                  if (sqlStringObj.parms != null)
                    selectCommand.Parameters.AddRange(sqlStringObj.parms);
                  num2 = selectCommand.ExecuteNonQuery();
                  selectCommand.Parameters.Clear();
                  continue;
                }
                selectCommand.CommandText = sqlStringObj.Sql;
                if (sqlStringObj.parms != null)
                  selectCommand.Parameters.AddRange(sqlStringObj.parms);
                num2 = selectCommand.ExecuteNonQuery();
                selectCommand.Parameters.Clear();
                if (stringList1.Count == 0)
                {
                  stringList1.Add(string.Format("INSERT INTO Sys_EventLog (UserName, MachineName, MAC, EventSource, AdditionalInformation, CreateTime)\r\n                                            VALUES ('{0}', '{1}', '{2}', '{3}', '{4}', GETDATE())\r\n                                            SELECT SCOPE_IDENTITY()", (object) userName, (object) computerName, (object) macSn, (object) eventSource, (object) eventLogAdditionalInformation));
                  continue;
                }
                continue;
              default:
                continue;
            }
          }
        }
        foreach (string input in stringList1)
        {
          if (num1 == -1)
          {
            selectCommand.CommandText = input;
            num1 = Convert.ToInt32(selectCommand.ExecuteScalar());
          }
          else
          {
            selectCommand.CommandText = SQLHelper.regEventLogID.Replace(input, num1.ToString());
            selectCommand.ExecuteNonQuery();
          }
        }
        sqlTransaction.Commit();
      }
      catch (Exception ex)
      {
        try
        {
          if (sqlTransaction != null)
            sqlTransaction.Rollback();
        }
        catch
        {
        }
        throw ex;
      }
      finally
      {
        selectCommand.Dispose();
        sqlConnection.Close();
      }
      return num2;
    }

    public static string BuildSearchConditionsSqlString(SearchSettings searchSettings)
    {
      if (searchSettings == null)
        return string.Empty;
      StringBuilder stringBuilder = new StringBuilder();
      bool flag = true;
      foreach (string key in searchSettings.Conditions.Keys)
      {
        string condition = searchSettings.Conditions[key];
        if (!(condition == string.Empty))
        {
          if (flag)
            flag = false;
          else
            stringBuilder.Append(" AND ");
          if (searchSettings.IsMatchWholeWord)
            stringBuilder.AppendFormat("{0} = '{1}'", (object) key, (object) condition);
          else
            stringBuilder.AppendFormat("{0} LIKE '%{1}%'", (object) key, (object) condition);
        }
      }
      if (searchSettings.ExtensionCondition != string.Empty)
      {
        if (flag)
          stringBuilder.Append(searchSettings.ExtensionCondition);
        else
          stringBuilder.AppendFormat(" AND ({0})", (object) searchSettings.ExtensionCondition);
      }
      return stringBuilder.ToString();
    }

    public static SqlParameter[] CreateCommonPagingStoredProcedureParameters(int startRow, int maxRows, string tableName, string primaryKey, string getFields, SearchSettings searchSettings, string sortExpression)
    {
      SqlParameter[] sqlParameterArray = new SqlParameter[8]
      {
        new SqlParameter("@StartRow", SqlDbType.Int),
        new SqlParameter("@MaxRows", SqlDbType.Int),
        new SqlParameter("@TableName", SqlDbType.NVarChar),
        new SqlParameter("@PrimaryKey", SqlDbType.NVarChar),
        new SqlParameter("@GetFields", SqlDbType.NVarChar),
        new SqlParameter("@SearchConditions", SqlDbType.NVarChar),
        new SqlParameter("@SortExpression", SqlDbType.NVarChar),
        new SqlParameter("@RecordsCount", SqlDbType.Int)
      };
      sqlParameterArray[0].Value = (object) startRow;
      sqlParameterArray[1].Value = (object) maxRows;
      sqlParameterArray[2].Value = (object) tableName;
      sqlParameterArray[3].Value = (object) primaryKey;
      sqlParameterArray[4].Value = (object) getFields;
      sqlParameterArray[5].Value = (object) SQLHelper.BuildSearchConditionsSqlString(searchSettings);
      sqlParameterArray[6].Value = (object) sortExpression;
      sqlParameterArray[7].Direction = ParameterDirection.ReturnValue;
      return sqlParameterArray;
    }

    public static void ExecuteNonQueryTransSql(string connectionString, List<SQLHelper.SqlStringObj> lstSql)
    {
      SqlConnection sqlConnection = new SqlConnection(connectionString);
      SqlTransaction sqlTransaction = (SqlTransaction) null;
      SqlCommand sqlCommand = new SqlCommand();
      SqlParameter[] sqlParameterArray = (SqlParameter[]) null;
      try
      {
        sqlConnection.Open();
        sqlTransaction = sqlConnection.BeginTransaction(IsolationLevel.ReadCommitted);
        sqlCommand.Connection = sqlConnection;
        sqlCommand.CommandType = CommandType.Text;
        sqlCommand.CommandTimeout = 1800;
        sqlCommand.Transaction = sqlTransaction;
        foreach (SQLHelper.SqlStringObj sqlStringObj in lstSql)
        {
          sqlParameterArray = sqlStringObj.parms;
          sqlCommand.CommandText = sqlStringObj.Sql;
          if (sqlStringObj.parms != null)
            sqlCommand.Parameters.AddRange(sqlStringObj.parms);
          sqlCommand.ExecuteNonQuery();
          sqlCommand.Parameters.Clear();
        }
        sqlTransaction.Commit();
      }
      catch (Exception ex)
      {
        try
        {
          if (sqlTransaction != null)
            sqlTransaction.Rollback();
        }
        catch
        {
        }
        StringBuilder stringBuilder = new StringBuilder();
        stringBuilder.AppendLine("ExecuteNonQueryTransSql 异常:" + ex.Message);
        stringBuilder.AppendLine("CurrentSQL:" + sqlCommand.CommandText);
        stringBuilder.AppendLine("AllParameter:");
        if (sqlParameterArray != null)
        {
          foreach (SqlParameter sqlParameter in sqlParameterArray)
            stringBuilder.AppendLine(sqlParameter.ParameterName + ":" + sqlParameter.SqlValue);
        }
        throw new Exception(stringBuilder.ToString(), ex);
      }
      finally
      {
        sqlCommand.Dispose();
        sqlConnection.Close();
      }
    }

    public static XmlDocument ExecuteXmlSql(string connectionString, string Sql, params SqlParameter[] parameters)
    {
      SqlConnection sqlConnection = new SqlConnection(connectionString);
      SqlCommand command = sqlConnection.CreateCommand();
      command.CommandText = Sql;
      command.CommandTimeout = 1800;
      if (parameters != null)
        command.Parameters.AddRange(parameters);
      try
      {
        sqlConnection.Open();
        XmlReader reader = command.ExecuteXmlReader();
        command.Parameters.Clear();
        XmlDocument xmlDocument = new XmlDocument();
        xmlDocument.Load(reader);
        if (xmlDocument.DocumentElement == null)
          return xmlDocument;
        XmlNode firstChild = xmlDocument.FirstChild;
        XmlDeclaration xmlDeclaration = xmlDocument.CreateXmlDeclaration("1.0", "utf-8", "yes");
        xmlDocument.InsertBefore((XmlNode) xmlDeclaration, firstChild);
        return xmlDocument;
      }
      catch (Exception ex)
      {
        throw ex;
      }
      finally
      {
        command.Dispose();
        sqlConnection.Close();
      }
    }

    public static void ExecuteNonQueryStoredProcedureList(string connectionString, List<string> lstStoredProcedureName, List<SqlParameter[]> lstParameters)
    {
      using (SqlConnection connection = new SqlConnection(connectionString))
      {
        for (int index = 0; index < lstStoredProcedureName.Count; ++index)
        {
          SqlCommand sqlCommand = new SqlCommand(lstStoredProcedureName[index], connection);
          sqlCommand.CommandType = CommandType.StoredProcedure;
          sqlCommand.CommandTimeout = 1800;
          if (lstParameters[index] != null)
            sqlCommand.Parameters.AddRange(lstParameters[index]);
          try
          {
            connection.Open();
            sqlCommand.ExecuteNonQuery();
            sqlCommand.Parameters.Clear();
          }
          catch (Exception ex)
          {
            throw ex;
          }
          finally
          {
            sqlCommand.Dispose();
            connection.Close();
          }
        }
      }
    }

    public static bool ExistsField(string connectionString, string ATableName, string AFieldName)
    {
      try
      {
        using (SqlConnection cn = new SqlConnection(connectionString))
          return SQLHelper.ExistsField(cn, ATableName, AFieldName);
      }
      catch (Exception ex)
      {
        return false;
      }
    }

    public static bool ExistsField(SqlConnection cn, string ATableName, string AFieldName)
    {
      try
      {
        SqlCommand command = cn.CreateCommand();
        command.CommandText = "SELECT 1 FROM sysobjects a, syscolumns b WHERE a.ID = b.ID AND a.Name = @TableName AND b.Name = @FieldName and a.xtype='U'";
        command.Parameters.AddWithValue("@TableName", (object) ATableName);
        command.Parameters.AddWithValue("@FieldName", (object) AFieldName);
        return (int) command.ExecuteScalar() > 0;
      }
      catch (Exception ex)
      {
        return false;
      }
    }

    public static void ExistsFieldPosition(string connectionString, string ATableName, Dictionary<string, int> dicFielsPosition)
    {
      try
      {
        using (SqlConnection cn = new SqlConnection(connectionString))
          SQLHelper.ExistsFieldPosition(cn, ATableName, dicFielsPosition);
      }
      catch (Exception ex)
      {
        throw ex;
      }
    }

    public static void ExistsFieldPosition(SqlConnection cn, string ATableName, Dictionary<string, int> dicFielsPosition)
    {
      if (dicFielsPosition == null || dicFielsPosition.Count == 0)
        return;
      string str1 = "";
      List<string> stringList = new List<string>();
      foreach (KeyValuePair<string, int> keyValuePair in dicFielsPosition)
      {
        stringList.Add(keyValuePair.Key);
        str1 = !string.IsNullOrEmpty(str1) ? str1 + string.Format(" OR b.Name = '{0}'", (object) keyValuePair.Key) : string.Format(" b.Name = '{0}'", (object) keyValuePair.Key);
      }
      string str2 = " ( " + str1 + " ) ";
      string sql = string.Format("SELECT b.Name , b.colorder FROM sysobjects a, syscolumns b WHERE a.ID = b.ID AND \r\n                                        a.Name = '{0}' AND {1} and a.xtype='U'", (object) ATableName, (object) str2);
      DataTable dataTable = SQLHelper.ExecuteDataTableSql(cn.ConnectionString, sql, (SqlParameter[]) null);
      if (dataTable.Rows.Count <= 0)
        return;
      foreach (DataRow row in (InternalDataCollectionBase) dataTable.Rows)
      {
        string key = row["Name"].ToString();
        int int32 = Convert.ToInt32(row["colorder"]);
        if (dicFielsPosition.ContainsKey(key))
        {
          dicFielsPosition[key] = int32;
        }
        else
        {
          foreach (string index in stringList)
          {
            if (index.ToUpper() == key.ToUpper())
            {
              dicFielsPosition[index] = int32;
              break;
            }
          }
        }
      }
    }

    public static bool ExistsTable(SqlConnection cn, string ATableName)
    {
      try
      {
        SqlCommand command = cn.CreateCommand();
        command.CommandText = "SELECT COUNT(1) FROM sysobjects WHERE [name] = @Name";
        command.Parameters.AddWithValue("@Name", (object) ATableName);
        return (int) command.ExecuteScalar() > 0;
      }
      catch (SqlException ex)
      {
        throw ex;
      }
    }

    public struct SqlStringObj
    {
      public string Sql;
      public SqlParameter[] parms;
    }
  }
}
