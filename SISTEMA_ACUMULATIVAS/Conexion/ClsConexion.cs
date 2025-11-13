using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SISTEMA_ACUMULATIVAS.Conexion
{
    public class ClsConexion
    {
        // CAMBIO IMPORTANTE: Apuntando a la BD correcta
        private readonly string _connectionString = @"Server=GERMAN25\SQLEXPRESS;Database=ACUMULATIVAS_DB;Integrated Security=True;";
        public SqlConnection GetConnection()
        {
            SqlConnection conn = new SqlConnection(_connectionString);
            try
            {
                conn.Open();

                // Lógica de auditoría para los Triggers (sigue igual)
                if (ClsSesion.UsuarioId != 0)
                {
                    using (SqlCommand cmd = new SqlCommand("sp_set_context_info", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        byte[] contextData = new byte[4];
                        BitConverter.GetBytes(ClsSesion.UsuarioId).CopyTo(contextData, 0);

                        cmd.Parameters.AddWithValue("@info", contextData);
                        cmd.ExecuteNonQuery();
                    }
                }
                return conn;
            }
            catch (Exception ex)
            {
                conn.Close();
                // En WPF, es mejor lanzar la excepción para que la UI la maneje
                throw new Exception("Error al conectar con la base de datos: " + ex.Message, ex);
            }
        }
    }
}
