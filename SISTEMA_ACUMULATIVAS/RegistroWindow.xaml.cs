using SISTEMA_ACUMULATIVAS.Conexion;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
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
using System.Windows.Shapes;

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
            this.DragMove();
        }

        private void btnVolver_Click(object sender, RoutedEventArgs e)
        {
            this.Close(); // Simplemente cierra esta ventana
        }

        private void btnRegistrar_Click(object sender, RoutedEventArgs e)
        {
            string nombre = txtNombreCompleto.Text.Trim();
            string usuario = txtUsuario.Text.Trim();
            string password = txtPassword.Password;
            string confirmPassword = txtConfirmPassword.Password;

            // --- INICIO DE VALIDACIONES ---
            if (string.IsNullOrEmpty(nombre) || string.IsNullOrEmpty(usuario) || string.IsNullOrEmpty(password))
            {
                MessageBox.Show("Todos los campos son obligatorios.", "Error de Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (password != confirmPassword)
            {
                MessageBox.Show("Las contraseñas no coinciden.", "Error de Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (UsuarioExiste(usuario))
            {
                MessageBox.Show("El 'Nombre de Usuario' ya está en uso. Por favor, elija otro.", "Usuario Duplicado", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            // --- FIN DE VALIDACIONES ---

            try
            {
                // 1. Generar el Hash y Salt
                ClsSeguridad.CrearPasswordHash(password, out byte[] hash, out byte[] salt);

                // 2. Registrar en la Base de Datos
                RegistrarUsuario(nombre, usuario, hash, salt);

                MessageBox.Show("¡Usuario registrado exitosamente!", "Registro Completo", MessageBoxButton.OK, MessageBoxImage.Information);
                this.Close(); // Cerrar la ventana de registro
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al registrar el usuario: " + ex.Message, "Error de Base de Datos", MessageBoxButton.OK, MessageBoxImage.Stop);
            }
        }

        // --- LÓGICA DE BASE DE DATOS ---

        private bool UsuarioExiste(string usuario)
        {
            using (SqlConnection conn = _conexion.GetConnection())
            {
                string query = "SELECT 1 FROM Usuarios WHERE Usuario = @usuario";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@usuario", usuario);
                    object result = cmd.ExecuteScalar();
                    return (result != null); // Si no es nulo, significa que encontró un '1' (existe)
                }
            }
        }

        private void RegistrarUsuario(string nombre, string usuario, byte[] hash, byte[] salt)
        {
            using (SqlConnection conn = _conexion.GetConnection())
            {
                // IMPORTANTE: Por seguridad, todos los usuarios nuevos se crean como 'Operador'
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