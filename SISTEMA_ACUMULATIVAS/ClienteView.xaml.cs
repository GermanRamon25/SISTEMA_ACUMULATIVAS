using SISTEMA_ACUMULATIVAS.Conexion;
using SISTEMA_ACUMULATIVAS.Models;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace SISTEMA_ACUMULATIVAS.Views
{
    public partial class ClienteView : UserControl
    {
        private ClsConexion _conexion;
        private List<Cliente> _clientesCache;

        public ClienteView()
        {
            InitializeComponent();
            _conexion = new ClsConexion();
            LimpiarFormulario();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            CargarClientes();
        }

        // --- 1. LECTURA (READ) ---
        private void CargarClientes()
        {
            _clientesCache = new List<Cliente>();
            dgClientes.ItemsSource = null;

            try
            {
                using (SqlConnection conn = _conexion.GetConnection())
                {
                    if (conn.State != System.Data.ConnectionState.Open) conn.Open();

                    string query = "SELECT Id, Nombre, RFC, CURP, TipoPersona, FechaRegistro, Activo FROM Clientes WHERE Activo = 1";
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                _clientesCache.Add(new Cliente
                                {
                                    Id = (int)reader["Id"],
                                    Nombre = reader["Nombre"].ToString(),
                                    RFC = reader["RFC"].ToString(),
                                    CURP = reader["CURP"] != DBNull.Value ? reader["CURP"].ToString() : string.Empty,
                                    TipoPersona = reader["TipoPersona"].ToString(),
                                    FechaRegistro = (DateTime)reader["FechaRegistro"],
                                    Activo = (bool)reader["Activo"]
                                });
                            }
                        }
                    }
                }
                dgClientes.ItemsSource = _clientesCache.OrderBy(c => c.Nombre).ToList();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al cargar clientes: " + ex.Message, "Error BD", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // --- 2. GUARDADO (CREATE / UPDATE) ---
        private void btnGuardar_Click(object sender, RoutedEventArgs e)
        {
            // A) Validaciones de Campos Vacíos
            if (string.IsNullOrWhiteSpace(txtNombre.Text) || string.IsNullOrWhiteSpace(txtRFC.Text))
            {
                MessageBox.Show("El Nombre y el RFC son obligatorios.", "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (cmbTipoPersona.SelectedItem == null)
            {
                MessageBox.Show("Seleccione el Tipo de Persona.", "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string nombre = txtNombre.Text.Trim().ToUpper();
            string rfc = txtRFC.Text.Trim().ToUpper();
            string curp = txtCURP.Text.Trim().ToUpper();
            string tipoPersona = ((ComboBoxItem)cmbTipoPersona.SelectedItem).Tag.ToString();

            int idActual = 0;
            if (txtId.Text != "(Nuevo)")
            {
                idActual = int.Parse(txtId.Text);
            }

            // B) --- VALIDACIÓN PREVIA (C#) ---
            string mensajeDuplicado = ValidarDuplicado(nombre, rfc, idActual);
            if (!string.IsNullOrEmpty(mensajeDuplicado))
            {
                MessageBox.Show(mensajeDuplicado, "Cliente Duplicado", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // C) Guardado en BD con "Paracaídas"
            try
            {
                if (idActual == 0)
                    InsertarCliente(nombre, rfc, curp, tipoPersona);
                else
                    ActualizarCliente(idActual, nombre, rfc, curp, tipoPersona);

                CargarClientes();
                LimpiarFormulario();
                MessageBox.Show("Cliente guardado correctamente.", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (SqlException sqlEx)
            {
                // --- AQUÍ ATRAPAMOS EL ERROR DE LA IMAGEN ---
                // El error 2601 o 2627 es "Violation of UNIQUE KEY"
                if (sqlEx.Number == 2601 || sqlEx.Number == 2627)
                {
                    MessageBox.Show($"El RFC '{rfc}' ya existe en la base de datos (posiblemente en un registro oculto o desactivado).\n\nNo se puede duplicar.",
                                    "RFC Duplicado", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                else
                {
                    MessageBox.Show("Error de Base de Datos: " + sqlEx.Message, "Error SQL", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error general: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // --- MÉTODO CORREGIDO: VALIDAR TODO (INCLUIDO BORRADOS) ---
        private string ValidarDuplicado(string nombre, string rfc, int idExcluir)
        {
            try
            {
                using (SqlConnection conn = _conexion.GetConnection())
                {
                    if (conn.State != System.Data.ConnectionState.Open) conn.Open();

                    // CORRECCIÓN: Quitamos "AND Activo = 1"
                    // La base de datos prohíbe duplicados SIEMPRE, así que nosotros también debemos buscar en TODO.
                    string query = @"SELECT COUNT(*) FROM Clientes 
                                     WHERE (Nombre = @Nombre OR RFC = @RFC) 
                                     AND Id != @Id";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@Nombre", nombre);
                        cmd.Parameters.AddWithValue("@RFC", rfc);
                        cmd.Parameters.AddWithValue("@Id", idExcluir);

                        int count = (int)cmd.ExecuteScalar();

                        if (count > 0)
                        {
                            // Averiguar cuál fue el problema
                            // Nota: Aquí también quitamos Activo=1 para saber quién causa el conflicto
                            query = "SELECT Nombre, RFC, Activo FROM Clientes WHERE (Nombre = @Nombre OR RFC = @RFC) AND Id != @Id";
                            cmd.CommandText = query;

                            using (SqlDataReader reader = cmd.ExecuteReader())
                            {
                                if (reader.Read())
                                {
                                    string nombreEncontrado = reader["Nombre"].ToString();
                                    string rfcEncontrado = reader["RFC"].ToString();
                                    bool esActivo = (bool)reader["Activo"];
                                    string estadoStr = esActivo ? "" : " (Cliente DESACTIVADO)";

                                    if (rfcEncontrado.ToUpper() == rfc)
                                        return $"El RFC '{rfc}' ya está registrado{estadoStr}.\nPertenece a: {nombreEncontrado}";

                                    if (nombreEncontrado.ToUpper() == nombre)
                                        return $"El nombre '{nombre}' ya existe{estadoStr}.\nRFC registrado: {rfcEncontrado}";
                                }
                            }
                            // Si count > 0 pero no entramos al if (raro), mensaje genérico
                            return "Ya existe un cliente con ese Nombre o RFC.";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return "Error al validar duplicados: " + ex.Message;
            }

            return null; // Todo limpio
        }

        private void EstablecerContextoUsuario(SqlConnection conn)
        {
            int usuarioId = ClsSesion.UsuarioId;
            string query = "DECLARE @Bin varbinary(4) = CONVERT(varbinary(4), @UserId); SET CONTEXT_INFO @Bin;";
            using (SqlCommand cmd = new SqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@UserId", usuarioId);
                cmd.ExecuteNonQuery();
            }
        }

        private void InsertarCliente(string nombre, string rfc, string curp, string tipo)
        {
            using (SqlConnection conn = _conexion.GetConnection())
            {
                EstablecerContextoUsuario(conn);
                string query = "INSERT INTO Clientes (Nombre, RFC, CURP, TipoPersona, Activo, FechaRegistro) VALUES (@Nombre, @RFC, @CURP, @Tipo, 1, GETDATE())";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@Nombre", nombre);
                    cmd.Parameters.AddWithValue("@RFC", rfc);
                    cmd.Parameters.AddWithValue("@CURP", string.IsNullOrEmpty(curp) ? (object)DBNull.Value : curp);
                    cmd.Parameters.AddWithValue("@Tipo", tipo);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        private void ActualizarCliente(int id, string nombre, string rfc, string curp, string tipo)
        {
            using (SqlConnection conn = _conexion.GetConnection())
            {
                EstablecerContextoUsuario(conn);
                string query = "UPDATE Clientes SET Nombre=@Nombre, RFC=@RFC, CURP=@CURP, TipoPersona=@Tipo WHERE Id=@Id";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@Id", id);
                    cmd.Parameters.AddWithValue("@Nombre", nombre);
                    cmd.Parameters.AddWithValue("@RFC", rfc);
                    cmd.Parameters.AddWithValue("@CURP", string.IsNullOrEmpty(curp) ? (object)DBNull.Value : curp);
                    cmd.Parameters.AddWithValue("@Tipo", tipo);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        private void btnEliminar_Click(object sender, RoutedEventArgs e)
        {
            if (txtId.Text == "(Nuevo)") return;

            if (MessageBox.Show("¿Eliminar este cliente? (Se archivará)", "Confirmar", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                try
                {
                    int id = int.Parse(txtId.Text);
                    using (SqlConnection conn = _conexion.GetConnection())
                    {
                        EstablecerContextoUsuario(conn);
                        string query = "UPDATE Clientes SET Activo = 0 WHERE Id = @Id";
                        using (SqlCommand cmd = new SqlCommand(query, conn))
                        {
                            cmd.Parameters.AddWithValue("@Id", id);
                            cmd.ExecuteNonQuery();
                        }
                    }
                    CargarClientes();
                    LimpiarFormulario();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error al eliminar: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void LimpiarFormulario()
        {
            txtId.Text = "(Nuevo)";
            txtNombre.Text = string.Empty;
            txtRFC.Text = string.Empty;
            txtCURP.Text = string.Empty;
            cmbTipoPersona.SelectedIndex = -1;
            dgClientes.SelectedItem = null;
            txtNombre.Focus();
        }

        private void btnNuevo_Click(object sender, RoutedEventArgs e)
        {
            LimpiarFormulario();
        }

        private void dgClientes_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dgClientes.SelectedItem is Cliente cliente)
            {
                txtId.Text = cliente.Id.ToString();
                txtNombre.Text = cliente.Nombre;
                txtRFC.Text = cliente.RFC;
                txtCURP.Text = cliente.CURP;
                cmbTipoPersona.SelectedIndex = (cliente.TipoPersona == "F") ? 0 : 1;
            }
        }

        private void txtBusqueda_TextChanged(object sender, TextChangedEventArgs e)
        {
            string filtro = txtBusqueda.Text.ToLower();
            if (_clientesCache != null)
            {
                var filtrado = _clientesCache
                    .Where(c => c.Nombre.ToLower().Contains(filtro) || c.RFC.ToLower().Contains(filtro))
                    .OrderBy(c => c.Nombre)
                    .ToList();
                dgClientes.ItemsSource = filtrado;
            }
        }
    }
}