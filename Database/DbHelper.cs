using System;
using System.Data;
using MySql.Data.MySqlClient;

namespace baranggaysystem1.Database
{
    internal static class DbHelper
    {
        public static DataTable LoadTable(string sql, Action<MySqlCommand>? configure = null)
        {
            using var conn = DBConnection.GetConnection();
            conn.Open();
            using var cmd = new MySqlCommand(sql, conn);
            configure?.Invoke(cmd);
            using var adapter = new MySqlDataAdapter(cmd);
            var table = new DataTable();
            adapter.Fill(table);
            return table;
        }

        public static int ExecuteNonQuery(string sql, Action<MySqlCommand>? configure = null)
        {
            using var conn = DBConnection.GetConnection();
            conn.Open();
            using var cmd = new MySqlCommand(sql, conn);
            configure?.Invoke(cmd);
            return cmd.ExecuteNonQuery();
        }

        public static T? ExecuteScalar<T>(string sql, Action<MySqlCommand>? configure = null)
        {
            using var conn = DBConnection.GetConnection();
            conn.Open();
            using var cmd = new MySqlCommand(sql, conn);
            configure?.Invoke(cmd);
            var result = cmd.ExecuteScalar();
            if (result == null || result == DBNull.Value)
            {
                return default;
            }

            return (T)Convert.ChangeType(result, typeof(T));
        }
    }
}
