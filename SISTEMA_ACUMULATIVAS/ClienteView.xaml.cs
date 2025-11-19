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
                    // Validamos si la conexión está abierta (GetConnection ya la abre, pero por seguridad)
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
                // Ordenar por nombre
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
            // Validaciones
            if (string.IsNullOrWhiteSpace(txtNombre.Text) || string.IsNullOrWhiteSpace(txtRFC.Text))
            {
                MessageBox.Show("Nombre y RFC son obligatorios.", "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (cmbTipoPersona.SelectedItem == null)
            {
                MessageBox.Show("Seleccione el Tipo de Persona.", "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string tipoPersona = ((ComboBoxItem)cmbTipoPersona.SelectedItem).Tag.ToString();

            try
            {
                if (txtId.Text == "(Nuevo)")
                {
                    InsertarCliente(txtNombre.Text.Trim(), txtRFC.Text.Trim(), txtCURP.Text.Trim(), tipoPersona);
                }
                else
                {
                    int id = int.Parse(txtId.Text);
                    ActualizarCliente(id, txtNombre.Text.Trim(), txtRFC.Text.Trim(), txtCURP.Text.Trim(), tipoPersona);
                }

                CargarClientes();
                LimpiarFormulario();
                MessageBox.Show("Cliente guardado correctamente.", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al guardar: " + ex.Message, "Error BD", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // MÉTODO CLAVE: Prepara la auditoría para el Trigger
        private void EstablecerContextoUsuario(SqlConnection conn)
        {
            // Si no hay usuario logueado (ej. pruebas), usamos 0
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
                // 1. Establecer quién es el usuario (para el Trigger)
                EstablecerContextoUsuario(conn);

                // 2. Ejecutar el Insert
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
                // 1. Establecer quién es el usuario (para el Trigger)
                EstablecerContextoUsuario(conn);

                // 2. Ejecutar el Update
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

        // --- 3. ELIMINAR (SOFT DELETE) ---
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
                        // 1. Establecer quién es el usuario (para el Trigger)
                        EstablecerContextoUsuario(conn);

                        // 2. Ejecutar el Soft Delete
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

        // --- 4. UTILIDADES DE UI ---
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