using System.Data;
using System.Data.SqlClient;

namespace CPUFrameWork
{
    public class SQLUtility
    {
        public static string ConnectionString = "";
        public static DataTable GetDataTable(string sqlcmd)
        {
            DataTable dt = new();
            SqlConnection conn = new();
            conn.ConnectionString = ConnectionString;
            conn.Open();

            SqlCommand cmd = new();
            cmd.CommandText = sqlcmd;
            cmd.Connection = conn;
            var dr = cmd.ExecuteReader();
            dt.Load(dr);
            return dt;
        }
    }
}
