using TCore.SqlCore;

namespace TCore.SqlClient;

public class SqlTransaction: ISqlTransaction
{
    public Microsoft.Data.SqlClient.SqlTransaction _Transaction;

    public SqlTransaction(Microsoft.Data.SqlClient.SqlTransaction transaction)
    {
        _Transaction = transaction;
    }

    public void Rollback() => _Transaction.Rollback();
    public void Commit() => _Transaction.Commit();
    public void Dispose() => _Transaction.Dispose();
}
