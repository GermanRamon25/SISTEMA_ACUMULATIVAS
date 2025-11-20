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

namespace SISTEMA_ACUMULATIVAS.Views
{
    public partial class OperacionesView : UserControl
    {
        private ClsConexion _conexion;
        private List<Operacion> _operacionesCache;
        private int _idOperacionSeleccionada = 0;

        // Lista maestra para el buscador
        private List<Cliente> _todosLosClientes;

        public OperacionesView()
        {
            InitializeComponent();
            _conexion = new ClsConexion();
            _todosLosClientes = new List<Cliente>();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            CargarClientesEnComboBox();
            CargarOperacionesGrid();
            LimpiarFormulario();
        }

        // --- 1. CARGA DE DATOS ---
        private void CargarClientesEnComboBox()
        {
            try
            {
                _todosLosClientes.Clear();
                using (SqlConnection conn = _conexion.GetConnection())
                {
                    string query = "SELECT Id, Nombre FROM Clientes WHERE Activo = 1 ORDER BY Nombre";
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                _todosLosClientes.Add(new Cliente
                                {
                                    Id = (int)reader["Id"],
                                    Nombre = reader["Nombre"].ToString()
                                });
                            }
                        }
                    }
                }
                cmbCliente.ItemsSource = _todosLosClientes;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al cargar clientes: " + ex.Message, "Error BD", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // --- ¡ESTA ES LA FUNCIÓN QUE FALTABA Y CAUSABA EL ERROR! ---
        private void cmbCliente_KeyUp(object sender, KeyEventArgs e)
        {
            // Ignorar teclas de navegación
            if (e.Key == Key.Up || e.Key == Key.Down || e.Key == Key.Enter || e.Key == Key.Tab)
                return;

            string textoBusqueda = cmbCliente.Text.ToUpper();

            if (string.IsNullOrEmpty(textoBusqueda))
            {
                cmbCliente.ItemsSource = _todosLosClientes;
                return;
            }

            // Filtro tipo Google
            List<Cliente> filtrados = _todosLosClientes
                .Where(c => c.Nombre.ToUpper().Contains(textoBusqueda))
                .ToList();

            cmbCliente.ItemsSource = filtrados;
            cmbCliente.IsDropDownOpen = true;
        }

        // --- RESTO DEL CÓDIGO ---
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

        private void btnGuardar_Click(object sender, RoutedEventArgs e)
        {
            if (cmbCliente.SelectedValue == null)
            {
                MessageBox.Show("Debe seleccionar un Cliente de la lista.", "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
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

            int clienteId = (int)cmbCliente.SelectedValue;

            string tipoOperacion = (cmbTipoOperacion.SelectedItem as ComboBoxItem).Content.ToString();
            if (tipoOperacion.Contains("\n")) tipoOperacion = tipoOperacion.Split('\n')[0].Trim();
            if (tipoOperacion.Contains("\r")) tipoOperacion = tipoOperacion.Split('\r')[0].Trim();

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

                // Cargar datos para edición
                cmbCliente.SelectedValue = item.ClienteId;
                // Forzamos que el texto se vea, ya que al ser editable a veces se limpia
                cmbCliente.Text = item.ClienteNombre;

                txtMonto.Text = item.Monto.ToString("0.00");
                txtFolioEscritura.Text = item.FolioEscritura;
                txtDescripcion.Text = item.Descripcion;
                dpFechaOperacion.SelectedDate = item.FechaOperacion;

                foreach (ComboBoxItem cbItem in cmbTipoOperacion.Items)
                {
                    if (cbItem.Content.ToString().Contains(item.TipoOperacion))
                    {
                        cmbTipoOperacion.SelectedItem = cbItem;
                        break;
                    }
                }
            }
        }

        private void btnNuevo_Click(object sender, RoutedEventArgs e)
        {
            LimpiarFormulario();
        }

        private void LimpiarFormulario()
        {
            _idOperacionSeleccionada = 0;
            cmbCliente.SelectedIndex = -1;
            cmbCliente.Text = "";
            if (_todosLosClientes != null) cmbCliente.ItemsSource = _todosLosClientes;

            cmbTipoOperacion.SelectedIndex = -1;
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