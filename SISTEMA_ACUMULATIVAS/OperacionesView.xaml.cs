using SISTEMA_ACUMULATIVAS.Conexion;
using SISTEMA_ACUMULATIVAS.Models;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace SISTEMA_ACUMULATIVAS.Views
{
    public partial class OperacionesView : UserControl
    {
        private ClsConexion _conexion;
        private List<Operacion> _operacionesCache;
        private int _idOperacionSeleccionada = 0;

        // Bandera para evitar que la alerta salte cuando se selecciona un registro del Grid
        private bool _seleccionAutomatica = false;

        // Lista maestra de clientes para el buscador predictivo
        private List<ClienteItem> _todosLosClientes;
        private int? _clienteSeleccionadoId = null;

        public class ClienteItem
        {
            public int Id { get; set; }
            public string Nombre { get; set; }
            public string RFC { get; set; }
        }

        public OperacionesView()
        {
            InitializeComponent();
            _conexion = new ClsConexion();
            _todosLosClientes = new List<ClienteItem>();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            CargarClientesParaBusqueda();
            CargarOperacionesGrid();
            LimpiarFormulario();
        }

        // --- 1. CARGA DE DATOS ---
        private void CargarClientesParaBusqueda()
        {
            try
            {
                _todosLosClientes.Clear();
                using (SqlConnection conn = _conexion.GetConnection())
                {
                    string query = "SELECT Id, Nombre, RFC FROM Clientes WHERE Activo = 1 ORDER BY Nombre";
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                _todosLosClientes.Add(new ClienteItem
                                {
                                    Id = Convert.ToInt32(reader["Id"]),
                                    Nombre = reader["Nombre"].ToString(),
                                    RFC = reader["RFC"] != DBNull.Value ? reader["RFC"].ToString() : ""
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al cargar clientes: " + ex.Message, "Error BD", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // --- 2. BÚSQUEDA PREDICTIVA EN TIEMPO REAL ---
        private void txtBuscarCliente_TextChanged(object sender, TextChangedEventArgs e)
        {
            string busqueda = txtBuscarCliente.Text.Trim().ToLower();

            if (string.IsNullOrWhiteSpace(busqueda))
            {
                popupClientes.IsOpen = false;
                _clienteSeleccionadoId = null;
                lblClienteSeleccionado.Text = "Ningún cliente seleccionado";
                lblClienteSeleccionado.Foreground = Brushes.Gray;
                return;
            }

            var filtrados = _todosLosClientes
                .Where(c => c.Nombre.ToLower().Contains(busqueda) || (!string.IsNullOrEmpty(c.RFC) && c.RFC.ToLower().Contains(busqueda)))
                .Take(10)
                .ToList();

            if (filtrados.Count > 0 && txtBuscarCliente.IsFocused)
            {
                lstClientesSugeridos.ItemsSource = filtrados;
                popupClientes.IsOpen = true;
            }
            else
            {
                popupClientes.IsOpen = false;
            }
        }

        private void lstClientesSugeridos_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (lstClientesSugeridos.SelectedItem is ClienteItem seleccionado)
            {
                _clienteSeleccionadoId = seleccionado.Id;

                txtBuscarCliente.TextChanged -= txtBuscarCliente_TextChanged;
                txtBuscarCliente.Text = seleccionado.Nombre;
                txtBuscarCliente.TextChanged += txtBuscarCliente_TextChanged;

                lblClienteSeleccionado.Text = $"✔ Seleccionado: {seleccionado.Nombre} (RFC: {seleccionado.RFC})";
                lblClienteSeleccionado.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#28A745"));

                popupClientes.IsOpen = false;
                lstClientesSugeridos.SelectedItem = null;
            }
        }

        private void txtBuscarCliente_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Down && popupClientes.IsOpen)
            {
                lstClientesSugeridos.Focus();
                if (lstClientesSugeridos.Items.Count > 0)
                {
                    lstClientesSugeridos.SelectedIndex = 0;
                }
            }
        }

        // --- NUEVO: ALERTA DE ACTIVIDAD VULNERABLE AL SELECCIONAR ---
        private void cmbTipoOperacion_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Si la selección viene de cargar datos del Grid o de limpiar el formulario, no mostramos la alerta
            if (_seleccionAutomatica) return;

            if (cmbTipoOperacion.SelectedItem is ComboBoxItem cbItem)
            {
                string tipoOperacion = cbItem.Content is TextBlock tb ? tb.Text : cbItem.Content?.ToString();

                if (!string.IsNullOrEmpty(tipoOperacion))
                {
                    MessageBox.Show($"¡ATENCIÓN!\n\nHa seleccionado:\n{tipoOperacion}\n\nEsta operación es catalogada como ACTIVIDAD VULNERABLE. Por favor, asegúrese de recabar la documentación de identificación requerida, aun si no se alcanza el umbral de UMAS para el aviso correspondiente.",
                        "Alerta Ley Antilavado",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                }
            }
        }

        // --- 3. CARGA DE TABLA DE OPERACIONES ---
        private void CargarOperacionesGrid()
        {
            _operacionesCache = new List<Operacion>();
            dgOperaciones.ItemsSource = null;

            try
            {
                using (SqlConnection conn = _conexion.GetConnection())
                {
                    string query = @"
                        SELECT 
                            o.Id, o.ClienteId, c.Nombre AS ClienteNombre, 
                            o.TipoOperacion, o.Monto, o.FechaOperacion, 
                            o.FolioEscritura, o.Descripcion 
                        FROM Operaciones o
                        INNER JOIN Clientes c ON o.ClienteId = c.Id
                        ORDER BY o.FechaOperacion DESC";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                _operacionesCache.Add(new Operacion
                                {
                                    Id = (int)reader["Id"],
                                    ClienteId = (int)reader["ClienteId"],
                                    ClienteNombre = reader["ClienteNombre"].ToString(),
                                    TipoOperacion = reader["TipoOperacion"].ToString(),
                                    Monto = (decimal)reader["Monto"],
                                    FechaOperacion = (DateTime)reader["FechaOperacion"],
                                    FolioEscritura = reader["FolioEscritura"].ToString(),
                                    Descripcion = reader["Descripcion"] != DBNull.Value ? reader["Descripcion"].ToString() : ""
                                });
                            }
                        }
                    }
                }
                dgOperaciones.ItemsSource = _operacionesCache;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al cargar operaciones: " + ex.Message, "Error BD", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // --- 4. GUARDAR / ACTUALIZAR OPERACIÓN ---
        private void btnGuardar_Click(object sender, RoutedEventArgs e)
        {
            if (!_clienteSeleccionadoId.HasValue || _clienteSeleccionadoId.Value <= 0)
            {
                MessageBox.Show("Debe buscar y seleccionar un Cliente de la lista sugerida.", "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtBuscarCliente.Focus();
                return;
            }
            if (cmbTipoOperacion.SelectedItem == null)
            {
                MessageBox.Show("Debe seleccionar un Tipo de Operación.", "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (string.IsNullOrWhiteSpace(txtMonto.Text) || !decimal.TryParse(txtMonto.Text, out decimal monto))
            {
                MessageBox.Show("Ingrese un Monto válido.", "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (string.IsNullOrWhiteSpace(txtFolioEscritura.Text))
            {
                MessageBox.Show("El Folio de Escritura es obligatorio.", "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (dpFechaOperacion.SelectedDate == null)
            {
                MessageBox.Show("Seleccione la Fecha de la Operación.", "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            int clienteId = _clienteSeleccionadoId.Value;

            string tipoOperacion = "";
            if (cmbTipoOperacion.SelectedItem is ComboBoxItem cbItem)
            {
                if (cbItem.Content is TextBlock tb)
                {
                    tipoOperacion = tb.Text;
                }
                else
                {
                    tipoOperacion = cbItem.Content?.ToString() ?? "";
                }
            }

            string folio = txtFolioEscritura.Text.Trim();
            string descripcion = txtDescripcion.Text.Trim();
            DateTime fecha = dpFechaOperacion.SelectedDate.Value;

            try
            {
                using (SqlConnection conn = _conexion.GetConnection())
                {
                    EstablecerContextoUsuario(conn);

                    string query;
                    if (_idOperacionSeleccionada == 0)
                    {
                        query = @"INSERT INTO Operaciones 
                                  (ClienteId, TipoOperacion, Monto, FechaOperacion, FolioEscritura, Descripcion, UsuarioId) 
                                  VALUES 
                                  (@ClienteId, @Tipo, @Monto, @Fecha, @Folio, @Desc, @UsuarioId)";
                    }
                    else
                    {
                        query = @"UPDATE Operaciones SET 
                                  ClienteId=@ClienteId, TipoOperacion=@Tipo, Monto=@Monto, 
                                  FechaOperacion=@Fecha, FolioEscritura=@Folio, Descripcion=@Desc 
                                  WHERE Id=@Id";
                    }

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@ClienteId", clienteId);
                        cmd.Parameters.AddWithValue("@Tipo", tipoOperacion);
                        cmd.Parameters.AddWithValue("@Monto", monto);
                        cmd.Parameters.AddWithValue("@Fecha", fecha);
                        cmd.Parameters.AddWithValue("@Folio", folio);
                        cmd.Parameters.AddWithValue("@Desc", descripcion);

                        if (_idOperacionSeleccionada > 0)
                            cmd.Parameters.AddWithValue("@Id", _idOperacionSeleccionada);
                        else
                            cmd.Parameters.AddWithValue("@UsuarioId", ClsSesion.UsuarioId);

                        cmd.ExecuteNonQuery();
                    }
                }

                MessageBox.Show("Operación guardada exitosamente.", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
                LimpiarFormulario();
                CargarOperacionesGrid();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al guardar: " + ex.Message, "Error BD", MessageBoxButton.OK, MessageBoxImage.Error);
            }
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

        private void dgOperaciones_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dgOperaciones.SelectedItem is Operacion item)
            {
                _idOperacionSeleccionada = item.Id;
                _clienteSeleccionadoId = item.ClienteId;

                txtBuscarCliente.TextChanged -= txtBuscarCliente_TextChanged;
                txtBuscarCliente.Text = item.ClienteNombre;
                txtBuscarCliente.TextChanged += txtBuscarCliente_TextChanged;

                var cli = _todosLosClientes.FirstOrDefault(c => c.Id == item.ClienteId);
                string rfcTexto = cli != null && !string.IsNullOrEmpty(cli.RFC) ? $" (RFC: {cli.RFC})" : "";
                lblClienteSeleccionado.Text = $"✔ Seleccionado: {item.ClienteNombre}{rfcTexto}";
                lblClienteSeleccionado.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#28A745"));

                txtMonto.Text = item.Monto.ToString("0.00");
                txtFolioEscritura.Text = item.FolioEscritura;
                txtDescripcion.Text = item.Descripcion;
                dpFechaOperacion.SelectedDate = item.FechaOperacion;

                _seleccionAutomatica = true; // Pausamos la alerta
                foreach (ComboBoxItem cbItem in cmbTipoOperacion.Items)
                {
                    string texto = cbItem.Content is TextBlock tb ? tb.Text : cbItem.Content?.ToString();
                    if (!string.IsNullOrEmpty(texto) && (texto.Contains(item.TipoOperacion) || item.TipoOperacion.Contains(texto)))
                    {
                        cmbTipoOperacion.SelectedItem = cbItem;
                        break;
                    }
                }
                _seleccionAutomatica = false; // Reactivamos la alerta
            }
        }

        private void btnNuevo_Click(object sender, RoutedEventArgs e)
        {
            LimpiarFormulario();
        }

        private void LimpiarFormulario()
        {
            _idOperacionSeleccionada = 0;
            _clienteSeleccionadoId = null;

            txtBuscarCliente.TextChanged -= txtBuscarCliente_TextChanged;
            txtBuscarCliente.Text = "";
            txtBuscarCliente.TextChanged += txtBuscarCliente_TextChanged;

            popupClientes.IsOpen = false;
            lblClienteSeleccionado.Text = "Ningún cliente seleccionado";
            lblClienteSeleccionado.Foreground = Brushes.Gray;

            _seleccionAutomatica = true; // Pausamos la alerta
            cmbTipoOperacion.SelectedIndex = -1;
            _seleccionAutomatica = false; // Reactivamos la alerta

            txtMonto.Clear();
            txtFolioEscritura.Clear();
            txtDescripcion.Clear();
            dpFechaOperacion.SelectedDate = DateTime.Now;
            dgOperaciones.SelectedItem = null;
        }

        private void txtMonto_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9.]+");
            e.Handled = regex.IsMatch(e.Text);
        }
    }
}