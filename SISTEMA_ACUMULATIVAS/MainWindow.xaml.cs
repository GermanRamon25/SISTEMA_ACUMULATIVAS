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
            CargarBannerNotaria();
            CargarDatosNotariaBanner();
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

        private void CargarBannerNotaria()
        {
            ClsConfiguracion config = new ClsConfiguracion();

            // Pasamos el ID del usuario en sesión actual
            var notaria = config.CargarDatosNotaria(ClsSesion.UsuarioId);

            if (notaria != null)
            {
                txtBannerNotaria.Text = $"NOTARÍA PÚBLICA NO. {notaria.NumeroNotaria} - {notaria.NombreTitular}";
            }
            else
            {
                txtBannerNotaria.Text = "NOTARÍA PÚBLICA (SIN CONFIGURAR)";
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

        private void CargarDatosNotariaBanner()
        {
            try
            {
                Conexion.ClsConexion conexionDb = new Conexion.ClsConexion();

                using (var con = conexionDb.GetConnection())
                {
                    if (con.State != System.Data.ConnectionState.Open)
                        con.Open();

                    // Filtramos por el UsuarioId de la sesión
                    string query = "SELECT NumeroNotaria, Municipio FROM DatosNotaria WHERE UsuarioId = @UsuarioId";

                    using (var cmd = new System.Data.SqlClient.SqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@UsuarioId", Conexion.ClsSesion.UsuarioId);

                        using (var dr = cmd.ExecuteReader())
                        {
                            if (dr.Read())
                            {
                                string num = dr["NumeroNotaria"] != DBNull.Value ? dr["NumeroNotaria"].ToString() : "";
                                string municipio = dr["Municipio"] != DBNull.Value ? dr["Municipio"].ToString() : "";

                                txtBannerNotaria.Text = !string.IsNullOrWhiteSpace(num)
                                    ? $"NOTARÍA PÚBLICA NO. {num}" + (!string.IsNullOrWhiteSpace(municipio) ? $" - {municipio.ToUpper()}" : "")
                                    : "NOTARÍA PÚBLICA";
                            }
                            else
                            {
                                txtBannerNotaria.Text = "NOTARÍA PÚBLICA (SIN CONFIGURAR)";
                            }
                        }
                    }
                }
            }
            catch (Exception)
            {
                txtBannerNotaria.Text = "NOTARÍA PÚBLICA";
            }
        }

        private void btnConfiguracionNotaria_Click(object sender, RoutedEventArgs e)
        {
            DatosNotariaWindow ventanaNotaria = new DatosNotariaWindow();
            ventanaNotaria.Owner = this;

            // Abre la ventana modal y actualiza el encabezado al cerrar
            if (ventanaNotaria.ShowDialog() == true)
            {
                CargarDatosNotariaBanner();
            }
        }
    }
}