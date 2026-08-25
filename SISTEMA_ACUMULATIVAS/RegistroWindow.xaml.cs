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

        public RegistroWindow()
        {
            InitializeComponent();
            _conexion = new ClsConexion();
            txtNombreCompleto.Focus();
        }

        // Permite mover la ventana
        private void Grid_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }

        private void btnVolver_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void btnRegistrar_Click(object sender, RoutedEventArgs e)
        {
            string nombre = txtNombreCompleto.Text.Trim();
            string usuario = txtUsuario.Text.Trim();
            string password = txtPassword.Password;
            string confirmPassword = txtConfirmPassword.Password;

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
                if (UsuarioExiste(usuario))
                {
                    MessageBox.Show("El nombre de usuario ya está registrado.", "Usuario Existente", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Generar Hash y Salt con el esquema de seguridad del proyecto
                ClsSeguridad.CrearPasswordHash(password, out byte[] hash, out byte[] salt);

                // Insertar usuario en la BD
                RegistrarUsuario(nombre, usuario, hash, salt);

                MessageBox.Show("Usuario registrado exitosamente. A continuación configure los datos de la notaría.",
                                "Registro Exitoso",
                                MessageBoxButton.OK,
                                MessageBoxImage.Information);

                // 1. Mostrar la ventana modal de la notaría
                DatosNotariaWindow notariaWin = new DatosNotariaWindow();
                notariaWin.Owner = this;
                notariaWin.ShowDialog();

                // 2. Redirigir al Login tras registrar y configurar
                LoginWindow login = new LoginWindow();
                login.Show();
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ocurrió un error al registrar: " + ex.Message, "Error Crítico", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // --- LÓGICA DE BASE DE DATOS ---

        // --- LÓGICA DE BASE DE DATOS ---

        // --- LÓGICA DE BASE DE DATOS ---

        private bool UsuarioExiste(string usuario)
        {
            SqlConnection conn = _conexion.GetConnection();
            try
            {
                // Si no está abierta, se abre; si GetConnection() ya la abrió, no hace nada
                if (conn.State == System.Data.ConnectionState.Closed)
                {
                    conn.Open();
                }

                string query = "SELECT 1 FROM Usuarios WHERE Usuario = @usuario";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@usuario", usuario);
                    object result = cmd.ExecuteScalar();
                    return (result != null);
                }
            }
            finally
            {
                // Cerramos explícitamente para liberar el pool y permitir la siguiente consulta
                if (conn != null && conn.State != System.Data.ConnectionState.Closed)
                {
                    conn.Close();
                }
            }
        }

        private void RegistrarUsuario(string nombre, string usuario, byte[] hash, byte[] salt)
        {
            SqlConnection conn = _conexion.GetConnection();
            try
            {
                // Si no está abierta, se abre; si GetConnection() ya la abrió, no hace nada
                if (conn.State == System.Data.ConnectionState.Closed)
                {
                    conn.Open();
                }

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
            finally
            {
                // Cerramos explícitamente al terminar
                if (conn != null && conn.State != System.Data.ConnectionState.Closed)
                {
                    conn.Close();
                }
            }
        }
    }
}