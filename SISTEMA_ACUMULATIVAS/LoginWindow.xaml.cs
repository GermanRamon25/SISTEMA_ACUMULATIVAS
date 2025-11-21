using SISTEMA_ACUMULATIVAS.Conexion;
using System;
using System.Collections.Generic;
using System.Data; // <--- AGREGADO: Necesario para CommandType.StoredProcedure
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
    /// <summary>
    /// Lógica de interacción para LoginWindow.xaml
    /// </summary>
    public partial class LoginWindow : Window
    {
        private ClsConexion _conexion;
        private bool _passwordVisible = false;

        public LoginWindow()
        {
            InitializeComponent();
            _conexion = new ClsConexion();
            txtUsuario.Focus(); // Pone el cursor en el campo de usuario al iniciar
        }

        // --- Lógica de la Interfaz ---

        private void btnSalir_Click(object sender, RoutedEventArgs e)
        {
            // Cierra la aplicación con un código de "cancelado"
            this.Close();
        }

        // Permite mover la ventana al no tener barra de título
        private void Grid_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            this.DragMove();
        }

        private void btnMostrarPass_Click(object sender, RoutedEventArgs e)
        {
            _passwordVisible = !_passwordVisible; // Invertir estado

            if (_passwordVisible)
            {
                // Mostrar contraseña
                txtPasswordVisible.Text = txtPassword.Password;
                txtPasswordVisible.Visibility = Visibility.Visible;
                txtPassword.Visibility = Visibility.Collapsed;
                btnMostrarPass.Content = "🙈";
            }
            else
            {
                // Ocultar contraseña
                txtPassword.Password = txtPasswordVisible.Text;
                txtPasswordVisible.Visibility = Visibility.Collapsed;
                txtPassword.Visibility = Visibility.Visible;
                btnMostrarPass.Content = "👁️";
            }
        }

        // --- Lógica de Negocio (Backend) ---

        private void btnIniciarSesion_Click(object sender, RoutedEventArgs e)
        {
            string usuario = txtUsuario.Text.Trim();
            // Leemos del control que esté visible
            string password = _passwordVisible ? txtPasswordVisible.Text : txtPassword.Password;

            if (string.IsNullOrEmpty(usuario) || string.IsNullOrEmpty(password))
            {
                MessageBox.Show("Por favor, ingrese usuario y contraseña.", "Campos Vacíos", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                if (ValidarUsuario(usuario, password))
                {
                    // --- NUEVO: MANTENIMIENTO AUTOMÁTICO (REGLA 6 MESES) ---
                    // Antes de cerrar el login, recalculamos los saldos para que 
                    // lo que tenga más de 6 meses se reste automáticamente.
                    EjecutarMantenimientoDiario();
                    // -------------------------------------------------------

                    // Si la validación es exitosa, cerramos el login.
                    // El formulario principal (App.xaml.cs) debe manejar esto.
                    this.DialogResult = true; // Marcamos como OK
                    this.Close(); // Cerramos esta ventana
                }
                else
                {
                    MessageBox.Show("Usuario o contraseña incorrectos.", "Acceso Denegado", MessageBoxButton.OK, MessageBoxImage.Error);
                    txtPassword.Clear();
                    txtPasswordVisible.Clear();
                    txtUsuario.Focus();
                }
            }
            catch (Exception ex)
            {
                // Captura errores de conexión a la BD
                MessageBox.Show(ex.Message, "Error de Conexión", MessageBoxButton.OK, MessageBoxImage.Stop);
            }
        }

        private bool ValidarUsuario(string usuario, string password)
        {
            // Esta lógica es idéntica a la de WinForms, ¡perfecto!
            using (SqlConnection conn = _conexion.GetConnection())
            {
                string query = "SELECT Id, PasswordHash, PasswordSalt, Rol, NombreCompleto FROM Usuarios WHERE Usuario = @usuario AND Activo = 1";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@usuario", usuario);

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read()) // ¿Encontramos al usuario?
                        {
                            int idUsuario = (int)reader["Id"];
                            byte[] hashGuardado = (byte[])reader["PasswordHash"];
                            byte[] saltGuardado = (byte[])reader["PasswordSalt"];
                            string rolUsuario = reader["Rol"].ToString();
                            string nombreUsuario = reader["NombreCompleto"].ToString();

                            // Verificar el hash
                            if (ClsSeguridad.VerificarPasswordHash(password, hashGuardado, saltGuardado))
                            {
                                ClsSesion.IniciarSesion(idUsuario, nombreUsuario, rolUsuario);
                                return true;
                            }
                        }
                    }
                }
            }
            return false; // Usuario no encontrado o contraseña incorrecta
        }

        // --- NUEVO MÉTODO AGREGADO ---
        private void EjecutarMantenimientoDiario()
        {
            try
            {
                // Abre una conexión rápida para ejecutar el Stored Procedure de limpieza
                using (SqlConnection conn = _conexion.GetConnection())
                {
                    // Asegúrate de haber creado el procedimiento 'sp_RecalcularAcumuladosDiarios' en SQL primero
                    using (SqlCommand cmd = new SqlCommand("sp_RecalcularAcumuladosDiarios", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception)
            {
                // Si falla el mantenimiento (ej. no existe el SP todavía), no bloqueamos el Login.
                // Solo lo ignoramos o podríamos usar Debug.WriteLine(ex.Message);
            }
        }

        // Este método es para el link de registro
        private void linkRegistro_Click(object sender, RoutedEventArgs e)
        {
            RegistroWindow registroVentana = new RegistroWindow();
            registroVentana.ShowDialog();
        }
    }
}