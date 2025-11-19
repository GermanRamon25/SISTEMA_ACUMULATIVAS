using SISTEMA_ACUMULATIVAS.Conexion;
using SISTEMA_ACUMULATIVAS.Models; // Importante: Aquí están tus clases independientes
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

        // CAMBIO: Ahora usamos la lista del Modelo oficial 'Operacion'
        private List<Operacion> _operacionesCache;
        private int _idOperacionSeleccionada = 0;

        public OperacionesView()
        {
            InitializeComponent();
            _conexion = new ClsConexion();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            CargarClientesEnComboBox();
            CargarOperacionesGrid();
            LimpiarFormulario();
        }

        // --- 1. LECTURA (READ) ---

        private void CargarClientesEnComboBox()
        {
            try
            {
                // CAMBIO: Usamos el modelo 'Cliente' existente en Models/Cliente.cs
                List<Cliente> listaClientes = new List<Cliente>();

                using (SqlConnection conn = _conexion.GetConnection())
                {
                    // Solo traemos ID y Nombre para ser eficientes, aunque la clase tenga más campos
                    string query = "SELECT Id, Nombre FROM Clientes WHERE Activo = 1 ORDER BY Nombre";
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                listaClientes.Add(new Cliente
                                {
                                    Id = (int)reader["Id"],
                                    Nombre = reader["Nombre"].ToString()
                                    // El resto de propiedades (RFC, CURP) se quedan vacías o nulas, no importa aquí.
                                });
                            }
                        }
                    }
                }
                // Asignamos la lista de objetos Cliente reales
                cmbCliente.ItemsSource = listaClientes;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al cargar clientes: " + ex.Message, "Error BD", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CargarOperacionesGrid()
        {
            // CAMBIO: Usamos el nuevo modelo 'Operacion'
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
                                // Llenamos el modelo independiente
                                _operacionesCache.Add(new Operacion
                                {
                                    Id = (int)reader["Id"],
                                    ClienteId = (int)reader["ClienteId"],
                                    ClienteNombre = reader["ClienteNombre"].ToString(), // Propiedad Extra del modelo
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

        // --- 2. GUARDADO (CREATE / UPDATE) ---
        // (Este bloque es idéntico en lógica, solo cambia que usamos los modelos externos si fuera necesario pasar objetos)

        private void btnGuardar_Click(object sender, RoutedEventArgs e)
        {
            // Validaciones
            if (cmbCliente.SelectedValue == null)
            {
                MessageBox.Show("Debe seleccionar un Cliente.", "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
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
                    EstablecerContextoUsuario(conn); // Auditoría

                    string query;
                    if (_idOperacionSeleccionada == 0) // INSERT
                    {
                        query = @"INSERT INTO Operaciones 
                                  (ClienteId, TipoOperacion, Monto, FechaOperacion, FolioEscritura, Descripcion, UsuarioId) 
                                  VALUES 
                                  (@ClienteId, @Tipo, @Monto, @Fecha, @Folio, @Desc, @UsuarioId)";
                    }
                    else // UPDATE
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
                CargarOperacionesGrid(); // Recargar tabla
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

        // --- 3. EVENTOS UI ---

        private void dgOperaciones_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // CAMBIO: Casteamos al modelo 'Operacion' oficial
            if (dgOperaciones.SelectedItem is Operacion item)
            {
                _idOperacionSeleccionada = item.Id;

                cmbCliente.SelectedValue = item.ClienteId;
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