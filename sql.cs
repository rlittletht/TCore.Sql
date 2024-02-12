using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using System.Data.SqlTypes;
using System.Diagnostics;
using System.Text.RegularExpressions;
using NUnit.Framework;
using TCore.Exceptions;
using Microsoft.Data.SqlClient;
using TCore.SqlCore;

namespace TCore.SqlClient;


// ===============================================================================
//  I  Q U E R Y  R E S U L T 
// ===============================================================================
public interface IQueryResult
{
    bool FAddResultRow(ISqlReader sqlr, int iRecordSet);
}

public class Sql: ISql
{
    public SqlConnection Connection => m_connection ?? throw new SqlExceptionNoConnection();
    public ISqlTransaction? Transaction => m_transaction;
    public bool InTransaction => m_transaction != null;

    private readonly SqlConnection? m_connection;
    private SqlTransaction? m_transaction;

    public Sql()
    {
        m_connection = null;
        m_transaction = null; 
    }

    public Sql(SqlConnection? connection, SqlTransaction? transaction)
    {
        m_connection = connection;
        m_transaction = transaction;
    }

    #region Connection Management

    /*----------------------------------------------------------------------------
        %%Function: OpenConnection
        %%Qualified: TCore.Sql.OpenConnection
    ----------------------------------------------------------------------------*/
    public static Sql OpenConnection(string sResourceConnString)
    {
        SqlConnection sqlc = new(sResourceConnString);

        sqlc.Open();

        return new Sql(sqlc, null);
    }

    /*----------------------------------------------------------------------------
        %%Function: SrSetupStaticSql
        %%Qualified: TCore.Sql.SrSetupStaticSql
    ----------------------------------------------------------------------------*/
    public static Sql SetupStaticSql(Sql? sqlIn, string sResourceConnString, out bool fLocalSql)
    {
        fLocalSql = false;

        if (sqlIn == null)
        {
            sqlIn = OpenConnection(sResourceConnString);
            fLocalSql = true;
        }

        return sqlIn;
    }

    /*----------------------------------------------------------------------------
        %%Function: SrReleaseStaticSql
        %%Qualified: TCore.Sql.SrReleaseStaticSql
    ----------------------------------------------------------------------------*/
    public static void ReleaseStaticSql(ref Sql sql, bool fLocalSql)
    {
        if (fLocalSql)
            sql.Close();
    }

    /*----------------------------------------------------------------------------
        %%Function: Close
        %%Qualified: TCore.Sql.Close
    ----------------------------------------------------------------------------*/
    public void Close()
    {
        if (InTransaction)
            throw new SqlExceptionNotInTransaction("can't close with pending transaction");

        m_connection!.Close();
        m_connection.Dispose();
    }

    #endregion

    #region Execute Non-Queries
    /*----------------------------------------------------------------------------
        %%Function: ExecuteNonQuery
        %%Qualified: TCore.Sql.ExecuteNonQuery
    ----------------------------------------------------------------------------*/
    public static void ExecuteNonQuery(
        Sql sql,
        SqlCommandTextInit cmdText,
        string sResourceConnString,
        CustomizeCommandDelegate? customizeParams = null)
    {
        ExecuteNonQuery(sql, cmdText.CommandText, sResourceConnString, customizeParams, cmdText.Aliases);
    }

    /*----------------------------------------------------------------------------
        %%Function: ExecuteNonQuery
        %%Qualified: TCore.Sql.ExecuteNonQuery

        Execute the given non query.  There is only a failed/success response
    ----------------------------------------------------------------------------*/
    private static void ExecuteNonQuery(
        Sql? sql,
        string query,
        string sResourceConnString,
        CustomizeCommandDelegate? customizeParams = null,
        TableAliases? aliases = null)
    {
        sql = SetupStaticSql(sql, sResourceConnString, out bool fLocalSql);

        ISqlCommand sqlcmd = sql.CreateCommand();

        try
        {
            sqlcmd.CommandText = aliases?.ExpandAliases(query) ?? query;
            if (customizeParams != null)
                customizeParams(sqlcmd);

            sqlcmd.Transaction = sql.Transaction;
            sqlcmd.ExecuteNonQuery();
        }
        finally
        {
            sqlcmd.Close();
            ReleaseStaticSql(ref sql, fLocalSql);
        }
    }

