using System;
using System.Data;
using System.Data.SqlClient;
using System.Configuration; // LIBRERÍA NECESARIA PARA LEER EL APP.CONFIG

namespace SISTEMA_ACUMULATIVAS.Conexion
{
    public class ClsConexion
    {
        // El sistema ahora lee la conexión desde el App.config en lugar de tenerla quemada
        private readonly string _connectionString = ConfigurationManager.ConnectionStrings["NotariaDB"].ConnectionString;

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