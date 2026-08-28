using System;
using System.Data;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using SISTEMA_ACUMULATIVAS.Conexion;

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

            this.Loaded += (s, e) => txtUsuario.Focus();
        }

        // ============================================================
        //              EVENTO DE ARRASTRE DE VENTANA
        // ============================================================

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
            {
                this.DragMove();
            }
        }

        // ============================================================
        //              EVENTOS MOSTRAR / OCULTAR CONTRASEÑA
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

        // ============================================================
        //              EVENTOS DE INPUT Y LIMPIEZA DE ERROR
        // ============================================================

        private void txtUsuario_TextChanged(object sender, TextChangedEventArgs e)
        {
            OcultarError();
        }

        private void txtPassword_PasswordChanged(object sender, RoutedEventArgs e)
        {
            OcultarError();
        }

        private void txtPasswordVisible_TextChanged(object sender, TextChangedEventArgs e)
        {
            OcultarError();
        }

        private void OcultarError()
        {
            if (errorBorder != null && errorBorder.Visibility == Visibility.Visible)
            {
                errorBorder.Visibility = Visibility.Collapsed;
            }
        }

        // Permitir presionar Enter en los campos para iniciar sesión
        private void Input_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                btnIniciarSesion_Click(btnIniciarSesion, new RoutedEventArgs());
            }
        }

        // ============================================================
        //              EVENTOS DE NAVEGACIÓN Y CIERRE
        // ============================================================

        private void linkRegistro_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                OcultarError();
                RegistroWindow registroVentana = new RegistroWindow
                {
                    Owner = this
                };
                registroVentana.ShowDialog();

                // Al volver del registro, limpia los campos y enfoca el usuario
                txtUsuario.Clear();
                txtPassword.Clear();
                txtPasswordVisible.Clear();
                txtUsuario.Focus();
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
                this.DialogResult = false;
                this.Close();
            }
        }

        // ============================================================
        //              LÓGICA DE INICIO DE SESIÓN
        // ============================================================

        private void btnIniciarSesion_Click(object sender, RoutedEventArgs e)
        {
            string usuario = txtUsuario.Text.Trim();
            string password = _passwordVisible ? txtPasswordVisible.Text : txtPassword.Password;

            // Validación de campos vacíos
            if (string.IsNullOrWhiteSpace(usuario) && string.IsNullOrWhiteSpace(password))
            {
                MostrarError("Por favor, ingresa tu usuario y contraseña.");
                txtUsuario.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(usuario))
            {
                MostrarError("Por favor, ingresa tu nombre de usuario.");
                txtUsuario.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(password))
            {
                MostrarError("Por favor, ingresa tu contraseña.");
                if (_passwordVisible) txtPasswordVisible.Focus(); else txtPassword.Focus();
                return;
            }

            try
            {
                // Validación de conexión
                if (!_conexion.TestConnection())
                {
                    MostrarError("No se pudo conectar a la base de datos. Verifica tu conexión SQL.");
                    return;
                }

                // Validación de credenciales
                if (ValidarUsuario(usuario, password))
                {
                    // Cargar los datos de notaría propios de este usuario en sesión
                    CargarDatosNotariaUsuario();

                    EjecutarMantenimientoDiario();

                    this.DialogResult = true;
                    this.Close();
                }
            }
            catch (SqlException sqlEx)
            {
                MostrarError($"Error de base de datos ({sqlEx.Number}): {sqlEx.Message}");
            }
            catch (Exception ex)
            {
                MostrarError($"Error inesperado: {ex.Message}");
            }
        }

        // ============================================================
        //              VALIDACIÓN EN BASE DE DATOS
        // ============================================================

        private bool ValidarUsuario(string usuario, string password)
        {
            using (SqlConnection conn = _conexion.GetConnection())
            {
                if (conn.State != ConnectionState.Open) conn.Open();

                // Consultamos el usuario independientemente del estado para dar retroalimentación clara
                string query = @"
                    SELECT Id, PasswordHash, PasswordSalt, Rol, NombreCompleto, Activo 
                    FROM Usuarios 
                    WHERE Usuario = @usuario";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@usuario", usuario);

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            bool activo = (bool)reader["Activo"];
                            if (!activo)
                            {
                                MostrarError("Esta cuenta de usuario se encuentra desactivada.");
                                return false;
                            }

                            int idUsuario = (int)reader["Id"];
                            byte[] hashGuardado = (byte[])reader["PasswordHash"];
                            byte[] saltGuardado = (byte[])reader["PasswordSalt"];
                            string rolUsuario = reader["Rol"].ToString();
                            string nombreUsuario = reader["NombreCompleto"].ToString();

                            if (ClsSeguridad.VerificarPasswordHash(password, hashGuardado, saltGuardado))
                            {
                                // Registra al usuario en la sesión global con su ID
                                ClsSesion.IniciarSesion(idUsuario, nombreUsuario, rolUsuario);
                                return true;
                            }
                            else
                            {
                                MostrarError("Contraseña incorrecta.");
                                LimpiarPasswordYFoco();
                                return false;
                            }
                        }
                        else
                        {
                            MostrarError("El usuario ingresado no existe.");
                            txtUsuario.Focus();
                            txtUsuario.SelectAll();
                            return false;
                        }
                    }
                }
            }
        }

        // ============================================================
        //              CARGA DE DATOS DE NOTARÍA DEL USUARIO
        // ============================================================

        private void CargarDatosNotariaUsuario()
        {
            try
            {
                ClsConfiguracion config = new ClsConfiguracion();
                var notaria = config.CargarDatosNotaria(ClsSesion.UsuarioId);

                if (notaria != null)
                {
                    ClsSesion.CargarDatosNotaria(
                        notaria.NumeroNotaria,
                        notaria.NombreTitular,
                        notaria.DireccionCompleta,
                        notaria.Telefono,
                        notaria.EmailContacto
                    );
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al precargar datos de notaría en login: {ex.Message}");
            }
        }

        // ============================================================
        //              MANTENIMIENTO DIARIO (ACUMULADOS)
        // ============================================================

        private void EjecutarMantenimientoDiario()
        {
            try
            {
                using (SqlConnection conn = _conexion.GetConnection())
                {
                    if (conn.State != ConnectionState.Open) conn.Open();

                    using (SqlCommand cmd = new SqlCommand("sp_RecalcularAcumuladosDiarios", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
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
        //              UI HELPERS
        // ============================================================

        private void MostrarError(string mensaje)
        {
            txtErrorMessage.Text = mensaje;
            errorBorder.Visibility = Visibility.Visible;
        }

        private void LimpiarPasswordYFoco()
        {
            txtPassword.Clear();
            txtPasswordVisible.Clear();
            if (_passwordVisible)
            {
                txtPasswordVisible.Focus();
            }
            else
            {
                txtPassword.Focus();
            }
        }
    }
}