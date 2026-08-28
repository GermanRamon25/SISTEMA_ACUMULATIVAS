using System;
using System.Data.SqlClient;
using SISTEMA_ACUMULATIVAS.Models;

namespace SISTEMA_ACUMULATIVAS.Conexion
{
    public class ClsConfiguracion
    {
        // Propiedad global para almacenar los datos en memoria durante la sesión
        public static NotariaModel NotariaActual { get; set; }

        #region MÉTODOS UMA (Estructura Clave-Valor)

        public decimal ObtenerUMA()
        {
            decimal valorUMA = 113.14m; // Valor inicial por defecto
            try
            {
                ClsConexion conexion = new ClsConexion();
                using (SqlConnection con = conexion.GetConnection())
                {
                    string query = "SELECT Valor FROM Configuracion WHERE Clave = 'UMA'";
                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        object result = cmd.ExecuteScalar();
                        if (result != null && result != DBNull.Value)
                        {
                            valorUMA = Convert.ToDecimal(result);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al obtener UMA: {ex.Message}");
            }
            return valorUMA;
        }

        public bool ActualizarUMA(decimal nuevoValor)
        {
            try
            {
                ClsConexion conexion = new ClsConexion();
                using (SqlConnection con = conexion.GetConnection())
                {
                    string query = @"IF EXISTS (SELECT 1 FROM Configuracion WHERE Clave = 'UMA')
                                        UPDATE Configuracion SET Valor = @Valor WHERE Clave = 'UMA'
                                     ELSE
                                        INSERT INTO Configuracion (Clave, Valor) VALUES ('UMA', @Valor)";

                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@Valor", nuevoValor);
                        int filas = cmd.ExecuteNonQuery();
                        return filas > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al actualizar UMA: {ex.Message}");
                return false;
            }
        }

        #endregion

        #region MÉTODOS NOTARÍA (Tabla DatosNotaria)

        public bool ExisteConfiguracionNotaria(int usuarioId)
        {
            try
            {
                ClsConexion conexion = new ClsConexion();
                using (SqlConnection con = conexion.GetConnection())
                {
                    string query = "SELECT COUNT(*) FROM DatosNotaria WHERE UsuarioId = @UsuarioId";
                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@UsuarioId", usuarioId);
                        int count = Convert.ToInt32(cmd.ExecuteScalar());
                        return count > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al verificar notaría: {ex.Message}");
                return false;
            }
        }

        public NotariaModel CargarDatosNotaria(int usuarioId)
        {
            try
            {
                ClsConexion conexion = new ClsConexion();
                using (SqlConnection con = conexion.GetConnection())
                {
                    string query = @"SELECT TOP 1 Id, NombreTitular, NumeroNotaria, DireccionCompleta, Telefono, EmailContacto 
                                     FROM DatosNotaria 
                                     WHERE UsuarioId = @UsuarioId 
                                     ORDER BY Id DESC";

                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@UsuarioId", usuarioId);

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
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al cargar datos notaría: {ex.Message}");
            }
            return null;
        }

        public bool GuardarOActualizarNotaria(NotariaModel notaria, int usuarioId)
        {
            try
            {
                ClsConexion conexion = new ClsConexion();
                using (SqlConnection con = conexion.GetConnection())
                {
                    string query;
                    if (ExisteConfiguracionNotaria(usuarioId))
                    {
                        query = @"UPDATE DatosNotaria 
                                  SET NombreTitular = @Nombre, 
                                      NumeroNotaria = @Numero, 
                                      DireccionCompleta = @Direccion, 
                                      Telefono = @Tel, 
                                      EmailContacto = @Email, 
                                      FechaActualizacion = GETDATE()
                                  WHERE UsuarioId = @UsuarioId";
                    }
                    else
                    {
                        query = @"INSERT INTO DatosNotaria (UsuarioId, NombreTitular, NumeroNotaria, DireccionCompleta, Telefono, EmailContacto) 
                                  VALUES (@UsuarioId, @Nombre, @Numero, @Direccion, @Tel, @Email)";
                    }

                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@UsuarioId", usuarioId);
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
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al guardar notaría: {ex.Message}");
            }
            return false;
        }

        #endregion
    }
}