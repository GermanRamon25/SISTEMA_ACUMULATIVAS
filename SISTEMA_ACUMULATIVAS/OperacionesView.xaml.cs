
using SISTEMA_ACUMULATIVAS.Conexion;
using SISTEMA_ACUMULATIVAS.Models; 
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Text.RegularExpressions; 
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace SISTEMA_ACUMULATIVAS.Views
{
    public partial class OperacionesView : UserControl
    {
        private ClsConexion _conexion;
        // private List<OperacionViewModel> _operacionesCache; 

        public OperacionesView()
        {
            InitializeComponent();
            _conexion = new ClsConexion();
        }

        // --- MÉTODOS REQUERIDOS POR EL XAML ---

        // 1. Método para el Error CS1061 (Línea 9)
        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            // Dejamos esto listo para cargar datos
            // CargarClientesEnComboBox();
            // CargarOperacionesGrid();
            LimpiarFormulario();
        }

        // 2. Método para el Error CS1061 (Línea 60)
        private void txtMonto_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            // Permite solo números y un punto decimal
            Regex regex = new Regex("[^0-9.]+"); // Expresión regular
            e.Handled = regex.IsMatch(e.Text);
        }

        // 3. Método para el Error CS1061 (Línea 24)
        private void dgOperaciones_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // TODO: Lógica para cargar datos del Grid al Formulario
        }

        // 4. Método para el Error CS1061 (Línea 80)
        private void btnNuevo_Click(object sender, RoutedEventArgs e)
        {
            LimpiarFormulario();
        }

        // 5. Método para el Error CS1061 (Línea 84)
        private void btnGuardar_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("¡Lógica de Guardar Operación pendiente!");
        }


        // --- MÉTODOS INTERNOS (Esqueleto) ---

        private void CargarClientesEnComboBox()
        {
            // TODO: Crear lógica para leer Clientes
        }

        private void CargarOperacionesGrid()
        {
            // TODO: Crear lógica para leer Operaciones
        }

        private void LimpiarFormulario()
        {
            cmbCliente.SelectedIndex = -1;
            cmbTipoOperacion.SelectedIndex = -1;
            txtMonto.Text = string.Empty;
            dpFechaOperacion.SelectedDate = DateTime.Now;
            txtFolioEscritura.Text = string.Empty;
            txtDescripcion.Text = string.Empty;
            dgOperaciones.SelectedItem = null;
        }
    }
}