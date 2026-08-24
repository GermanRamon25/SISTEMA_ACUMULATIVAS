using SISTEMA_ACUMULATIVAS.Conexion;
using System;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Input;

namespace SISTEMA_ACUMULATIVAS
{
    public partial class LoginWindow : Window
    {
        private ClsConexion _conexion;
        private bool _passwordVisible = false;

        public LoginWindow()
        {
            InitializeComponent();
            _conexion = new ClsConexion();

            // Poner foco en el campo de usuario al abrir la ventana
            this.Loaded += (s, e) => txtUsuario.Focus();
        }

        // ============================================================
        //              EVENTOS DE ARRASTRE DE VENTANA
        // ============================================================

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
            {
                this.DragMove();
            }
        }

        // ============================================================
        //              EVENTOS DE TOGGLE PASSWORD
        // ============================================================

        private void chkMostrarPass_Checked(object sender, RoutedEventArgs e)
        {
            _passwordVisible = true;
            txtPasswordVisible.Text = txtPassword.Password;
            txtPassword.Visibility = Visibility.Collapsed;
            txtPasswordVisible.Visibility = Visibility.Visible;
            txtPasswordVisible.Focus();
            txtPasswordVisible.CaretIndex = txtPasswordVisible.Text.Length;
        }

        private void chkMostrarPass_Unchecked(object sender, RoutedEventArgs e)
        {
            _passwordVisible = false;
            txtPassword.Password = txtPasswordVisible.Text;
            txtPasswordVisible.Visibility = Visibility.Collapsed;
            txtPassword.Visibility = Visibility.Visible;
            txtPassword.Focus();
        }

        private void txtUsuario_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            if (txtErrorMessage.Visibility == Visibility.Visible)
            {
                txtErrorMessage.Visibility = Visibility.Collapsed;
            }
        }

        // ============================================================
        //              EVENTOS DE NAVEGACIÓN
        // ============================================================

        private void linkRegistro_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                RegistroWindow registroVentana = new RegistroWindow();
                registroVentana.Owner = this;
                registroVentana.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al abrir el registro: {ex.Message}",
                                "Error",
                                MessageBoxButton.OK,
                                MessageBoxImage.Error);
            }
        }

        private void btnSalir_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("¿Estás seguro de que deseas salir?",
                                         "Confirmar salida",
                                         MessageBoxButton.YesNo,
                                         MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                Application.Current.Shutdown();
            }
        }

        // ============================================================
        //              LÓGICA DE INICIO DE SESIÓN
        // ============================================================

        private void btnIniciarSesion_Click(object sender, RoutedEventArgs e)
        {
            string usuario = txtUsuario.Text.Trim();
            string password = _passwordVisible ? txtPasswordVisible.Text : txtPassword.Password;

            if (string.IsNullOrEmpty(usuario) || string.IsNullOrEmpty(password))
            {
                MostrarError("Por favor, ingresa usuario y contraseña.");
                return;
            }

            try
            {
                // Verificar conexión a la BD
                if (!_conexion.TestConnection())
                {
                    MostrarError("No se pudo conectar a la base de datos. Verifica tu conexión SQL.");
                    return;
                }

                // Validar usuario
                if (ValidarUsuario(usuario, password))
                {
                    EjecutarMantenimientoDiario();

                    this.DialogResult = true;
                    this.Close();
                }
                else
                {
                    MostrarError("Usuario o contraseña incorrectos.");
                    txtPassword.Clear();
                    txtPasswordVisible.Clear();
                    txtUsuario.Focus();
                    txtUsuario.SelectAll();
                }
            }
            catch (SqlException sqlEx)
            {
                MostrarError($"Error de base de datos: {sqlEx.Message}");
            }
            catch (Exception ex)
            {
                MostrarError($"Error inesperado: {ex.Message}");
            }
        }

        // ============================================================
        //              LÓGICA DE VALIDACIÓN
        // ============================================================

        private bool ValidarUsuario(string usuario, string password)
        {
            using (SqlConnection conn = _conexion.GetConnection())
            {
                string query = @"
                    SELECT Id, PasswordHash, PasswordSalt, Rol, NombreCompleto 
                    FROM Usuarios 
                    WHERE Usuario = @usuario AND Activo = 1";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@usuario", usuario);

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            int idUsuario = (int)reader["Id"];
                            byte[] hashGuardado = (byte[])reader["PasswordHash"];
                            byte[] saltGuardado = (byte[])reader["PasswordSalt"];
                            string rolUsuario = reader["Rol"].ToString();
                            string nombreUsuario = reader["NombreCompleto"].ToString();

                            if (ClsSeguridad.VerificarPasswordHash(password, hashGuardado, saltGuardado))
                            {
                                ClsSesion.IniciarSesion(idUsuario, nombreUsuario, rolUsuario);
                                return true;
                            }
                        }
                    }
                }
            }
            return false;
        }

        // ============================================================
        //              MANTENIMIENTO DIARIO (6 MESES)
        // ============================================================

        private void EjecutarMantenimientoDiario()
        {
            try
            {
                using (SqlConnection conn = _conexion.GetConnection())
                {
                    using (SqlCommand cmd = new SqlCommand("sp_RecalcularAcumuladosDiarios", conn))
                    {
                        cmd.CommandType = System.Data.CommandType.StoredProcedure;
                        cmd.CommandTimeout = 120;
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error en mantenimiento de acumulados: {ex.Message}");
            }
        }

        // ============================================================
        //              MÉTODOS DE AYUDA (UI)
        // ============================================================

        private void MostrarError(string mensaje)
        {
            txtErrorMessage.Text = mensaje;
            txtErrorMessage.Visibility = Visibility.Visible;
        }
    }
}