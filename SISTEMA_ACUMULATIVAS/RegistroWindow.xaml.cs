using SISTEMA_ACUMULATIVAS.Conexion;
using System;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Input;

namespace SISTEMA_ACUMULATIVAS
{
    public partial class RegistroWindow : Window
    {
        private ClsConexion _conexion;
        private bool _passwordVisible = false;
        private bool _confirmPasswordVisible = false;

        public RegistroWindow()
        {
            InitializeComponent();
            _conexion = new ClsConexion();
            txtNombreCompleto.Focus();
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
        //              TOGGLE DE CONTRASEÑAS
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

        private void chkMostrarConfirmPass_Checked(object sender, RoutedEventArgs e)
        {
            _confirmPasswordVisible = true;
            txtConfirmPasswordVisible.Text = txtConfirmPassword.Password;
            txtConfirmPassword.Visibility = Visibility.Collapsed;
            txtConfirmPasswordVisible.Visibility = Visibility.Visible;
            txtConfirmPasswordVisible.Focus();
            txtConfirmPasswordVisible.CaretIndex = txtConfirmPasswordVisible.Text.Length;
        }

        private void chkMostrarConfirmPass_Unchecked(object sender, RoutedEventArgs e)
        {
            _confirmPasswordVisible = false;
            txtConfirmPassword.Password = txtConfirmPasswordVisible.Text;
            txtConfirmPasswordVisible.Visibility = Visibility.Collapsed;
            txtConfirmPassword.Visibility = Visibility.Visible;
            txtConfirmPassword.Focus();
        }

        // ============================================================
        //              EVENTOS DE NAVEGACIÓN Y ACCIÓN
        // ============================================================

        private void btnVolver_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void btnRegistrar_Click(object sender, RoutedEventArgs e)
        {
            string nombre = txtNombreCompleto.Text.Trim();
            string usuario = txtUsuario.Text.Trim();
            string password = _passwordVisible ? txtPasswordVisible.Text : txtPassword.Password;
            string confirmPassword = _confirmPasswordVisible ? txtConfirmPasswordVisible.Text : txtConfirmPassword.Password;

            if (string.IsNullOrEmpty(nombre) || string.IsNullOrEmpty(usuario) || string.IsNullOrEmpty(password))
            {
                MessageBox.Show("Por favor, complete todos los campos obligatorios.", "Campos Incompletos", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (password != confirmPassword)
            {
                MessageBox.Show("Las contraseñas no coinciden.", "Error de Contraseña", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                // 1. Validar si ya existe el usuario
                if (UsuarioExiste(usuario))
                {
                    MessageBox.Show("El nombre de usuario ya está registrado.", "Usuario Existente", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // 2. Generar Hash y Salt
                ClsSeguridad.CrearPasswordHash(password, out byte[] hash, out byte[] salt);

                // 3. Registrar en BD
                RegistrarUsuario(nombre, usuario, hash, salt);

                MessageBox.Show("Usuario registrado exitosamente. A continuación configure los datos de la notaría.",
                                "Registro Exitoso",
                                MessageBoxButton.OK,
                                MessageBoxImage.Information);

                // 4. Abrir la ventana modal de datos de la Notaría
                DatosNotariaWindow notariaWin = new DatosNotariaWindow();
                notariaWin.Owner = this;
                notariaWin.ShowDialog();

                // 5. Cerrar registro
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ocurrió un error al registrar: " + ex.Message, "Error Crítico", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ============================================================
        //              MÉTODOS BD
        // ============================================================

        private bool UsuarioExiste(string usuario)
        {
            using (SqlConnection conn = _conexion.GetConnection())
            {
                string query = "SELECT 1 FROM Usuarios WHERE Usuario = @usuario";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@usuario", usuario);
                    object result = cmd.ExecuteScalar();
                    return (result != null);
                }
            }
        }

        private void RegistrarUsuario(string nombre, string usuario, byte[] hash, byte[] salt)
        {
            using (SqlConnection conn = _conexion.GetConnection())
            {
                string query = @"INSERT INTO Usuarios (Usuario, NombreCompleto, PasswordHash, PasswordSalt, Rol, Activo) 
                                 VALUES (@usuario, @nombre, @hash, @salt, 'Operador', 1)";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@usuario", usuario);
                    cmd.Parameters.AddWithValue("@nombre", nombre);
                    cmd.Parameters.AddWithValue("@hash", hash);
                    cmd.Parameters.AddWithValue("@salt", salt);

                    cmd.ExecuteNonQuery();
                }
            }
        }
    }
}