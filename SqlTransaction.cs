using TCore.SqlCore;

namespace TCore.SqlClient;

public class SqlTransaction: ISqlTransaction
{
    public Microsoft.Data.SqlClient.SqlTransaction _Transaction;

    public SqlTransaction(Microsoft.Data.SqlClient.SqlTransaction transaction)
    {
        _Transaction = transaction;
    }

    void ISqlTransaction.Rollback() => _Transaction.Rollback();
    void ISqlTransaction.Commit() => _Transaction.Commit();
    void ISqlTransaction.Dispose() => _Transaction.Dispose();
}
