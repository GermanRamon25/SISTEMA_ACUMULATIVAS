using System.Windows;
using System.Windows.Input;
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
            txtNombreTitular.Focus();
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
            {
                this.DragMove();
            }
        }

        private void BtnGuardar_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtNombreTitular.Text) ||
                string.IsNullOrWhiteSpace(txtNumeroNotaria.Text))
            {
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

            // Pasamos el UsuarioId de la sesión actual
            if (config.GuardarOActualizarNotaria(model, ClsSesion.UsuarioId))
            {
                MessageBox.Show("Datos de la notaría sincronizados correctamente.", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);

                // Asignamos true para notificar a MainWindow que actualice el banner
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

            // Consultamos usando el UsuarioId del usuario conectado
            var notaria = config.CargarDatosNotaria(ClsSesion.UsuarioId);

            if (notaria != null)
            {
                txtNombreTitular.Text = notaria.NombreTitular;
                txtNumeroNotaria.Text = notaria.NumeroNotaria;
                txtDireccion.Text = notaria.DireccionCompleta;
                txtTelefono.Text = notaria.Telefono;
                txtEmail.Text = notaria.EmailContacto;
            }
            else
            {
                // Si es un usuario nuevo sin datos previos, dejamos los campos vacíos
                txtNombreTitular.Clear();
                txtNumeroNotaria.Clear();
                txtDireccion.Clear();
                txtTelefono.Clear();
                txtEmail.Clear();
            }
        }

        private void BtnCerrar_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}