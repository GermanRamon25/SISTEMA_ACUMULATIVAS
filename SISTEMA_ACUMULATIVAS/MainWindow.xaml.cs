using SISTEMA_ACUMULATIVAS.Conexion;
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
using System.Windows.Navigation;
using System.Windows.Shapes;



namespace SISTEMA_ACUMULATIVAS
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            CargarDatosSesion();
        }

        private void CargarDatosSesion()
        {
            // Verificamos que alguien se haya logueado
            if (ClsSesion.UsuarioId != 0)
            {
                lblUsuarioActual.Text = ClsSesion.NombreUsuario;
            }
            else
            {
                // Si por alguna razón se abre esta ventana sin login, la cerramos.
                MessageBox.Show("Error de sesión. Nadie ha iniciado sesión.", "Error Fatal", MessageBoxButton.OK, MessageBoxImage.Stop);
                Application.Current.Shutdown();
            }
        }

        // --- ESTA ES LA FUNCIÓN QUE FALTA Y CAUSA EL ERROR ---
        private void btnCerrarSesion_Click(object sender, RoutedEventArgs e)
        {
            // 1. Preguntar si está seguro
            if (MessageBox.Show("¿Está seguro de que desea cerrar la sesión?", "Confirmar Cierre", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No)
            {
                return;
            }

            // 2. Limpiar la sesión global
            ClsSesion.CerrarSesion();

            // 3. Abrir la ventana de Login
            LoginWindow login = new LoginWindow();
            login.Show();

            // 4. Cerrar esta ventana (el Dashboard)
            this.Close();
        }
    }
}