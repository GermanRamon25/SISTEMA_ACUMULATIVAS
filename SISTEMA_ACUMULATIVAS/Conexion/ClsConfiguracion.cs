using System;
using System.Data.SqlClient;
using SISTEMA_ACUMULATIVAS.Models;

namespace SISTEMA_ACUMULATIVAS.Conexion
{
    public class ClsConfiguracion
    {
        // Propiedad global para almacenar los datos en memoria durante la sesión
        public static NotariaModel NotariaActual { get; set; }

        #region MÉTODOS UMA

        public decimal ObtenerUMA()
        {
            decimal valorUMA = 0;
            ClsConexion conexion = new ClsConexion();
            using (SqlConnection con = conexion.GetConnection())
            {
                con.Open();
                string query = "SELECT TOP 1 ValorUMA FROM Configuracion ORDER BY Id DESC";
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    object result = cmd.ExecuteScalar();
                    if (result != null && result != DBNull.Value)
                    {
                        valorUMA = Convert.ToDecimal(result);
                    }
                }
            }
            return valorUMA;
        }

        public bool ActualizarUMA(decimal nuevoValor)
        {
            ClsConexion conexion = new ClsConexion();
            using (SqlConnection con = conexion.GetConnection())
            {
                con.Open();
                string query = @"IF EXISTS (SELECT 1 FROM Configuracion)
                                    UPDATE Configuracion SET ValorUMA = @ValorUMA, FechaActualizacion = GETDATE()
                                 ELSE
                                    INSERT INTO Configuracion (ValorUMA, FechaActualizacion) VALUES (@ValorUMA, GETDATE())";

                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@ValorUMA", nuevoValor);
                    int filas = cmd.ExecuteNonQuery();
                    return filas > 0;
                }
            }
        }

        #endregion

        #region MÉTODOS NOTARÍA

        public bool ExisteConfiguracionNotaria()
        {
            ClsConexion conexion = new ClsConexion();
            using (SqlConnection con = conexion.GetConnection())
            {
                con.Open();
                string query = "SELECT COUNT(*) FROM DatosNotaria";
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    int count = Convert.ToInt32(cmd.ExecuteScalar());
                    return count > 0;
                }
            }
        }

        public NotariaModel CargarDatosNotaria()
        {
            ClsConexion conexion = new ClsConexion();
            using (SqlConnection con = conexion.GetConnection())
            {
                con.Open();
                string query = "SELECT TOP 1 Id, NombreTitular, NumeroNotaria, DireccionCompleta, Telefono, EmailContacto FROM DatosNotaria ORDER BY Id DESC";
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            NotariaActual = new NotariaModel
                            {
                                Id = Convert.ToInt32(reader["Id"]),
                                NombreTitular = reader["NombreTitular"].ToString(),
                                NumeroNotaria = reader["NumeroNotaria"].ToString(),
                                DireccionCompleta = reader["DireccionCompleta"].ToString(),
                                Telefono = reader["Telefono"].ToString(),
                                EmailContacto = reader["EmailContacto"].ToString()
                            };
                            return NotariaActual;
                        }
                    }
                }
            }
            return null;
        }

        public bool GuardarOActualizarNotaria(NotariaModel notaria)
        {
            ClsConexion conexion = new ClsConexion();
            using (SqlConnection con = conexion.GetConnection())
            {
                con.Open();
                string query;
                if (ExisteConfiguracionNotaria())
                {
                    query = @"UPDATE DatosNotaria 
                              SET NombreTitular = @Nombre, NumeroNotaria = @Numero, 
                                  DireccionCompleta = @Direccion, Telefono = @Tel, 
                                  EmailContacto = @Email, FechaActualizacion = GETDATE()";
                }
                else
                {
                    query = @"INSERT INTO DatosNotaria (NombreTitular, NumeroNotaria, DireccionCompleta, Telefono, EmailContacto) 
                              VALUES (@Nombre, @Numero, @Direccion, @Tel, @Email)";
                }

                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@Nombre", notaria.NombreTitular ?? "");
                    cmd.Parameters.AddWithValue("@Numero", notaria.NumeroNotaria ?? "");
                    cmd.Parameters.AddWithValue("@Direccion", notaria.DireccionCompleta ?? "");
                    cmd.Parameters.AddWithValue("@Tel", notaria.Telefono ?? "");
                    cmd.Parameters.AddWithValue("@Email", notaria.EmailContacto ?? "");

                    int rows = cmd.ExecuteNonQuery();
                    if (rows > 0)
                    {
                        NotariaActual = notaria;
                        return true;
                    }
                }
            }
            return false;
        }

        #endregion
    }
}