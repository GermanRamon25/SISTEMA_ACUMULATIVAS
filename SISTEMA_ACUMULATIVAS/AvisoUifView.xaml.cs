using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;


namespace SISTEMA_ACUMULATIVAS.Views
{
    public partial class AvisoUifView : UserControl
    {
        public AvisoUifView()
        {
            InitializeComponent();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            // Lógica para cargar ComboBox de Mes y Año
            CargarPeriodos();
            txtLog.Text = "Listo. Seleccione un periodo y presione 'Cargar'.";
        }

        private void CargarPeriodos()
        {
            // Llenar Año
            cmbAnio.Items.Add(DateTime.Now.Year);
            cmbAnio.Items.Add(DateTime.Now.Year - 1);
            cmbAnio.SelectedIndex = 0;

            // Llenar Mes
            for (int i = 1; i <= 12; i++)
            {
                cmbMes.Items.Add(new ComboBoxItem
                {
                    Content = new DateTime(2000, i, 1).ToString("MMMM"), // "Enero", "Febrero", etc.
                    Tag = i
                });
            }
            cmbMes.SelectedIndex = DateTime.Now.Month - 1; // Seleccionar mes actual
        }

        private void btnCargarOperaciones_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Lógica para buscar en la BD operaciones que
            // superen el umbral de Aviso en el mes/año seleccionado.

            txtLog.Text = $"Buscando operaciones para {cmbMes.Text} {cmbAnio.Text}...\n";
            txtLog.Text += "Carga de ejemplo (Frontend).\n";
            // (Simulación)
            // dgOperacionesAviso.ItemsSource = ...
            txtLog.Text += "¡Operaciones cargadas! Listo para generar XML.\n";
            btnGenerarXml.IsEnabled = true;
        }

        private void btnGenerarXml_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Lógica para tomar los datos del DataGrid
            // y convertirlos al formato XML oficial del SAT/UIF.

            txtLog.Text += "Generando archivo XML...\n";
            // (Simulación de guardado)

            txtLog.Text += "¡ÉXITO! Archivo 'Aviso_UIF_2025_11.xml' guardado en Documentos.\n";
            MessageBox.Show("Archivo XML generado y guardado (simulación).", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
            btnGenerarXml.IsEnabled = false;
        }
    }
}
