using System;
using System.Data;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using SISTEMA_ACUMULATIVAS.Conexion;

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
            this.Loaded += (s, e) => txtNombreCompleto.Focus();
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
        //              EVENTO ENTER PARA INPUTS
        // ============================================================

        private void Input_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                btnRegistrar_Click(btnRegistrar, new RoutedEventArgs());
            }
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

            // 1. Obtener contraseñas según visibilidad
            string password = _passwordVisible ? txtPasswordVisible.Text : txtPassword.Password;
            string confirmarPassword = _confirmPasswordVisible ? txtConfirmPasswordVisible.Text : txtConfirmPassword.Password;

            // 2. Validación únicamente de campos obligatorios no vacíos
            if (string.IsNullOrWhiteSpace(nombre) || string.IsNullOrWhiteSpace(usuario) || string.IsNullOrEmpty(password))
            {
                MessageBox.Show("Por favor llena todos los campos.", "Campos vacíos", MessageBoxButton.OK, MessageBoxImage.Warning);
                if (string.IsNullOrWhiteSpace(nombre)) txtNombreCompleto.Focus();
                else if (string.IsNullOrWhiteSpace(usuario)) txtUsuario.Focus();
                else if (_passwordVisible) txtPasswordVisible.Focus(); else txtPassword.Focus();
                return;
            }

            // 3. Validación de coincidencia de contraseñas
            if (password != confirmarPassword)
            {
                MessageBox.Show("Las contraseñas no coinciden.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtConfirmPassword.Clear();
                txtConfirmPasswordVisible.Clear();
                if (_confirmPasswordVisible) txtConfirmPasswordVisible.Focus(); else txtConfirmPassword.Focus();
                return;
            }

            // 4. Validación de conexión con la base de datos
            if (!_conexion.TestConnection())
            {
                MessageBox.Show("No se pudo conectar a la base de datos. Verifica tu conexión SQL.", "Error de Conexión", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // 5. Validación de usuario duplicado
            if (UsuarioExiste(usuario))
            {
                MessageBox.Show("El nombre de usuario ya se encuentra registrado. Elige otro.", "Usuario duplicado", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtUsuario.Focus();
                txtUsuario.SelectAll();
                return;
            }

            try
            {
                // Encriptación de contraseña libre (cualquier longitud o caracteres)
                ClsSeguridad.CrearPasswordHash(password, out byte[] passwordHash, out byte[] passwordSalt);

                // Inserción en la base de datos
                int nuevoUsuarioId = RegistrarUsuarioYRetornarId(nombre, usuario, passwordHash, passwordSalt);

                if (nuevoUsuarioId > 0)
                {
                    MessageBox.Show("Usuario registrado con éxito. A continuación configure los datos de la notaría.",
                                    "Paso siguiente", MessageBoxButton.OK, MessageBoxImage.Information);

                    this.Visibility = Visibility.Collapsed;

                    DatosNotariaWindow frmNotaria = new DatosNotariaWindow(nuevoUsuarioId);
                    frmNotaria.Owner = this.Owner;
                    frmNotaria.ShowDialog();

                    this.Close();
                }
                else
                {
                    MessageBox.Show("No se pudo registrar el usuario en la base de datos.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (SqlException sqlEx)
            {
                MessageBox.Show($"Error de base de datos ({sqlEx.Number}): {sqlEx.Message}", "Error SQL", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ocurrió un error en el registro: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ============================================================
        //              MÉTODOS BD
        // ============================================================

        private bool UsuarioExiste(string usuario)
        {
            using (SqlConnection conn = _conexion.GetConnection())
            {
                if (conn.State != ConnectionState.Open) conn.Open();

                string query = "SELECT 1 FROM Usuarios WHERE Usuario = @usuario";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@usuario", usuario);
                    object result = cmd.ExecuteScalar();
                    return (result != null);
                }
            }
        }

        private int RegistrarUsuarioYRetornarId(string nombre, string usuario, byte[] hash, byte[] salt)
        {
            using (SqlConnection conn = _conexion.GetConnection())
            {
                if (conn.State != ConnectionState.Open) conn.Open();

                string query = @"INSERT INTO Usuarios (Usuario, NombreCompleto, PasswordHash, PasswordSalt, Rol, Activo, FechaCreacion) 
                                 VALUES (@usuario, @nombre, @hash, @salt, 'Operador', 1, GETDATE());
                                 SELECT SCOPE_IDENTITY();";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@usuario", usuario);
                    cmd.Parameters.AddWithValue("@nombre", nombre);
                    cmd.Parameters.AddWithValue("@hash", hash);
                    cmd.Parameters.AddWithValue("@salt", salt);

                    object result = cmd.ExecuteScalar();
                    return (result != null && result != DBNull.Value) ? Convert.ToInt32(result) : 0;
                }
            }
        }
    }
}