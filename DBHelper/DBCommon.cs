// Decompiled with JetBrains decompiler
// Type: TrueLore.DBUtility.DBCommon
// Assembly: TrueLore.DBUtility, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 02232C77-1BA1-4231-9762-7CFCC0F825B3
// Assembly location: D:\Work_Market\SVN\NET\CommonDll\TrueLore.DBUtility.dll

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OracleClient;
using System.Data.SqlClient;
using System.Text.RegularExpressions;
using TrueLore.TLUtility;

namespace TrueLore.DBUtility
{
  public class DBCommon
  {
    private const string STR_不支持当前数据库 = "不支持当前数据库";

    public static bool CheckParamName(string param)
    {
      param = param.Trim();
      return !new Regex("'| |@|\\\\").IsMatch(param.ToLower());
    }

    public static DataTable GetByCondition(string dataConnString, string strSql, List<ConditionEntity> entities, bool hasWhere, bool isAnd)
    {
      return DBCommon.CommonByCondition(dataConnString, strSql, entities, hasWhere, isAnd, "", true);
    }

    public static DataTable GetByCondition(string dataConnString, string strSql, List<ConditionEntity> entities, bool hasWhere, bool isAnd, string sortFiled, bool isAsc)
    {
      return DBCommon.CommonByCondition(dataConnString, strSql, entities, hasWhere, isAnd, sortFiled, isAsc);
    }

    private static DataTable CommonByCondition(string dataConnString, string strSql, List<ConditionEntity> entities, bool hasWhere, bool isAnd, string sortFiled, bool isAsc)
    {
      try
      {
        if (!hasWhere)
          strSql += " WHERE  1=1 ";
        List<SqlParameter> sqlParameterList = new List<SqlParameter>();
        List<string> stringList = new List<string>((IEnumerable<string>) new string[9]
        {
          "=",
          "<>",
          ">",
          ">=",
          "<",
          "<=",
          "like",
          "not like",
          "in"
        });
        foreach (ConditionEntity entity in entities)
        {
          if (!string.IsNullOrEmpty(entity.ParamName))
          {
            entity.ParamName = entity.ParamName.Trim();
            if (string.IsNullOrEmpty(entity.FiledName))
              entity.FiledName = entity.ParamName;
            entity.FiledName = entity.FiledName.Trim();
            if (!DBCommon.CheckParamName(entity.ParamName) || !DBCommon.CheckParamName(entity.FiledName))
            {
              strSql += " AND 1=2 ";
            }
            else
            {
              if (string.IsNullOrEmpty(entity.Oper))
                entity.Oper = "=";
              string str1 = entity.Oper.ToLower().Trim();
              if (!stringList.Contains(str1))
                str1 = "=";
              string str2 = isAnd ? " AND " : " OR ";
              if (str1.IndexOf("in") > -1)
              {
                string str3 = string.Empty;
                string[] strArray = entity.ParamValue.Split(',');
                for (int index = 0; index < strArray.Length; ++index)
                {
                  str3 = str3 + ",@" + entity.ParamName + index.ToString();
                  SqlParameter sqlParameter = new SqlParameter(entity.ParamName + index.ToString(), (object) strArray[index].Trim());
                  sqlParameterList.Add(sqlParameter);
                }
                strSql = strSql + str2 + entity.FiledName + " " + str1 + " (" + str3.Substring(1) + ")";
              }
              else
              {
                if (entity.ParamValue.ToLower() == "getdate()")
                {
                  if (str1.IndexOf("like") <= -1)
                    strSql = strSql + str2 + entity.FiledName + str1 + entity.ParamValue;
                  else
                    continue;
                }
                else if (str1.IndexOf("like") > -1)
                  strSql = strSql + str2 + entity.FiledName + " " + str1 + " '%' + @" + entity.ParamName + " + '%'";
                else
                  strSql = strSql + str2 + entity.FiledName + str1 + "@" + entity.ParamName;
                SqlParameter sqlParameter = new SqlParameter(entity.ParamName, (object) entity.ParamValue);
                sqlParameterList.Add(sqlParameter);
              }
            }
          }
        }
        if (!string.IsNullOrEmpty(sortFiled))
        {
          sortFiled = sortFiled.Trim();
          if (DBCommon.CheckParamName(sortFiled))
          {
            string str = isAsc ? " ASC " : " DESC ";
            strSql = strSql + " ORDER BY " + sortFiled + str;
          }
        }
        return SQLHelper.ExecuteDataTableSql(dataConnString, strSql, sqlParameterList.ToArray());
      }
      catch (Exception ex)
      {
        throw ex;
      }
    }

    public static List<ConditionEntity> JsonToCondition(string jsonText)
    {
      try
      {
        if (string.IsNullOrEmpty(jsonText))
          return new List<ConditionEntity>();
        return ((JToken) JsonConvert.DeserializeObject(jsonText)).ToObject<List<ConditionEntity>>();
      }
      catch (Exception ex)
      {
        throw ex;
      }
    }

    public static DataTable ExecuteDataTableSql(DatabaseType databaseType, string connectionString, string sql, params object[] parameters)
    {
      switch (databaseType)
      {
        case DatabaseType.SQLSERVER:
          return SQLHelper.ExecuteDataTableSql(connectionString, sql, parameters.Length == 0 ? (SqlParameter[]) null : (SqlParameter[]) parameters);
        case DatabaseType.Oracle:
          return OracleHelper.ExecuteDataTableSql(connectionString, sql, parameters.Length == 0 ? (OracleParameter[]) null : (OracleParameter[]) parameters);
        default:
          throw new Exception("不支持当前数据库");
      }
    }

