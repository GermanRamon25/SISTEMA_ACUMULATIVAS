using System.Windows;
using SISTEMA_ACUMULATIVAS.Conexion;
using SISTEMA_ACUMULATIVAS.Models;

namespace SISTEMA_ACUMULATIVAS
{
    public partial class DatosNotariaWindow : Window
    {
        public DatosNotariaWindow()
        {
            InitializeComponent();
            CargarValoresSiExisten();
        }

        private void BtnGuardar_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtNombreTitular.Text) ||
                string.IsNullOrWhiteSpace(txtNumeroNotaria.Text))
            {
                // Corregido a MessageBoxButton y MessageBoxImage de WPF
                MessageBox.Show("Por favor complete los campos obligatorios.", "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var model = new NotariaModel
            {
                NombreTitular = txtNombreTitular.Text.Trim(),
                NumeroNotaria = txtNumeroNotaria.Text.Trim(),
                DireccionCompleta = txtDireccion.Text.Trim(),
                Telefono = txtTelefono.Text.Trim(),
                EmailContacto = txtEmail.Text.Trim()
            };

            ClsConfiguracion config = new ClsConfiguracion();
            if (config.GuardarOActualizarNotaria(model))
            {
                MessageBox.Show("Datos de la notaría sincronizados correctamente.", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
                this.DialogResult = true;
                this.Close();
            }
            else
            {
                MessageBox.Show("Error al guardar los datos de la notaría.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CargarValoresSiExisten()
        {
            ClsConfiguracion config = new ClsConfiguracion();
            var notaria = config.CargarDatosNotaria();
            if (notaria != null)
            {
                txtNombreTitular.Text = notaria.NombreTitular;
                txtNumeroNotaria.Text = notaria.NumeroNotaria;
                txtDireccion.Text = notaria.DireccionCompleta;
                txtTelefono.Text = notaria.Telefono;
                txtEmail.Text = notaria.EmailContacto;
            }
        }

        private void BtnCerrar_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}