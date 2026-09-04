using System;
using System.Data;
using System.Data.SqlClient;

namespace SISTEMA_ACUMULATIVAS.Conexion
{
    public class ClsConexion
    {
        private readonly string _connectionString = @"Server=GERMAN25\SQLEXPRESS;Database=ACUMULATIVAS_DB;Integrated Security=True;";

        public SqlConnection GetConnection()
        {
            SqlConnection conn = new SqlConnection(_connectionString);
            if (conn.State == ConnectionState.Closed)
            {
                conn.Open();
            }
            return conn;
        }

        public SqlConnection EstablecerConexion()
        {
            return new SqlConnection(_connectionString);
        }

        public bool TestConnection()
        {
            try
            {
                using (SqlConnection conn = GetConnection())
                {
                    return conn.State == ConnectionState.Open;
                }
            }
            catch
            {
                return false;
            }
        }
    }
}