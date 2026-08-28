using SISTEMA_ACUMULATIVAS.Conexion;
using SISTEMA_ACUMULATIVAS.Models;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input; // Necesario para KeyEventArgs

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

        // --- VALIDACIÓN VISUAL: BLOQUEAR ESPACIOS (Para RFC y CURP) ---
        private void TxtSinEspacios_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Space)
            {
                e.Handled = true;
            }
        }

        // --- 1. LECTURA (READ) FILTRADA POR USUARIO ---
        private void CargarClientes()
        {
            _clientesCache = new List<Cliente>();
            dgClientes.ItemsSource = null;

            try
            {
                using (SqlConnection conn = _conexion.GetConnection())
                {
                    if (conn.State != System.Data.ConnectionState.Open) conn.Open();

                    // Consulta filtrando por el UsuarioId en sesión
                    string query = @"SELECT Id, Nombre, RFC, CURP, TipoPersona, FechaRegistro, Activo 
                                     FROM Clientes 
                                     WHERE Activo = 1 AND UsuarioId = @UsuarioId";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@UsuarioId", ClsSesion.UsuarioId);

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

            // B) Obtención de datos
            string nombre = txtNombre.Text.Trim();
            string rfc = txtRFC.Text.Trim().ToUpper();
            string curp = txtCURP.Text.Trim().ToUpper();
            string tipoPersona = ((ComboBoxItem)cmbTipoPersona.SelectedItem).Tag.ToString();

            // Validación de longitud de RFC
            if (tipoPersona == "F" && rfc.Length != 13)
            {
                MessageBox.Show("El RFC de una Persona FÍSICA debe tener 13 caracteres.", "Formato Incorrecto", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (tipoPersona == "M" && rfc.Length != 12)
            {
                MessageBox.Show("El RFC de una Persona MORAL debe tener 12 caracteres.", "Formato Incorrecto", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            int idActual = 0;
            if (txtId.Text != "(Nuevo)")
            {
                idActual = int.Parse(txtId.Text);
            }

            // Validación de duplicados dentro de la cartera del mismo usuario
            string mensajeDuplicado = ValidarDuplicado(nombre, rfc, idActual);
            if (!string.IsNullOrEmpty(mensajeDuplicado))
            {
                string mensajeFinal = mensajeDuplicado + "\n\n¿Desea registrarlo de todas formas?";

                if (MessageBox.Show(mensajeFinal, "Confirmar Duplicado", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.No)
                {
                    return;
                }
            }

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
                if (sqlEx.Number == 2601 || sqlEx.Number == 2627)
                {
                    MessageBox.Show($"Imposible guardar: El RFC '{rfc}' ya está registrado y no se permiten duplicados exactos.",
                                    "Error de Restricción", MessageBoxButton.OK, MessageBoxImage.Error);
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

        private string ValidarDuplicado(string nombre, string rfc, int idExcluir)
        {
            try
            {
                using (SqlConnection conn = _conexion.GetConnection())
                {
                    if (conn.State != System.Data.ConnectionState.Open) conn.Open();

                    // 1. Coincidencia EXACTA de RFC dentro de los clientes del usuario
                    string queryRFC = @"SELECT Nombre FROM Clientes 
                                       WHERE RFC = @RFC AND Id != @Id AND UsuarioId = @UsuarioId AND Activo = 1";
                    using (SqlCommand cmd = new SqlCommand(queryRFC, conn))
                    {
                        cmd.Parameters.AddWithValue("@RFC", rfc);
                        cmd.Parameters.AddWithValue("@Id", idExcluir);
                        cmd.Parameters.AddWithValue("@UsuarioId", ClsSesion.UsuarioId);

                        object result = cmd.ExecuteScalar();
                        if (result != null)
                        {
                            return $"El RFC ingresado ya pertenece al cliente: {result}.";
                        }
                    }

                    // 2. Coincidencia FONÉTICA de Nombre dentro de los clientes del usuario
                    string queryNombre = @"SELECT TOP 1 Nombre, RFC 
                                           FROM Clientes 
                                           WHERE SOUNDEX(Nombre) = SOUNDEX(@Nombre) 
                                             AND Id != @Id 
                                             AND UsuarioId = @UsuarioId 
                                             AND Activo = 1";

                    using (SqlCommand cmd = new SqlCommand(queryNombre, conn))
                    {
                        cmd.Parameters.AddWithValue("@Nombre", nombre);
                        cmd.Parameters.AddWithValue("@Id", idExcluir);
                        cmd.Parameters.AddWithValue("@UsuarioId", ClsSesion.UsuarioId);

                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                string nombreEncontrado = reader["Nombre"].ToString();
                                string rfcEncontrado = reader["RFC"].ToString();

                                return $"¡ATENCIÓN! Se encontró un cliente con nombre fonéticamente similar:\n\n" +
                                       $"Nombre: {nombreEncontrado}\n" +
                                       $"RFC: {rfcEncontrado}";
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return "Error al validar duplicados: " + ex.Message;
            }

            return null;
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
                if (conn.State != System.Data.ConnectionState.Open) conn.Open();
                EstablecerContextoUsuario(conn);

                string query = @"INSERT INTO Clientes (Nombre, RFC, CURP, TipoPersona, UsuarioId, Activo, FechaRegistro) 
                                 VALUES (@Nombre, @RFC, @CURP, @Tipo, @UsuarioId, 1, GETDATE())";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@Nombre", nombre);
                    cmd.Parameters.AddWithValue("@RFC", rfc);
                    cmd.Parameters.AddWithValue("@CURP", string.IsNullOrEmpty(curp) ? (object)DBNull.Value : curp);
                    cmd.Parameters.AddWithValue("@Tipo", tipo);
                    cmd.Parameters.AddWithValue("@UsuarioId", ClsSesion.UsuarioId);

                    cmd.ExecuteNonQuery();
                }
            }
        }

        private void ActualizarCliente(int id, string nombre, string rfc, string curp, string tipo)
        {
            using (SqlConnection conn = _conexion.GetConnection())
            {
                if (conn.State != System.Data.ConnectionState.Open) conn.Open();
                EstablecerContextoUsuario(conn);

                string query = @"UPDATE Clientes 
                                 SET Nombre = @Nombre, RFC = @RFC, CURP = @CURP, TipoPersona = @Tipo 
                                 WHERE Id = @Id AND UsuarioId = @UsuarioId";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@Id", id);
                    cmd.Parameters.AddWithValue("@Nombre", nombre);
                    cmd.Parameters.AddWithValue("@RFC", rfc);
                    cmd.Parameters.AddWithValue("@CURP", string.IsNullOrEmpty(curp) ? (object)DBNull.Value : curp);
                    cmd.Parameters.AddWithValue("@Tipo", tipo);
                    cmd.Parameters.AddWithValue("@UsuarioId", ClsSesion.UsuarioId);

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
                        if (conn.State != System.Data.ConnectionState.Open) conn.Open();
                        EstablecerContextoUsuario(conn);

                        string query = "UPDATE Clientes SET Activo = 0 WHERE Id = @Id AND UsuarioId = @UsuarioId";
                        using (SqlCommand cmd = new SqlCommand(query, conn))
                        {
                            cmd.Parameters.AddWithValue("@Id", id);
                            cmd.Parameters.AddWithValue("@UsuarioId", ClsSesion.UsuarioId);
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
                if (cliente.TipoPersona == "F") cmbTipoPersona.SelectedIndex = 0;
                else if (cliente.TipoPersona == "M") cmbTipoPersona.SelectedIndex = 1;
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

        private void cmbTipoPersona_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbTipoPersona.SelectedItem is ComboBoxItem itemSeleccionado && txtRFC != null)
            {
                string tag = itemSeleccionado.Tag?.ToString();
                if (tag == "M")
                {
                    txtRFC.MaxLength = 12;
                    if (txtRFC.Text.Length > 12)
                    {
                        txtRFC.Text = txtRFC.Text.Substring(0, 12);
                    }
                }
                else // Persona Física
                {
                    txtRFC.MaxLength = 13;
                }
            }
        }

        private void BtnImportarExcel_Click(object sender, RoutedEventArgs e)
        {
            ImportarClientesWindow ventanaImportar = new ImportarClientesWindow();
            if (ventanaImportar.ShowDialog() == true)
            {
                CargarClientes();
            }
        }

        private void BtnImportarClientes_Click(object sender, RoutedEventArgs e)
        {
            ImportarClientesWindow ventanaImportar = new ImportarClientesWindow();
            if (ventanaImportar.ShowDialog() == true)
            {
                CargarClientes();
            }
        }

        private void txtRFC_TextChanged(object sender, TextChangedEventArgs e)
        {
            string rfc = txtRFC.Text.Trim().ToUpper();

            if (rfc.Length == 12)
            {
                cmbTipoPersona.SelectedIndex = 1; // Persona Moral
            }
            else if (rfc.Length == 13)
            {
                cmbTipoPersona.SelectedIndex = 0; // Persona Física
            }
        }
    }
}