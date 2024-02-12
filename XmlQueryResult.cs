using System;
using System.Text;
using System.Text.RegularExpressions;
using TCore.SqlCore;

namespace TCore.SqlClient;

// ================================================================================
// X M L  Q U E R Y  R E S U L T
// ================================================================================

// implementation of IQueryResult for XML results
class XmlQueryResult : IQueryResult
{
    StringBuilder m_sb;

    /* F  A D D  R E S U L T  R O W */
    /*----------------------------------------------------------------------------
        %%Function: StatsSvc.StatsQuery.FAddResultRow
        %%Contact: rlittle

        Implemenation if IQueryResult.FAddResultRow for XML results (adds the
        current result to a string containing XML)

        This only handles 1 recordset
    ----------------------------------------------------------------------------*/
    public bool FAddResultRow(ISqlReader sqlr, int iRecordSet)
    {
        if (iRecordSet != 0)
            throw new Exception("internal error - XmlQueryResult should never get multiple recordsets!");

        m_sb.Append("<line>");
        for (int i = 0, iMac = sqlr.GetFieldCount(); i < iMac; i++)
        {
            m_sb.Append("<");
            m_sb.Append(Regex.Replace(sqlr.GetFieldName(i), " ", "_"));
            m_sb.Append(">");
            Object o = sqlr.GetNativeValue(i);
            m_sb.Append(o.ToString());
            m_sb.Append("</");
            m_sb.Append(Regex.Replace(sqlr.GetFieldName(i), " ", "_"));
            m_sb.Append(">");
        }
        m_sb.Append("</line>");
        return true;
    }

    /* X M L  Q U E R Y  R E S U L T */
    /*----------------------------------------------------------------------------
        %%Function: StatsSvc.StatsQuery.XmlQueryResult
        %%Contact: rlittle

    ----------------------------------------------------------------------------*/
    public XmlQueryResult()
    {
        m_sb = new StringBuilder(2048);
    }

    /* B E G I N  R E S U L T  S E T */
    /*----------------------------------------------------------------------------
        %%Function: StatsSvc.StatsQuery.BeginResultSet
        %%Contact: rlittle

    ----------------------------------------------------------------------------*/
    public void BeginResultSet()
    {
        m_sb.Append("<results>");
    }

    /* E N D  R E S U L T  S E T */
    /*----------------------------------------------------------------------------
        %%Function: StatsSvc.StatsQuery.EndResultSet
        %%Contact: rlittle

    ----------------------------------------------------------------------------*/
    public void EndResultSet()
    {
        m_sb.Append("</results>");
    }

    public string XML { get { return m_sb.ToString(); } }
}