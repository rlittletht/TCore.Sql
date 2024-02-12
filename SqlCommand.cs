using Microsoft.Data.SqlClient;
using TCore.SqlCore;

namespace TCore.SqlClient;

public class SqlCommand : ISqlCommand
{
    private readonly Microsoft.Data.SqlClient.SqlCommand m_command;
    private ISqlTransaction? m_transaction;

    public string CommandText
    {
        get => m_command.CommandText;
        set => m_command.CommandText = value;
    }

    public ISqlTransaction? Transaction
    {
        get => m_transaction;
        set => m_transaction = value;
    }

    public ISqlReader ExecuteReader() => new SqlReader(m_command.ExecuteReader());

    public SqlReader ExecuteReaderInternal() => new SqlReader(m_command.ExecuteReader());

    public SqlCommand(Microsoft.Data.SqlClient.SqlCommand command)
    {
        m_command = command;
    }

    public int ExecuteNonQuery() => m_command.ExecuteNonQuery();

    public object ExecuteScalar() => m_command.ExecuteScalar();

    public void AddParameterWithValue(string parameterName, object? value) => m_command.Parameters.AddWithValue(parameterName, value);

    public void Close()
    {
        m_command.Dispose();
    }
}
