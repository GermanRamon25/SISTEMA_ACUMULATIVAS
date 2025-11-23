using SISTEMA_ACUMULATIVAS.Conexion;
using SISTEMA_ACUMULATIVAS.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;

namespace SISTEMA_ACUMULATIVAS.Views
{
    public partial class PanelControlView : UserControl
    {
        private ClsConexion _conexion;
        private ClsConfiguracion _config;

        public PanelControlView()
        {
            InitializeComponent();
            _conexion = new ClsConexion();
            _config = new ClsConfiguracion();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            CargarUMA();
            CargarPapelera();
            CargarLogs();
            CargarUsuarios(); // <--- Added so the list loads on startup
        }

        // --- 1. UMA CONFIGURATION ---
        private void CargarUMA()
        {
            try
            {
                txtUMA.Text = _config.ObtenerUMA().ToString("0.00");
            }
            catch { txtUMA.Text = "0.00"; }
        }

        private void btnGuardarUMA_Click(object sender, RoutedEventArgs e)
        {
            if (decimal.TryParse(txtUMA.Text, out decimal nuevaUma))
            {
                try
                {
                    _config.ActualizarUMA(nuevaUma);
                    MessageBox.Show("Valor de UMA actualizado correctamente.", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex) { MessageBox.Show("Error: " + ex.Message); }
            }
            else MessageBox.Show("Ingrese un número válido.");
        }

        // --- 2. RECYCLE BIN ---
        private void CargarPapelera()
        {
            List<Cliente> eliminados = new List<Cliente>();
            try
            {
                using (SqlConnection conn = _conexion.GetConnection())
                {
                    // Search for INACTIVE clients (Activo = 0)
                    string query = "SELECT Id, Nombre, RFC FROM Clientes WHERE Activo = 0";
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                eliminados.Add(new Cliente
                                {
                                    Id = (int)reader["Id"],
                                    Nombre = reader["Nombre"].ToString(),
                                    RFC = reader["RFC"].ToString()
                                });
                            }
                        }
                    }
                }
                dgPapelera.ItemsSource = eliminados;
            }
            catch { }
        }

        private void btnRefrescarPapelera_Click(object sender, RoutedEventArgs e) { CargarPapelera(); }

        private void btnRestaurar_Click(object sender, RoutedEventArgs e)
        {
            if (dgPapelera.SelectedItem is Cliente cliente)
            {
                if (MessageBox.Show($"¿Restaurar al cliente '{cliente.Nombre}'?", "Confirmar", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    try
                    {
                        using (SqlConnection conn = _conexion.GetConnection())
                        {
                            string query = "UPDATE Clientes SET Activo = 1 WHERE Id = @Id";
                            using (SqlCommand cmd = new SqlCommand(query, conn))
                            {
                                cmd.Parameters.AddWithValue("@Id", cliente.Id);
                                cmd.ExecuteNonQuery();
                            }
                        }
                        CargarPapelera();
                        MessageBox.Show("Cliente restaurado. Ahora aparece en la lista normal.");
                    }
                    catch (Exception ex) { MessageBox.Show("Error: " + ex.Message); }
                }
            }
            else MessageBox.Show("Seleccione un cliente de la lista.");
        }

        // --- 3. SECURITY LOGS (AUDIT) ---
        public class LogItem
        {
            public DateTime Fecha { get; set; }
            public string Usuario { get; set; }
            public string Accion { get; set; }
            public string Detalle { get; set; }
        }

        private void CargarLogs()
        {
            List<LogItem> logs = new List<LogItem>();
            try
            {
                using (SqlConnection conn = _conexion.GetConnection())
                {
                    // Join with the Users table to see the real name
                    string query = @"SELECT TOP 100 L.Fecha, U.Usuario, L.Accion, L.Detalle 
                                     FROM LogsSistema L
                                     LEFT JOIN Usuarios U ON L.UsuarioId = U.Id
                                     ORDER BY L.Fecha DESC";
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            logs.Add(new LogItem
                            {
                                Fecha = (DateTime)reader["Fecha"],
                                Usuario = reader["Usuario"] != DBNull.Value ? reader["Usuario"].ToString() : "Sistema",
                                Accion = reader["Accion"].ToString(),
                                Detalle = reader["Detalle"].ToString()
                            });
                        }
                    }
                }
                dgLogs.ItemsSource = logs;
            }
            catch { }
        }

        private void btnActualizarLogs_Click(object sender, RoutedEventArgs e) { CargarLogs(); }

        // --- 4. USER MANAGEMENT (NEW) ---

        // Helper class for the DataGrid
        public class UsuarioItem
        {
            public int Id { get; set; }
            public string Usuario { get; set; }
            public string NombreCompleto { get; set; }
            public string Rol { get; set; }
            public bool Activo { get; set; }
        }

        private void CargarUsuarios()
        {
            List<UsuarioItem> lista = new List<UsuarioItem>();
            try
            {
                using (SqlConnection conn = _conexion.GetConnection())
                {
                    string query = "SELECT Id, Usuario, NombreCompleto, Rol, Activo FROM Usuarios";
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            lista.Add(new UsuarioItem
                            {
                                Id = (int)reader["Id"],
                                Usuario = reader["Usuario"].ToString(),
                                NombreCompleto = reader["NombreCompleto"].ToString(),
                                Rol = reader["Rol"].ToString(),
                                Activo = (bool)reader["Activo"]
                            });
                        }
                    }
                }
                dgUsuarios.ItemsSource = lista;
            }
            catch { }
        }

        private void btnRefrescarUsuarios_Click(object sender, RoutedEventArgs e) { CargarUsuarios(); }

        // Generic method to change Role or Status
        private void ActualizarUsuario(string columna, object valor, string mensajeExito)
        {
            if (dgUsuarios.SelectedItem is UsuarioItem user)
            {
                // Prevent removing Admin role from yourself by mistake
                if (user.Id == ClsSesion.UsuarioId && columna == "Rol" && valor.ToString() != "Admin")
                {
                    MessageBox.Show("No puedes quitarte el rol de Admin a ti mismo.", "Seguridad", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                try
                {
                    using (SqlConnection conn = _conexion.GetConnection())
                    {
                        string query = $"UPDATE Usuarios SET {columna} = @Valor WHERE Id = @Id";
                        using (SqlCommand cmd = new SqlCommand(query, conn))
                        {
                            cmd.Parameters.AddWithValue("@Valor", valor);
                            cmd.Parameters.AddWithValue("@Id", user.Id);
                            cmd.ExecuteNonQuery();
                        }
                    }
                    CargarUsuarios(); // Reload list
                    MessageBox.Show(mensajeExito, "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex) { MessageBox.Show("Error: " + ex.Message); }
            }
            else MessageBox.Show("Seleccione un usuario.");
        }

        private void btnHacerAdmin_Click(object sender, RoutedEventArgs e)
        {
            ActualizarUsuario("Rol", "Admin", "Usuario promovido a ADMINISTRADOR.");
        }

        private void btnHacerOperador_Click(object sender, RoutedEventArgs e)
        {
            ActualizarUsuario("Rol", "Operador", "Usuario degradado a OPERADOR.");
        }

        private void btnBloquear_Click(object sender, RoutedEventArgs e)
        {
            if (dgUsuarios.SelectedItem is UsuarioItem user)
            {
                bool nuevoEstado = !user.Activo; // Invert state
                string texto = nuevoEstado ? "DESBLOQUEADO" : "BLOQUEADO";
                ActualizarUsuario("Activo", nuevoEstado, $"El usuario ha sido {texto}.");
            }
        }
    }
}