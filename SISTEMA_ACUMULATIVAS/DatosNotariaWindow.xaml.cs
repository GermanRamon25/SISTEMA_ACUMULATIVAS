using System;
using System.Data.SqlClient;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using SISTEMA_ACUMULATIVAS.Conexion;
using SISTEMA_ACUMULATIVAS.Models;

namespace SISTEMA_ACUMULATIVAS
{
    public partial class DatosNotariaWindow : Window
    {
        private int _idUsuarioActual;
        private ClsConexion _conexion;

        // Constructor 1: Se usa desde MainWindow (cuando ya hay sesión iniciada)
        public DatosNotariaWindow()
        {
            InitializeComponent();
            _conexion = new ClsConexion();
            _idUsuarioActual = ClsSesion.UsuarioId;
            ConfigurarEventosTelefono();
            CargarValoresSiExisten();
            txtNombreTitular.Focus();
        }

        // Constructor 2: Se usa inmediatamente después de registrar al usuario nuevo
        public DatosNotariaWindow(int idUsuarioRecienCreado)
        {
            InitializeComponent();
            _conexion = new ClsConexion();
            _idUsuarioActual = idUsuarioRecienCreado;
            ConfigurarEventosTelefono();
            CargarValoresSiExisten();
            txtNombreTitular.Focus();
        }

        // Configuración para limitar longitud y solo permitir números
        private void ConfigurarEventosTelefono()
        {
            if (txtTelefono != null)
            {
                txtTelefono.MaxLength = 10;
                txtTelefono.PreviewTextInput += TxtTelefono_PreviewTextInput;
                txtTelefono.PreviewKeyDown += TxtTelefono_PreviewKeyDown;
            }
        }

        // Bloquear cualquier carácter que no sea número del 0 al 9
        private void TxtTelefono_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        // Bloquear barra espaciadora
        private void TxtTelefono_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Space)
            {
                e.Handled = true;
            }
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
        //              EVENTO ENTER PARA GUARDAR RÁPIDO
        // ============================================================

