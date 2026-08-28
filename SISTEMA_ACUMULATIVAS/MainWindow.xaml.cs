using SISTEMA_ACUMULATIVAS.Conexion;
using System;
using System.Data.SqlClient;
using System.Windows;

namespace SISTEMA_ACUMULATIVAS
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            CargarDatosSesion();
            CargarDatosNotariaBanner();
        }

        private void CargarDatosSesion()
        {
            // Verificamos que alguien se haya logueado
            if (ClsSesion.UsuarioId != 0)
            {
                lblUsuarioActual.Text = ClsSesion.NombreUsuario;
                tabPanelControl.Visibility = Visibility.Visible;
            }
            else
            {
                MessageBox.Show("Error de sesión. Nadie ha iniciado sesión.", "Error Fatal", MessageBoxButton.OK, MessageBoxImage.Stop);
                Application.Current.Shutdown();
            }
        }

        public void CargarDatosNotariaBanner()
        {
            try
            {
                ClsConexion conexionDb = new ClsConexion();

                using (var con = conexionDb.GetConnection())
                {
                    // Consultamos las columnas reales de la tabla DatosNotaria
                    string query = @"SELECT TOP 1 NumeroNotaria, NombreTitular, DireccionCompleta, Telefono, EmailContacto 
                                     FROM DatosNotaria 
                                     WHERE UsuarioId = @UsuarioId OR UsuarioId IS NULL 
                                     ORDER BY Id DESC";

                    using (var cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@UsuarioId", ClsSesion.UsuarioId);

                        using (var dr = cmd.ExecuteReader())
                        {
                            if (dr.Read())
                            {
                                string num = dr["NumeroNotaria"] != DBNull.Value ? dr["NumeroNotaria"].ToString() : "";
                                string titular = dr["NombreTitular"] != DBNull.Value ? dr["NombreTitular"].ToString() : "";
                                string direccion = dr["DireccionCompleta"] != DBNull.Value ? dr["DireccionCompleta"].ToString() : "";
                                string telefono = dr["Telefono"] != DBNull.Value ? dr["Telefono"].ToString() : "";
                                string email = dr["EmailContacto"] != DBNull.Value ? dr["EmailContacto"].ToString() : "";

                                // Guardamos en ClsSesion para que AvisoUifView y otros módulos lo utilicen sin consultar de nuevo
                                ClsSesion.CargarDatosNotaria(num, titular, direccion, telefono, email);

                                // Formateamos el texto del header superior izquierdo
                                if (!string.IsNullOrWhiteSpace(num))
                                {
                                    txtBannerNotaria.Text = $"NOTARÍA PÚBLICA NO. {num}";
                                }
                                else
                                {
                                    txtBannerNotaria.Text = "NOTARÍA PÚBLICA";
                                }
                            }
                            else
                            {
                                txtBannerNotaria.Text = "NOTARÍA PÚBLICA (SIN CONFIGURAR)";
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al cargar banner de notaría: {ex.Message}");
                txtBannerNotaria.Text = "NOTARÍA PÚBLICA";
            }
        }

        private void btnConfiguracionNotaria_Click(object sender, RoutedEventArgs e)
        {
            DatosNotariaWindow ventanaNotaria = new DatosNotariaWindow();
            ventanaNotaria.Owner = this;

            // Abre la ventana modal y actualiza el encabezado al guardar cambios
            if (ventanaNotaria.ShowDialog() == true)
            {
                CargarDatosNotariaBanner();
            }
        }

        private void btnCerrarSesion_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("¿Está seguro de que desea cerrar la sesión?", "Confirmar Cierre", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No)
            {
                return;
            }

            ClsSesion.CerrarSesion();
            this.Hide();

            LoginWindow login = new LoginWindow();
            bool? resultado = login.ShowDialog();

            if (resultado == true)
            {
                MainWindow nuevoDashboard = new MainWindow();
                Application.Current.MainWindow = nuevoDashboard;
                nuevoDashboard.Show();
                this.Close();
            }
            else
            {
                Application.Current.Shutdown();
            }
        }

        private void ClienteView_Loaded(object sender, RoutedEventArgs e)
        {

        }
    }
}