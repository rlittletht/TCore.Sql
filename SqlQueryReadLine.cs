﻿

using System;

namespace TCore
{
    // Convenient for running a query and reading a single result row into a struct.
    //
    // To use, create this object and pass to Sql.ExecuteQuery.
    //
    // NOTE: This will throw SqlExceptionNotSingleRow if there is not exactly one result row
    public class SqlQueryReadLine<TResult> : IQueryResult where TResult : struct
    {
        private TResult m_tResult;
        private int m_cRowsRead = 0;

        public delegate void ReadLine(SqlReader sqlr, ref TResult tResult);
        
        ReadLine m_read;

        public SqlQueryReadLine(ReadLine read)
        {
            m_read = read;
        }

        public bool FAddResultRow(SqlReader sqlr, int iRecordSet)
        {
            if (m_cRowsRead++ != 0)
                throw new TcSqlExceptionNotSingleRow();

            m_read(sqlr, ref m_tResult);
            return true;
        }

        public TResult Value
        {
            get
            {
                if (m_cRowsRead != 1)
                    throw new TcSqlExceptionNoResults();

                return m_tResult; 
            }
        }
    }
}