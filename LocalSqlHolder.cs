using System;
using TCore.Exceptions;
using TCore.SqlCore;

namespace TCore.SqlClient;

// The idea here is that you can create one of these on your stack and when it passes out of
// scope, the local SQL will get closed for you. To make this work, you MUST use "using"
//
// e.g.

public class LocalSqlHolder: ISql
{
    private readonly ISql m_sql;
    private readonly bool m_fLocal;

    public ISql Sql => m_sql;
    public Guid Crids { get; }

    public LocalSqlHolder(ISql? sql, Guid crids, string sConnectionString)
    {
        Crids = crids;
        m_sql = SqlClient.Sql.SetupStaticSql(sql, sConnectionString, out m_fLocal);
    }

    public bool InTransaction => m_sql.InTransaction;

    public ISqlTransaction? Transaction => m_sql.Transaction;

    public ISqlCommand CreateCommand() => m_sql.CreateCommand();
    public ISqlReader CreateReader() => m_sql.CreateReader();

    public void ExecuteNonQuery(string commandText, CustomizeCommandDelegate? customizeParams = null, TableAliases? aliases = null)
    {
        m_sql.ExecuteNonQuery(commandText, customizeParams, aliases);
    }

    public void ExecuteNonQuery(SqlCommandTextInit commandText, CustomizeCommandDelegate? customizeParams = null)
    {
        m_sql.ExecuteNonQuery(commandText, customizeParams);
    }

    public ISqlReader ExecuteQuery(
        Guid crids, string query, TableAliases? aliases = null, CustomizeCommandDelegate? customizeDelegate = null) =>
        m_sql.ExecuteQuery(crids, query, aliases, customizeDelegate);

    public T ExecuteDelegatedQuery<T>(
        Guid crids, string query, ISqlReader.DelegateReader<T> delegateReader, TableAliases? aliases = null, CustomizeCommandDelegate? customizeDelegate = null) where T : new() =>
        m_sql.ExecuteDelegatedQuery(crids, query, delegateReader, aliases, customizeDelegate);

    public T ExecuteMultiSetDelegatedQuery<T>(Guid crids, string sQuery, ISqlReader.DelegateMultiSetReader<T> delegateReader, TableAliases? aliases = null, CustomizeCommandDelegate? customizeDelegate = null) where T : new() => m_sql.ExecuteMultiSetDelegatedQuery(crids, sQuery, delegateReader, aliases, customizeDelegate);

    public string SExecuteScalar(SqlCommandTextInit cmdText) => m_sql.SExecuteScalar(cmdText);

    public int NExecuteScalar(SqlCommandTextInit cmdText) => m_sql.NExecuteScalar(cmdText);

    public DateTime DttmExecuteScalar(SqlCommandTextInit cmdText) => m_sql.DttmExecuteScalar(cmdText);

    public void BeginExclusiveTransaction()
    {
        m_sql.BeginExclusiveTransaction();
    }

    public void BeginTransaction()
    {
        m_sql.BeginTransaction();
    }

    public void Rollback()
    {
        m_sql.Rollback();
    }

    public void Commit()
    {
        m_sql.Commit();
    }

    public void Close()
    {
        if (m_fLocal)
            Sql.Close();
    }
}