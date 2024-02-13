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

        ((ISqlCommand)sqlcmd).CommandText = sQuery;
        ((ISqlCommand)sqlcmd).Transaction = m_sql.Transaction;

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
    void ISqlReader.Close()
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

    Int16 ISqlReader.GetInt16(int index) => _Reader.GetInt16(index);
    Int32 ISqlReader.GetInt32(int index) => _Reader.GetInt32(index);
    string ISqlReader.GetString(int index) => _Reader.GetString(index);
    Guid ISqlReader.GetGuid(int index) => _Reader.GetGuid(index);
    double ISqlReader.GetDouble(int index) => _Reader.GetDouble(index);
    Int64 ISqlReader.GetInt64(int index) => _Reader.GetInt64(index);
    DateTime ISqlReader.GetDateTime(int index) => _Reader.GetDateTime(index);

    Int16? ISqlReader.GetNullableInt16(int index) => _Reader.IsDBNull(index) ? null : _Reader.GetInt16(index);
    Int32? ISqlReader.GetNullableInt32(int index) => _Reader.IsDBNull(index) ? null : _Reader.GetInt32(index);
    string? ISqlReader.GetNullableString(int index) => _Reader.IsDBNull(index) ? null : _Reader.GetString(index);
    Guid? ISqlReader.GetNullableGuid(int index) => _Reader.IsDBNull(index) ? null : _Reader.GetGuid(index);
    double? ISqlReader.GetNullableDouble(int index) => _Reader.IsDBNull(index) ? null : _Reader.GetDouble(index);
    Int64? ISqlReader.GetNullableInt64(int index) => _Reader.IsDBNull(index) ? null : _Reader.GetInt64(index);
    DateTime? ISqlReader.GetNullableDateTime(int index) => _Reader.IsDBNull(index) ? null : _Reader.GetDateTime(index);

    bool ISqlReader.IsDBNull(int index) => _Reader.IsDBNull(index);

    int ISqlReader.GetFieldCount() => _Reader.FieldCount;
    string ISqlReader.GetFieldName(int index) => _Reader.GetName(index);
    object ISqlReader.GetNativeValue(int index) => _Reader.GetSqlValue(index);

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

    Type ISqlReader.GetFieldAffinity(int index)
    {
        Type? type = _Reader.GetFieldType(index);

        if (type == null)
            return typeof(void);

        if (s_mapAffinityTypes.TryGetValue(type, out Type? affinity))
            return affinity;

        return typeof(void);
    }

    Type ISqlReader.GetFieldType(int index) => _Reader.GetFieldType(index) ?? typeof(void);

    bool ISqlReader.NextResult() => m_reader?.NextResult() ?? false;
    bool ISqlReader.Read() => m_reader?.Read() ?? false;
    public SqlDataReader Reader => m_reader ?? throw new SqlExceptionNoReader();
}