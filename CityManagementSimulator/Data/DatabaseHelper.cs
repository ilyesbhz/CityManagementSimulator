using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;

namespace CityManagementSimulator.Data
{
    public class DatabaseHelper
    {
        private readonly string _conn;
        public DatabaseHelper()
        {
            _conn = ConfigurationManager.ConnectionStrings["CityDB"].ConnectionString;
        }
        public SqlConnection GetConnection() => new SqlConnection(_conn);

        public DataTable ExecuteQuery(string sql, params SqlParameter[] pars)
        {
            using (var c = GetConnection())
            using (var cmd = new SqlCommand(sql, c))
            {
                if (pars != null) cmd.Parameters.AddRange(pars);
                using (var da = new SqlDataAdapter(cmd))
                {
                    var dt = new DataTable();
                    da.Fill(dt);
                    return dt;
                }
            }
        }

        public int ExecuteNonQuery(string sql, params SqlParameter[] pars)
        {
            using (var c = GetConnection())
            using (var cmd = new SqlCommand(sql, c))
            {
                if (pars != null) cmd.Parameters.AddRange(pars);
                c.Open();
                return cmd.ExecuteNonQuery();
            }
        }

        public object ExecuteScalar(string sql, params SqlParameter[] pars)
        {
            using (var c = GetConnection())
            using (var cmd = new SqlCommand(sql, c))
            {
                if (pars != null) cmd.Parameters.AddRange(pars);
                c.Open();
                return cmd.ExecuteScalar();
            }
        }
    }
}