        private void Input_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                BtnGuardar_Click(sender, new RoutedEventArgs());
            }
        }

        // ============================================================
        //              VALIDACIÓN DE DUPLICADOS EN BD
        // ============================================================

        private string ValidarDuplicadosNotaria(string numeroNotaria, string telefono, string email)
        {
            try
            {
                using (SqlConnection conn = _conexion.GetConnection())
                {
                    if (conn.State != System.Data.ConnectionState.Open) conn.Open();

                    // 1. Validar que no exista el mismo Número de Notaría en otro usuario
                    string queryNumero = @"SELECT COUNT(*) FROM DatosNotaria 
                                          WHERE NumeroNotaria = @NumeroNotaria AND UsuarioId != @UsuarioId";
                    using (SqlCommand cmd = new SqlCommand(queryNumero, conn))
                    {
                        cmd.Parameters.AddWithValue("@NumeroNotaria", numeroNotaria);
                        cmd.Parameters.AddWithValue("@UsuarioId", _idUsuarioActual);

                        int count = Convert.ToInt32(cmd.ExecuteScalar());
                        if (count > 0)
                        {
                            return $"El Número de Notaría '{numeroNotaria}' ya se encuentra registrado por otro usuario.";
                        }
                    }

                    // 2. Validar que no exista el mismo Teléfono en otro usuario
                    if (!string.IsNullOrWhiteSpace(telefono))
                    {
                        string queryTel = @"SELECT COUNT(*) FROM DatosNotaria 
                                           WHERE Telefono = @Telefono AND UsuarioId != @UsuarioId";
                        using (SqlCommand cmd = new SqlCommand(queryTel, conn))
                        {
                            cmd.Parameters.AddWithValue("@Telefono", telefono);
                            cmd.Parameters.AddWithValue("@UsuarioId", _idUsuarioActual);

                            int count = Convert.ToInt32(cmd.ExecuteScalar());
                            if (count > 0)
                            {
                                return $"El número de teléfono '{telefono}' ya se encuentra registrado en otra notaría.";
                            }
                        }
                    }

                    // 3. Validar que no exista el mismo Correo Electrónico en otro usuario
                    if (!string.IsNullOrWhiteSpace(email))
                    {
                        string queryEmail = @"SELECT COUNT(*) FROM DatosNotaria 
                                             WHERE EmailContacto = @Email AND UsuarioId != @UsuarioId";
                        using (SqlCommand cmd = new SqlCommand(queryEmail, conn))
                        {
                            cmd.Parameters.AddWithValue("@Email", email);
                            cmd.Parameters.AddWithValue("@UsuarioId", _idUsuarioActual);

                            int count = Convert.ToInt32(cmd.ExecuteScalar());
                            if (count > 0)
                            {
                                return $"El correo electrónico '{email}' ya se encuentra registrado en otra notaría.";
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return $"Error al validar datos duplicados: {ex.Message}";
            }

            return null; // Sin duplicados
        }

        // ============================================================
        //              GUARDAR / ACTUALIZAR CON VALIDACIONES
        // ============================================================

        private void BtnGuardar_Click(object sender, RoutedEventArgs e)
        {
            string nombreTitular = txtNombreTitular.Text.Trim();
            string numeroNotaria = txtNumeroNotaria.Text.Trim();
            string direccion = txtDireccion.Text.Trim();
            string telefono = txtTelefono.Text.Trim();
            string email = txtEmail.Text.Trim();

            // 1. Validación de campos obligatorios
            if (string.IsNullOrWhiteSpace(nombreTitular))
            {
                MessageBox.Show("Por favor ingresa el nombre del Notario Titular.", "Campo Obligatorio", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtNombreTitular.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(numeroNotaria))
            {
                MessageBox.Show("Por favor ingresa el número de la Notaría Pública.", "Campo Obligatorio", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtNumeroNotaria.Focus();
                return;
            }

            // 2. Validación de Teléfono (Solo números y exactamente 10 dígitos si se ingresó)
            if (!string.IsNullOrWhiteSpace(telefono))
            {
                if (telefono.Length != 10 || !Regex.IsMatch(telefono, @"^\d{10}$"))
                {
                    MessageBox.Show("El número de teléfono debe contener exactamente 10 dígitos numéricos.", "Teléfono Inválido", MessageBoxButton.OK, MessageBoxImage.Warning);
                    txtTelefono.Focus();
                    txtTelefono.SelectAll();
                    return;
                }
            }

            // 3. Validación de formato de correo (solo si escribió algo)
            if (!string.IsNullOrWhiteSpace(email))
            {
                string patronEmail = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
                if (!Regex.IsMatch(email, patronEmail, RegexOptions.IgnoreCase))
                {
                    MessageBox.Show("El correo electrónico ingresado no tiene un formato válido (ejemplo: contacto@notaria.com).", "Formato Inválido", MessageBoxButton.OK, MessageBoxImage.Warning);
                    txtEmail.Focus();
                    txtEmail.SelectAll();
                    return;
                }
            }

            // 4. Validación de no duplicidad de Número de Notaría, Teléfono y Correo
            string mensajeDuplicado = ValidarDuplicadosNotaria(numeroNotaria, telefono, email);
            if (!string.IsNullOrEmpty(mensajeDuplicado))
            {
                MessageBox.Show(mensajeDuplicado, "Dato Ya Registrado", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // 5. Crear el modelo de datos
            var model = new NotariaModel
            {
                NombreTitular = nombreTitular,
                NumeroNotaria = numeroNotaria,
                DireccionCompleta = direccion,
                Telefono = telefono,
                EmailContacto = email
            };

            ClsConfiguracion config = new ClsConfiguracion();

            try
            {
                if (config.GuardarOActualizarNotaria(model, _idUsuarioActual))
                {
                    // 6. Si el usuario guardado es el mismo de la sesión actual, actualizamos ClsSesion en memoria
                    if (_idUsuarioActual == ClsSesion.UsuarioId)
                    {
                        ClsSesion.CargarDatosNotaria(
                            numeroNotaria,
                            nombreTitular,
                            direccion,
                            telefono,
                            email
                        );
                    }

                    MessageBox.Show("Datos de la notaría guardados y sincronizados correctamente.", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);

                    this.DialogResult = true;
                    this.Close();
                }
                else
                {
                    MessageBox.Show("No se pudo guardar la información en la base de datos.", "Error al Guardar", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ocurrió un error inesperado: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ============================================================
        //              CARGA DE INFORMACIÓN EXISTENTE
        // ============================================================

        private void CargarValoresSiExisten()
        {
            if (_idUsuarioActual <= 0) return;

            try
            {
                ClsConfiguracion config = new ClsConfiguracion();
                var notaria = config.CargarDatosNotaria(_idUsuarioActual);

                if (notaria != null)
                {
                    txtNombreTitular.Text = notaria.NombreTitular ?? string.Empty;
                    txtNumeroNotaria.Text = notaria.NumeroNotaria ?? string.Empty;
                    txtDireccion.Text = notaria.DireccionCompleta ?? string.Empty;
                    txtTelefono.Text = notaria.Telefono ?? string.Empty;
                    txtEmail.Text = notaria.EmailContacto ?? string.Empty;
                }
                else
                {
                    txtNombreTitular.Clear();
                    txtNumeroNotaria.Clear();
                    txtDireccion.Clear();
                    txtTelefono.Clear();
                    txtEmail.Clear();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al precargar valores de notaría: {ex.Message}");
            }
        }

        private void BtnCerrar_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}