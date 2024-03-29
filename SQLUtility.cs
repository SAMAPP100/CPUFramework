﻿using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Text;

namespace CPUFrameWork
{
    public class SQLUtility
    {
        public static string ConnectionString = "";

        public static SqlCommand GetSqlCommand(string sprocname)
        {
            SqlCommand cmd;

            using (SqlConnection conn = new(ConnectionString))
            {
                cmd = new SqlCommand(sprocname, conn);
                cmd.CommandType = CommandType.StoredProcedure;
                conn.Open();
                SqlCommandBuilder.DeriveParameters(cmd);
            }
            return cmd;
        }
        public static DataTable GetDataTable(SqlCommand cmd)
        {
            return DoExecuteSql(cmd, true);
        }

        private static DataTable DoExecuteSql(SqlCommand cmd, bool loadtable)
        {
            DataTable dt = new DataTable();
            using (SqlConnection conn = new(ConnectionString))
            {
                conn.Open();
                cmd.Connection = conn;
                Debug.Print(GetSql(cmd));
                try
                {

                    SqlDataReader dr = cmd.ExecuteReader();
                    CheckRetrurnValue(cmd);
                    if (loadtable == true)
                    {
                        dt.Load(dr);
                    }

                }
                catch (SqlException ex)
                {
                    string msg = ParseConstraintMsg(ex.Message);
                    throw new Exception(msg);
                }
                catch (InvalidCastException ex)
                {
                    throw new Exception(cmd.CommandText + ": " + ex.Message, ex);
                }
            }
            SetAllColumnsAllowNull(dt);
            return dt;
        }

        private static void CheckRetrurnValue(SqlCommand cmd)
        {
            int returnvalue = 0;
            string msg = "";
            if (cmd.CommandType == CommandType.StoredProcedure)
            {
                foreach (SqlParameter p in cmd.Parameters)
                {
                    if (p.Direction == ParameterDirection.ReturnValue)
                    {
                        if (p.Value != null)
                        {

                            returnvalue = (int)p.Value;
                        }
                    }
                    else if (p.ParameterName.ToLower() == "@message")
                    {
                        if (p.Value != null)
                        {
                            msg = p.Value.ToString();
                        }
                    }
                }
                if (returnvalue == 1)
                {
                    if (msg == "")
                    {
                        msg = $"{cmd.CommandText} did not do action that was requested.";
                    }
                    throw new Exception(msg);
                }
            }

        }

        public static DataTable GetDataTable(string sqlcmd)
        {
            return DoExecuteSql(new SqlCommand(sqlcmd), true);
        }

        public static void ExecuteSQL(SqlCommand sqlcmd)
        {
            DoExecuteSql(sqlcmd, false);
        }

        public static void ExecuteSQL(string sqlcmd)
        {
            GetDataTable(sqlcmd);
        }

        public static void SetParameterValue(SqlCommand cmd, string paramname, object value)
        {
            try
            {

                cmd.Parameters[paramname].Value = value;
            }
            catch (Exception ex)
            {

                throw new Exception(cmd.CommandText + ": " + ex.Message, ex);
            }
        }

        private static string ParseConstraintMsg(string msg)
        {
            string origmsg = msg;
            string prefix = "ck_";
            string msgend = "";
            if (!msg.Contains(prefix))
            {
                if (msg.Contains("u_"))
                {
                    prefix = "u_";
                    msgend = " must be unique";
                }
                else if (msg.Contains("f_"))
                {
                    prefix = "f_";
                }
            }
            if (msg.Contains(prefix))
            {
                msg = msg.Replace("\"", "'");
                int pos = msg.IndexOf(prefix) + prefix.Length;
                msg = msg.Substring(pos);
                pos = msg.IndexOf("'");
                if (pos == -1)
                {
                    msg = origmsg;
                }
                else
                {

                    msg = msg.Substring(0, pos);
                    msg = msg.Replace("_", " ");
                    msg += msgend;
                    if (prefix == "f_")
                    {
                        var words = msg.Split(' ');
                        if (words.Length > 1)
                        {
                            msg = $"Cannot delete {words[0]} because it has a related {words[1]} record";
                        }
                    }
                }
            }
            return msg;
        }
        public static int GetFirstColunmFirstRowValue(string sql)
        {

            int n = 0;
            DataTable dt = GetDataTable(sql);

            if (dt.Rows.Count > 0 && dt.Columns.Count > 0)
            {
                if (dt.Rows[0][0] != DBNull.Value)
                {

                    int.TryParse(dt.Rows[0][0].ToString(), out n);
                }

            }

            return n;

        }

        private static void SetAllColumnsAllowNull(DataTable dt)
        {
            foreach (DataColumn c in dt.Columns)
            {
                c.AllowDBNull = true;
            }
        }

        public static string GetSql(SqlCommand cmd)
        {
            string val = "";
#if DEBUG
            StringBuilder sb = new StringBuilder();

            if (cmd.Connection != null)
            {
                sb.AppendLine($"-- {cmd.Connection.DataSource}");
                sb.AppendLine($"use {cmd.Connection.Database}");
                sb.AppendLine("GO");
            }
            if (cmd.CommandType == CommandType.StoredProcedure)
            {
                sb.AppendLine($"exec {cmd.CommandText}");
                int paramcount = cmd.Parameters.Count - 1;
                int paramnum = 0;
                string comma = ",";
                foreach (SqlParameter p in cmd.Parameters)
                {
                    if (p.Direction != ParameterDirection.ReturnValue)
                    {
                        if (paramnum == paramcount) comma = "";
                        sb.AppendLine($"{p.ParameterName} = {(p.Value == null ? "null" : p.Value.ToString())}{comma}");

                    }
                    paramnum++;
                }
            }
            else
            {
                sb.AppendLine($"{cmd.CommandText}");
            }
            val = sb.ToString();
#endif
            return val;
        }

        public static void DebugPrintDataTable(DataTable dt)
        {
            foreach (DataRow r in dt.Rows)
            {
                foreach (DataColumn c in dt.Columns)
                {
                    Debug.Print(c.ColumnName + " " + r[c.ColumnName].ToString());
                }
            }
        }
    }
}
//