    public static DataSet ExecuteDataSetStoredProcedure(DatabaseType databaseType, string connectionString, string storedProcedureName, params object[] parameters)
    {
      switch (databaseType)
      {
        case DatabaseType.SQLSERVER:
          return SQLHelper.ExecuteDataSetStoredProcedure(connectionString, storedProcedureName, parameters.Length == 0 ? (SqlParameter[]) null : (SqlParameter[]) parameters);
        case DatabaseType.Oracle:
          return OracleHelper.ExecuteDataSetStoredProcedure(connectionString, storedProcedureName, parameters.Length == 0 ? (OracleParameter[]) null : (OracleParameter[]) parameters);
        default:
          throw new Exception("不支持当前数据库");
      }
    }

    public static DataTable ExecuteDataTableStoredProcedure(DatabaseType databaseType, string connectionString, string storedProcedureName, params object[] parameters)
    {
      switch (databaseType)
      {
        case DatabaseType.SQLSERVER:
          return SQLHelper.ExecuteDataTableStoredProcedure(connectionString, storedProcedureName, parameters.Length == 0 ? (SqlParameter[]) null : (SqlParameter[]) parameters);
        case DatabaseType.Oracle:
          return OracleHelper.ExecuteDataTableStoredProcedure(connectionString, storedProcedureName, parameters.Length == 0 ? (OracleParameter[]) null : (OracleParameter[]) parameters);
        default:
          throw new Exception("不支持当前数据库");
      }
    }

    public static int ExecuteNonQuerySql(DatabaseType databaseType, string connectionString, string sql, params object[] parameters)
    {
      switch (databaseType)
      {
        case DatabaseType.SQLSERVER:
          return SQLHelper.ExecuteNonQuerySql(connectionString, sql, parameters.Length == 0 ? (SqlParameter[]) null : (SqlParameter[]) parameters);
        case DatabaseType.Oracle:
          return OracleHelper.ExecuteNonQuerySql(connectionString, sql, parameters.Length == 0 ? (OracleParameter[]) null : (OracleParameter[]) parameters);
        default:
          throw new Exception("不支持当前数据库");
      }
    }

    public static int ExecuteNonQueryStoredProcedure(DatabaseType databaseType, string connectionString, string storedProcedureName, int commandTimeout, params object[] parameters)
    {
      switch (databaseType)
      {
        case DatabaseType.SQLSERVER:
          return SQLHelper.ExecuteNonQueryStoredProcedure(connectionString, storedProcedureName, commandTimeout, parameters.Length == 0 ? (SqlParameter[]) null : (SqlParameter[]) parameters);
        case DatabaseType.Oracle:
          return OracleHelper.ExecuteNonQueryStoredProcedure(connectionString, storedProcedureName, commandTimeout, parameters.Length == 0 ? (OracleParameter[]) null : (OracleParameter[]) parameters);
        default:
          throw new Exception("不支持当前数据库");
      }
    }

    public static int ExecuteNonQueryStoredProcedure(DatabaseType databaseType, string connectionString, string storedProcedureName, params object[] parameters)
    {
      switch (databaseType)
      {
        case DatabaseType.SQLSERVER:
          return SQLHelper.ExecuteNonQueryStoredProcedure(connectionString, storedProcedureName, parameters.Length == 0 ? (SqlParameter[]) null : (SqlParameter[]) parameters);
        case DatabaseType.Oracle:
          return OracleHelper.ExecuteNonQueryStoredProcedure(connectionString, storedProcedureName, parameters.Length == 0 ? (OracleParameter[]) null : (OracleParameter[]) parameters);
        default:
          throw new Exception("不支持当前数据库");
      }
    }

    public static void ExecuteNonQueryTransSql(DatabaseType databaseType, string connectionString, List<string> lstSql)
    {
      switch (databaseType)
      {
        case DatabaseType.SQLSERVER:
          SQLHelper.ExecuteNonQueryTransSql(connectionString, lstSql);
          break;
        case DatabaseType.Oracle:
          OracleHelper.ExecuteNonQueryTransSql(connectionString, lstSql);
          break;
        default:
          throw new Exception("不支持当前数据库");
      }
    }

    public static void ExecuteNonQueryTransSql(DatabaseType databaseType, string connectionString, object lstSql)
    {
      switch (databaseType)
      {
        case DatabaseType.SQLSERVER:
          SQLHelper.ExecuteNonQueryTransSql(connectionString, (List<SQLHelper.SqlStringObj>) lstSql);
          break;
        case DatabaseType.Oracle:
          OracleHelper.ExecuteNonQueryTransSql(connectionString, (List<OracleHelper.OracleStringObj>) lstSql);
          break;
        default:
          throw new Exception("不支持当前数据库");
      }
    }

    public static object ExecuteScalarSql(DatabaseType databaseType, string connectionString, string sql, params object[] parameters)
    {
      switch (databaseType)
      {
        case DatabaseType.SQLSERVER:
          return SQLHelper.ExecuteScalarSql(connectionString, sql, parameters.Length == 0 ? (SqlParameter[]) null : (SqlParameter[]) parameters);
        case DatabaseType.Oracle:
          return OracleHelper.ExecuteScalarSql(connectionString, sql, parameters.Length == 0 ? (OracleParameter[]) null : (OracleParameter[]) parameters);
        default:
          throw new Exception("不支持当前数据库");
      }
    }

    public static DataRow GetRowBySQL(DatabaseType databaseType, string connectionString, string sql)
    {
      switch (databaseType)
      {
        case DatabaseType.SQLSERVER:
          return SQLHelper.GetRowBySQL(connectionString, sql);
        case DatabaseType.Oracle:
          return OracleHelper.GetRowBySQL(connectionString, sql);
        default:
          throw new Exception("不支持当前数据库");
      }
    }
  }
}
