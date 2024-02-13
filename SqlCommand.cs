using Microsoft.Data.SqlClient;
using TCore.SqlCore;

namespace TCore.SqlClient;

public class SqlCommand : ISqlCommand
{
    private readonly Microsoft.Data.SqlClient.SqlCommand m_command;
    private ISqlTransaction? m_transaction;

    string ISqlCommand.CommandText
    {
        get => m_command.CommandText;
        set => m_command.CommandText = value;
    }

    ISqlTransaction? ISqlCommand.Transaction
    {
        get => m_transaction;
        set => m_transaction = value;
    }

    ISqlReader ISqlCommand.ExecuteReader() => new SqlReader(m_command.ExecuteReader());

    public SqlReader ExecuteReaderInternal() => new SqlReader(m_command.ExecuteReader());

    public SqlCommand(Microsoft.Data.SqlClient.SqlCommand command)
    {
        m_command = command;
    }

    int ISqlCommand.ExecuteNonQuery() => m_command.ExecuteNonQuery();

    object ISqlCommand.ExecuteScalar() => m_command.ExecuteScalar();

    void ISqlCommand.AddParameterWithValue(string parameterName, object? value) => m_command.Parameters.AddWithValue(parameterName, value);

    void ISqlCommand.Close()
    {
        m_command.Dispose();
    }
}