    public void ExecuteNonQuery(
        SqlCommandTextInit cmdText,
        CustomizeCommandDelegate? customizeParams = null)
    {
        ExecuteNonQuery(cmdText.CommandText, customizeParams, cmdText.Aliases);
    }

    /*----------------------------------------------------------------------------
        %%Function: ExecuteNonQuery
        %%Qualified: TCore.Sql.ExecuteNonQuery
    ----------------------------------------------------------------------------*/
    public void ExecuteNonQuery(
        string commandText,
        CustomizeCommandDelegate? customizeParams = null,
        TableAliases? aliases = null)
    {
        ISqlCommand sqlcmd = CreateCommand();

        try
        {
            sqlcmd.CommandText = aliases?.ExpandAliases(commandText) ?? commandText;
            if (customizeParams != null)
                customizeParams(sqlcmd);

            if (Transaction != null)
                sqlcmd.Transaction = Transaction;
            sqlcmd.ExecuteNonQuery();
        }
        finally
        {
            sqlcmd.Close();
        }
    }

    public static void ExecuteNonQuery(
        SqlCommandTextInit cmdText,
        string sResourceConnString)
    {
        ExecuteNonQuery(null, cmdText.CommandText, sResourceConnString, null, cmdText.Aliases);
    }

    /*----------------------------------------------------------------------------
        %%Function: ExecuteNonQuery
        %%Qualified: TCore.Sql.ExecuteNonQuery
    ----------------------------------------------------------------------------*/
    public static void ExecuteNonQuery(
        string commandText,
        string sResourceConnString,
        TableAliases? aliases = null)
    {
        ExecuteNonQuery(null, commandText, sResourceConnString, null, aliases);
    }
    #endregion

    #region Execute Scalars

    private static int NExecuteScalar(
        Sql sql,
        SqlCommandTextInit cmdText,
        string sResourceConnString,
        int nDefaultValue)
    {
        return NExecuteScalar(sql, cmdText.CommandText, sResourceConnString, nDefaultValue, cmdText.Aliases);
    }

    /*----------------------------------------------------------------------------
        %%Function: NExecuteScalar
        %%Qualified: TCore.Sql.NExecuteScalar
    ----------------------------------------------------------------------------*/
    private static int NExecuteScalar(
        Sql sql,
        string s,
        string sResourceConnString,
        int nDefaultValue,
        TableAliases? aliases = null)
    {
        try
        {
            sql = SetupStaticSql(sql, sResourceConnString, out bool fLocalSql);

            int nRet = sql.NExecuteScalar(s, aliases);
            ReleaseStaticSql(ref sql, fLocalSql);

            return nRet;
        }
        catch
        {
            return nDefaultValue;
        }
    }

    /*----------------------------------------------------------------------------
        %%Function: NExecuteScalar
        %%Qualified: TCore.Sql.NExecuteScalar

           Execute the scalar command, returning the result.  
    ----------------------------------------------------------------------------*/
    public int NExecuteScalar(SqlCommandTextInit cmdText)
    {
        return NExecuteScalar(cmdText.CommandText, cmdText.Aliases);
    }

    private int NExecuteScalar(string sQuery, TableAliases? aliases = null)
    {
        ISqlCommand sqlcmd = CreateCommand();

        try
        {
            sqlcmd.CommandText = aliases?.ExpandAliases(sQuery) ?? sQuery;
            sqlcmd.Transaction = Transaction;

            return (int)sqlcmd.ExecuteScalar();
        }
        finally
        {
            sqlcmd.Close();
        }
    }

