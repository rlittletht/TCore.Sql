using System;
using TCore.Exceptions;

namespace TCore.SqlClient;

// The idea here is that you can create one of these on your stack and when it passes out of
// scope, the local SQL will get closed for you. To make this work, you MUST use "using"
//
// e.g.

public class LocalSqlHolder
{
    private Sql m_sql;
    private readonly bool m_fLocal;

    public Sql Sql => m_sql;
    public Guid Crids { get; }

    public static implicit operator Sql(LocalSqlHolder lsh)
    {
        return lsh.Sql;
    }

    public LocalSqlHolder(Sql? sql, Guid crids, string sConnectionString)
    {
        Crids = crids;
        m_sql = Sql.SetupStaticSql(sql, sConnectionString, out m_fLocal);
    }

    public void Close()
    {
        Sql.ReleaseStaticSql(ref m_sql, m_fLocal);
    }
}