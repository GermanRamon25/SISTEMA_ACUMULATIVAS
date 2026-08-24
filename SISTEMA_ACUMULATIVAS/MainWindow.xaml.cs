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

                // --- CAMBIO: Se removió la validación de rol (Admin vs Operador).
                // Ahora el panel de control siempre se muestra para cualquier usuario.
                tabPanelControl.Visibility = Visibility.Visible;
            }
            else
            {
                // Si por alguna razón se abre esta ventana sin login, la cerramos.
                MessageBox.Show("Error de sesión. Nadie ha iniciado sesión.", "Error Fatal", MessageBoxButton.OK, MessageBoxImage.Stop);
                Application.Current.Shutdown();
            }
        }

        private void btnCerrarSesion_Click(object sender, RoutedEventArgs e)
        {
            // 1. Preguntar si está seguro
            if (MessageBox.Show("¿Está seguro de que desea cerrar la sesión?", "Confirmar Cierre", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No)
            {
                return;
            }

            // 2. Limpiar la sesión global
            ClsSesion.CerrarSesion();

            // 3. Ocultamos la ventana actual (Dashboard) para que se vea limpio mientras carga el login
            this.Hide();

            // 4. Abrir la ventana de Login EN MODO DIÁLOGO
            LoginWindow login = new LoginWindow();

            // Usamos ShowDialog() en lugar de Show(). 
            // Esto permite que 'DialogResult = true' funcione en LoginWindow.xaml.cs
            bool? resultado = login.ShowDialog();

            if (resultado == true)
            {
                // Si el usuario se logueó correctamente, creamos un NUEVO Dashboard
                // Esto reinicia la ventana principal con los permisos del nuevo usuario
                MainWindow nuevoDashboard = new MainWindow();

                // Le decimos a la App que esta es la nueva ventana principal
                Application.Current.MainWindow = nuevoDashboard;

                nuevoDashboard.Show();

                // Cerramos definitivamente esta instancia vieja del Dashboard
                this.Close();
            }
            else
            {
                // Si cerró la ventana de Login sin entrar (canceló), apagamos la app por completo
                Application.Current.Shutdown();
            }
        }
    }
}