    /*----------------------------------------------------------------------------
        %%Function: SExecuteScalar
        %%Qualified: TCore.Sql.SExecuteScalar

        Execute the scalar command, returning the result.  
    ----------------------------------------------------------------------------*/
    public string SExecuteScalar(SqlCommandTextInit cmdText)
    {
        return SExecuteScalar(cmdText.CommandText, cmdText.Aliases);
    }

    private string SExecuteScalar(string sQuery, TableAliases? aliases = null)
    {
        ISqlCommand sqlcmd = CreateCommand();

        try
        {
            sqlcmd.CommandText = aliases?.ExpandAliases(sQuery) ?? sQuery;
            sqlcmd.Transaction = this.Transaction;

            return (string)sqlcmd.ExecuteScalar();
        }
        finally
        {
            sqlcmd.Close();
        }
    }

    /*----------------------------------------------------------------------------
        %%Function: DttmExecuteScalar
        %%Qualified: TCore.Sql.DttmExecuteScalar
    ----------------------------------------------------------------------------*/
    public DateTime DttmExecuteScalar(SqlCommandTextInit cmdText)
    {
        return DttmExecuteScalar(cmdText.CommandText, cmdText.Aliases);
    }


    private DateTime DttmExecuteScalar(string sQuery, TableAliases? aliases = null)
    {
        ISqlCommand sqlcmd = CreateCommand();
        try
        {
            sqlcmd.CommandText = aliases?.ExpandAliases(sQuery) ?? sQuery;
            sqlcmd.Transaction = this.Transaction;

            return (DateTime)sqlcmd.ExecuteScalar();
        }
        finally
        {
            sqlcmd.Close();
        }
    }

    #endregion

    #region Transactions

    /* B E G I N  T R A N S A C T I O N */
    /*----------------------------------------------------------------------------
        %%Function: BeginTransaction
        %%Qualified: BhSvc.Sql.BeginTransaction
        %%Contact: rlittle

    ----------------------------------------------------------------------------*/
    public void BeginTransaction()
    {
        if (InTransaction)
            throw new SqlExceptionInTransaction("cannot nest transactions");

        m_transaction = new SqlTransaction(Connection.BeginTransaction());
    }

    public void BeginExclusiveTransaction() => throw new SqlExceptionNotImplementedInThisClient();

    /*----------------------------------------------------------------------------
        %%Function: Rollback
        %%Qualified: TCore.Sql.Rollback
    ----------------------------------------------------------------------------*/
    public void Rollback()
    {
        try
        {
            if (!InTransaction)
                throw new SqlExceptionNotInTransaction("can't rollback if not in transaction");

            m_transaction!.Rollback();
        }
        finally
        {
            m_transaction?.Dispose();
            m_transaction = null;
        }
    }

    /*----------------------------------------------------------------------------
        %%Function: Commit
        %%Qualified: TCore.Sql.Commit
    ----------------------------------------------------------------------------*/
    public void Commit()
    {
        try
        {
            if (!InTransaction)
                throw new SqlExceptionNotInTransaction("can't commit if not in transaction");

            m_transaction!.Commit();
        }
        finally
        {
            m_transaction?.Dispose();
            m_transaction = null;
        }
    }
    #endregion


    /*----------------------------------------------------------------------------
        %%Function: CreateCommand
        %%Qualified: TCore.Sql.CreateCommand
    ----------------------------------------------------------------------------*/
    public ISqlCommand CreateCommand()
    {
        return new SqlCommand(Connection.CreateCommand());
    }

    /*----------------------------------------------------------------------------
        %%Function: ExecuteReader
        %%Qualified: TCore.Sql.ExecuteReader
    ----------------------------------------------------------------------------*/
    public void ExecuteReader(
        string sQuery,
        out SqlReader sqlr,
        string sResourceConnString,
        TableAliases? aliases = null)
    {
        sqlr = new SqlReader(this);

        sqlr.ExecuteQuery(sQuery, sResourceConnString, null, aliases);
    }

    #region Queries

