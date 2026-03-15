using System;
using System.Data;
using MySql.Data.MySqlClient;

namespace baranggaysystem1.Database
{
    internal static class DbHelper
    {
        public static DataTable LoadTable(string sql, Action<MySqlCommand>? configure = null)
        {
        var parameters = DbParameterMapper.Capture(configure);
        // return cached result if available, otherwise fetch and automatically cache
        if (DatabaseManager.TryGetCachedTable(sql, parameters, out DataTable cached))
        {
            return cached;
        }

        DataTable fresh = DatabaseManager.Select(sql, parameters);
        // store copy in cache for future
        DatabaseManager.SelectCached(sql, parameters); // side-effect caching
        return fresh;
        }

        public static int ExecuteNonQuery(string sql, Action<MySqlCommand>? configure = null)
        {
            return DatabaseManager.Execute(sql, DbParameterMapper.Capture(configure));
        }

        public static T? ExecuteScalar<T>(string sql, Action<MySqlCommand>? configure = null)
        {
            return DatabaseManager.ExecuteScalar<T>(sql, DbParameterMapper.Capture(configure));
        }
    }
}
