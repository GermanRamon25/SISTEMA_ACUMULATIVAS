using SISTEMA_ACUMULATIVAS.Conexion;
using SISTEMA_ACUMULATIVAS.Models;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq; // <-- ¡ESTA ES LA LÍNEA QUE FALTABA!
using System.Windows;
using System.Windows.Controls;

namespace SISTEMA_ACUMULATIVAS.Views
{
    public partial class ClienteView : UserControl
    {
        private ClsConexion _conexion;
        private List<Cliente> _clientesCache; // Caché para la búsqueda

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

        private void CargarClientes()
        {
            _clientesCache = new List<Cliente>();
            dgClientes.ItemsSource = null; // Limpiar DataGrid

            try
            {
                using (SqlConnection conn = _conexion.GetConnection())
                {
                    // Nota: Omitimos 'ORDER BY' para ordenar en memoria después
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

                // Cargar la lista al DataGrid (ordenada por Nombre)
                dgClientes.ItemsSource = _clientesCache.OrderBy(c => c.Nombre).ToList(); // Ahora sí funcionará
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al cargar los clientes: " + ex.Message, "Error de BD", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LimpiarFormulario()
        {
            txtId.Text = "(Nuevo)";
            txtNombre.Text = string.Empty;
            txtRFC.Text = string.Empty;
            txtCURP.Text = string.Empty;
            cmbTipoPersona.SelectedIndex = -1; // Deseleccionar
            dgClientes.SelectedItem = null; // Deseleccionar DataGrid
            txtNombre.Focus();
        }

        // --- Eventos (Por ahora solo limpiamos) ---

        private void btnNuevo_Click(object sender, RoutedEventArgs e)
        {
            LimpiarFormulario();
        }

        private void btnGuardar_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Agregar lógica de Guardar (Insertar/Actualizar)
            MessageBox.Show("¡Lógica de Guardar pendiente!");
        }

        private void btnEliminar_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Agregar lógica de Desactivar (Update Activo=0)
            MessageBox.Show("¡Lógica de Eliminar/Desactivar pendiente!");
        }

        private void dgClientes_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Cargar datos del DataGrid al Formulario
            if (dgClientes.SelectedItem is Cliente clienteSeleccionado)
            {
                txtId.Text = clienteSeleccionado.Id.ToString();
                txtNombre.Text = clienteSeleccionado.Nombre;
                txtRFC.Text = clienteSeleccionado.RFC;
                txtCURP.Text = clienteSeleccionado.CURP;
                cmbTipoPersona.SelectedIndex = (clienteSeleccionado.TipoPersona == "F") ? 0 : 1;
            }
        }

        private void txtBusqueda_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Filtrar el DataGrid usando el caché
            string filtro = txtBusqueda.Text.ToLower();
            if (string.IsNullOrEmpty(filtro))
            {
                dgClientes.ItemsSource = _clientesCache.OrderBy(c => c.Nombre).ToList(); // Ahora sí funcionará
            }
            else
            {
                var filtrado = _clientesCache.Where(c => c.Nombre.ToLower().Contains(filtro) || // Ahora sí funcionará
                                                        c.RFC.ToLower().Contains(filtro))
                                             .OrderBy(c => c.Nombre) // Ahora sí funcionará
                                             .ToList(); // Ahora sí funcionará
                dgClientes.ItemsSource = filtrado;
            }
        }
    }
}