    /*----------------------------------------------------------------------------
        %%Function: ExecuteQuery
        %%Qualified: TCore.Sql.ExecuteQuery
    ----------------------------------------------------------------------------*/
    private static void ExecuteQuery(
        string sQuery,
        IQueryResult iqr,
        string sResourceConnString, 
        TableAliases? aliases = null)
    {
        Sql sql = Sql.OpenConnection(sResourceConnString);

        ExecuteQuery(sql, sQuery, iqr, sResourceConnString, aliases);
    }

    /*----------------------------------------------------------------------------
        %%Function: ExecuteQuery
        %%Qualified: TCore.Sql.ExecuteQuery

        Execute the given query and send the results to IQueryResult...
    ----------------------------------------------------------------------------*/
    private static void ExecuteQuery(
        Sql sql,
        string sQuery,
        IQueryResult iqr,
        string sResourceConnString,
        TableAliases? aliases = null,
        CustomizeCommandDelegate? customizeDelegate = null)
    {
        SqlReader sqlr;
        int iRecordSet = 0;

        sqlr = new SqlReader(sql);

        sqlr.ExecuteQuery(sQuery, sResourceConnString, customizeDelegate, aliases);

        do
        {
            while (sqlr.Reader.Read())
            {
                if (!iqr.FAddResultRow(sqlr, iRecordSet))
                    throw new SqlCore.SqlException("FAddResultRow failed!");
            }
            iRecordSet++;
        } while (sqlr.Reader.NextResult());

        sqlr.Close();
    }

    /*----------------------------------------------------------------------------
        %%Function: DoGenericQueryDelegateRead
        %%Qualified: TCore.Sql.SqlClient.Sql.DoGenericQueryDelegateRead<T>
    ----------------------------------------------------------------------------*/
    public T DoGenericQueryDelegateRead<T>(
        Guid crids,
        string query,
        ISqlReader.DelegateReader<T> delegateReader,
        TableAliases? aliases = null,
        CustomizeCommandDelegate? customizeDelegate = null) where T : new()
    {
        SqlSelect selectTags = new SqlSelect();

        selectTags.AddBase(query);
        if (aliases != null)
            selectTags.AddAliases(aliases);

        string sQuery = selectTags.ToString();

        SqlReader? sqlr = null;

        if (delegateReader == null)
            throw new Exception("must provide delegate reader");

        try
        {
            string sCmd = sQuery;

            sqlr = new(this);
            sqlr.ExecuteQuery(sQuery, null, customizeDelegate);

            T t = new();
            bool fOnce = false;

            while (sqlr.Read())
            {
                delegateReader(sqlr, crids, ref t);
                fOnce = true;
            }

            if (!fOnce)
                throw new SqlExceptionNoResults();

            return t;
        }
        finally
        {
            sqlr?.Close();
        }
    }

    /*----------------------------------------------------------------------------
        %%Function: DoGenericQueryDelegateRead
        %%Qualified: TCore.SqlReader.DoGenericQueryDelegateRead<T>
    ----------------------------------------------------------------------------*/
    public T DoGenericMultiSetQueryDelegateRead<T>(
        Guid crids,
        string sQuery,
        ISqlReader.DelegateMultiSetReader<T> delegateReader,
        CustomizeCommandDelegate? customizeDelegate = null) where T : new()
    {
        SqlReader? reader = null;

        if (delegateReader == null)
            throw new Exception("must provide delegate reader");

        try
        {
            string sCmd = sQuery;

            reader = new(this);
            reader.ExecuteQuery(sQuery, null, customizeDelegate);

            int recordSet = 0;

            T t = new();
            do
            {
                bool fOnce = false;

                while (reader.Reader.Read())
                {
                    delegateReader(reader, crids, recordSet, ref t);
                    fOnce = true;
                }

                if (!fOnce)
                    throw new SqlExceptionNoResults();

                recordSet++;
            } while (reader.Reader.NextResult());

            return t;
        }
        finally
        {
            reader?.Close();
        }
    }
    #endregion
}