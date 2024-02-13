using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using TCore.SqlCore;

namespace TCore.SqlClient;

public class SqlReader: ISqlReader
{
    Sql? m_sql;
    SqlDataReader? m_reader;
    bool m_fAttached;
    private readonly Guid m_crids;

    private SqlDataReader _Reader => m_reader ?? throw new Exception("no reader");

    #region Constructors

    public SqlReader()
    {
        m_fAttached = false;
        m_crids = Guid.Empty;
    }

    public SqlReader(SqlDataReader reader)
    {
        m_reader = reader;
        m_fAttached = true;
    }

    public SqlReader(Guid crids)
    {
        m_fAttached = false;
        m_crids = crids;
    }

    public SqlReader(Sql sql)
    {
        Attach(sql);
        m_crids = Guid.Empty;
    }

    public SqlReader(Sql sql, Guid crids)
    {
        Attach(sql);
        m_crids = crids;
    }
    #endregion

    /*----------------------------------------------------------------------------
        %%Function: Attach
        %%Qualified: TCore.SqlReader.Attach
    ----------------------------------------------------------------------------*/
    public void Attach(Sql sql)
    {
        m_sql = sql;
        if (m_sql != null)
            m_fAttached = true;
    }

    public void ExecuteQuery(
        SqlCommandTextInit cmdText,
        string? sResourceConnString = null,
        CustomizeCommandDelegate? customizeDelegate = null)
    {
        ExecuteQuery(cmdText.CommandText, sResourceConnString, customizeDelegate, cmdText.Aliases);
    }

    public void ExecuteQuery(
        string sQuery,
        CustomizeCommandDelegate? customizeDelegate = null,
        TableAliases? aliases = null)
    {
        ExecuteQuery(sQuery, null, customizeDelegate, aliases);
    }

    /*----------------------------------------------------------------------------
        %%Function: ExecuteQuery
        %%Qualified: TCore.SqlReader.ExecuteQuery
    ----------------------------------------------------------------------------*/
    public void ExecuteQuery(
        string sQuery,
        string? sResourceConnString = null,
        CustomizeCommandDelegate? customizeDelegate = null, 
        TableAliases? aliases = null)
    {
        if (m_sql == null)
        {
            if (sResourceConnString == null)
                throw new SqlExceptionNoConnection();

            m_sql = Sql.OpenConnection(sResourceConnString);
            m_fAttached = false;
        }

        if (m_sql == null)
            throw new SqlCore.SqlException("could not open sql connection");

        SqlCommand sqlcmd = m_sql.CreateCommandInternal();

        sqlcmd.CommandText = sQuery;
        sqlcmd.Transaction = m_sql.Transaction;

        if (customizeDelegate != null)
            customizeDelegate(sqlcmd);

        if (m_reader != null)
            m_reader.Close();

        try
        {
            m_reader = sqlcmd.ExecuteReaderInternal()._Reader;
        }
        catch (Exception exc)
        {
            throw new SqlCore.SqlException(m_crids, exc, "caught exception executing reader");
        }
    }

    public delegate void DelegateReader<T>(SqlReader sqlr, Guid crids, ref T t);
    public delegate void DelegateMultiSetReader<T>(SqlReader sqlr, Guid crids, int recordSet, ref T t);

    /// <summary>
    /// Execute the given query. This supports queries that return multiple recordsets.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="sql">already connected SQL object</param>
    /// <param name="crids">short correlation id (guid)</param>
    /// <param name="sQuery">t-sql query (multiple recordsets ok)</param>
    /// <param name="delegateReader">delegate that will be called for every record</param>
    /// <param name="customizeDelegate">optional customization delegate (for adding parameter values)</param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    /// <exception cref="SqlExceptionNoResults"></exception>

    /*----------------------------------------------------------------------------
        %%Function: Close
        %%Qualified: TCore.SqlReader.Close
    ----------------------------------------------------------------------------*/
    public void Close()
    {
        if (m_reader != null)
        {
            m_reader.Close();
            m_reader.Dispose();
        }

        if (!m_fAttached)
        {
            ((ISql?)m_sql)?.Close();
            m_sql = null;
        }
    }

    public Int16 GetInt16(int index) => _Reader.GetInt16(index);
    public Int32 GetInt32(int index) => _Reader.GetInt32(index);
    public string GetString(int index) => _Reader.GetString(index);
    public Guid GetGuid(int index) => _Reader.GetGuid(index);
    public double GetDouble(int index) => _Reader.GetDouble(index);
    public Int64 GetInt64(int index) => _Reader.GetInt64(index);
    public DateTime GetDateTime(int index) => _Reader.GetDateTime(index);

    public Int16? GetNullableInt16(int index) => _Reader.IsDBNull(index) ? null : _Reader.GetInt16(index);
    public Int32? GetNullableInt32(int index) => _Reader.IsDBNull(index) ? null : _Reader.GetInt32(index);
    public string? GetNullableString(int index) => _Reader.IsDBNull(index) ? null : _Reader.GetString(index);
    public Guid? GetNullableGuid(int index) => _Reader.IsDBNull(index) ? null : _Reader.GetGuid(index);
    public double? GetNullableDouble(int index) => _Reader.IsDBNull(index) ? null : _Reader.GetDouble(index);
    public Int64? GetNullableInt64(int index) => _Reader.IsDBNull(index) ? null : _Reader.GetInt64(index);
    public DateTime? GetNullableDateTime(int index) => _Reader.IsDBNull(index) ? null : _Reader.GetDateTime(index);

    public bool IsDBNull(int index) => _Reader.IsDBNull(index);

    public int GetFieldCount() => _Reader.FieldCount;
    public string GetFieldName(int index) => _Reader.GetName(index);
    public object GetNativeValue(int index) => _Reader.GetSqlValue(index);

    private static readonly Dictionary<Type, Type> s_mapAffinityTypes =
        new()
        {
            { typeof(Int16), typeof(Int64) },
            { typeof(Int32), typeof(Int64) },
            { typeof(Int64), typeof(Int64) },
            { typeof(int), typeof(Int64) },
            { typeof(string), typeof(string) },
            { typeof(float), typeof(double) },
            { typeof(double), typeof(double) },
            { typeof(DateTime), typeof(DateTime) },
            { typeof(bool), typeof(Int64) },
            { typeof(Guid), typeof(string) }
        };

    public Type GetFieldAffinity(int index)
    {
        Type? type = _Reader.GetFieldType(index);

        if (type == null)
            return typeof(void);

        if (s_mapAffinityTypes.TryGetValue(type, out Type? affinity))
            return affinity;

        return typeof(void);
    }

    public Type GetFieldType(int index) => _Reader.GetFieldType(index) ?? typeof(void);

    public bool NextResult() => m_reader?.NextResult() ?? false;
    public bool Read() => m_reader?.Read() ?? false;
    public SqlDataReader Reader => m_reader ?? throw new SqlExceptionNoReader();
}