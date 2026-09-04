using SISTEMA_ACUMULATIVAS.Conexion;
using SISTEMA_ACUMULATIVAS.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace SISTEMA_ACUMULATIVAS.Views
{
    public partial class PanelControlView : UserControl
    {
        private ClsConexion _conexion;
        private ClsConfiguracion _config;

        public PanelControlView()
        {
            InitializeComponent();
            _conexion = new ClsConexion();
            _config = new ClsConfiguracion();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            CargarUMA();
            CargarCatalogoOperaciones();
            CargarPapelera();
            CargarLogs();
            CargarClientesParaExpediente();
        }

        // --- 1. UMA CONFIGURATION ---
        private void CargarUMA()
        {
            try
            {
                txtUMA.Text = _config.ObtenerUMA().ToString("0.00");
            }
            catch { txtUMA.Text = "0.00"; }
        }

        private void btnGuardarUMA_Click(object sender, RoutedEventArgs e)
        {
            if (decimal.TryParse(txtUMA.Text, out decimal nuevaUma))
            {
                try
                {
                    _config.ActualizarUMA(nuevaUma);
                    MessageBox.Show("Valor de UMA actualizado correctamente.", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex) { MessageBox.Show("Error: " + ex.Message); }
            }
            else MessageBox.Show("Ingrese un número válido.");
        }

        // --- 2. CATÁLOGO DE OPERACIONES ---
        public class CatalogoOperacionItem
        {
            public int Id { get; set; }
            public string Nombre { get; set; }
            public bool EsAvisoObligatorio { get; set; }
            public bool Activo { get; set; }

            public string TextoObligatorio => EsAvisoObligatorio ? "SÍ (Obligatorio)" : "No (Sujeto a Umbral)";
            public string ColorObligatorio => EsAvisoObligatorio ? "#DC2626" : "#64748B";
        }

        private void CargarCatalogoOperaciones()
        {
            List<CatalogoOperacionItem> lista = new List<CatalogoOperacionItem>();
            try
            {
                using (SqlConnection conn = _conexion.GetConnection())
                {
                    string query = "SELECT Id, Nombre, EsAvisoObligatorio, Activo FROM Cat_TiposOperacion ORDER BY Id DESC";
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            lista.Add(new CatalogoOperacionItem
                            {
                                Id = (int)reader["Id"],
                                Nombre = reader["Nombre"].ToString(),
                                EsAvisoObligatorio = (bool)reader["EsAvisoObligatorio"],
                                Activo = (bool)reader["Activo"]
                            });
                        }
                    }
                }
                dgCatalogoOperaciones.ItemsSource = lista;
            }
            catch { }
        }

        private void btnGuardarOperacion_Click(object sender, RoutedEventArgs e)
        {
            string nombreOp = txtNuevaOperacion.Text.Trim();
            bool esObligatorio = chkAvisoObligatorio.IsChecked ?? false;

            if (string.IsNullOrWhiteSpace(nombreOp))
            {
                MessageBox.Show("Debe escribir un nombre para la operación.", "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                using (SqlConnection conn = _conexion.GetConnection())
                {
                    string query = "INSERT INTO Cat_TiposOperacion (Nombre, EsAvisoObligatorio) VALUES (@Nombre, @Obligatorio)";
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@Nombre", nombreOp);
                        cmd.Parameters.AddWithValue("@Obligatorio", esObligatorio);
                        cmd.ExecuteNonQuery();
                    }
                }

                MessageBox.Show("Operación agregada al catálogo exitosamente.", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
                txtNuevaOperacion.Clear();
                chkAvisoObligatorio.IsChecked = false;
                CargarCatalogoOperaciones();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al guardar (Verifique que el nombre no esté duplicado): " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnEliminarOperacion_Click(object sender, RoutedEventArgs e)
        {
            if (dgCatalogoOperaciones.SelectedItem is CatalogoOperacionItem seleccionada)
            {
                if (MessageBox.Show($"¿Está seguro de eliminar el tipo de operación '{seleccionada.Nombre}'?",
                    "Confirmar Eliminación", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                {
                    try
                    {
                        using (SqlConnection conn = _conexion.GetConnection())
                        {
                            string query = "DELETE FROM Cat_TiposOperacion WHERE Id = @Id";
                            using (SqlCommand cmd = new SqlCommand(query, conn))
                            {
                                cmd.Parameters.AddWithValue("@Id", seleccionada.Id);
                                cmd.ExecuteNonQuery();
                            }
                        }

                        MessageBox.Show("Operación eliminada del catálogo exitosamente.", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
                        CargarCatalogoOperaciones();
                    }
                    catch (Exception)
                    {
                        MessageBox.Show("No se puede eliminar esta operación porque ya se encuentra vinculada a registros de operaciones activas en el sistema.",
                            "Aviso de Integridad", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            else
            {
                MessageBox.Show("Por favor, seleccione de la tabla la operación que desea eliminar.", "Selección Requerida", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        // --- 3. RECYCLE BIN ---
        private void CargarPapelera()
        {
            List<Cliente> eliminados = new List<Cliente>();
            try
            {
                using (SqlConnection conn = _conexion.GetConnection())
                {
                    string query = "SELECT Id, Nombre, RFC FROM Clientes WHERE Activo = 0";
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                eliminados.Add(new Cliente
                                {
                                    Id = (int)reader["Id"],
                                    Nombre = reader["Nombre"].ToString(),
                                    RFC = reader["RFC"].ToString()
                                });
                            }
                        }
                    }
                }
                dgPapelera.ItemsSource = eliminados;
            }
            catch { }
        }

        private void btnRefrescarPapelera_Click(object sender, RoutedEventArgs e) { CargarPapelera(); }

        private void btnRestaurar_Click(object sender, RoutedEventArgs e)
        {
            if (dgPapelera.SelectedItem is Cliente cliente)
            {
                if (MessageBox.Show($"¿Restaurar al cliente '{cliente.Nombre}'?", "Confirmar", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    try
                    {
                        using (SqlConnection conn = _conexion.GetConnection())
                        {
                            string query = "UPDATE Clientes SET Activo = 1 WHERE Id = @Id";
                            using (SqlCommand cmd = new SqlCommand(query, conn))
                            {
                                cmd.Parameters.AddWithValue("@Id", cliente.Id);
                                cmd.ExecuteNonQuery();
                            }
                        }
                        CargarPapelera();
                        MessageBox.Show("Cliente restaurado. Ahora aparece en la lista normal.");
                    }
                    catch (Exception ex) { MessageBox.Show("Error: " + ex.Message); }
                }
            }
            else MessageBox.Show("Seleccione un cliente de la lista.");
        }

        // --- 4. EXPEDIENTE ÚNICO DEL CLIENTE ---
        private List<Cliente> _todosLosClientesExp;

        public class OperacionExpediente
        {
            public DateTime FechaOperacion { get; set; }
            public string FolioEscritura { get; set; }
            public string TipoOperacion { get; set; }
            public decimal Monto { get; set; }
            public string Usuario { get; set; }
        }

        private void CargarClientesParaExpediente()
        {
            _todosLosClientesExp = new List<Cliente>();
            try
            {
                using (SqlConnection conn = _conexion.GetConnection())
                {
                    string query = "SELECT Id, Nombre, RFC FROM Clientes WHERE Activo = 1 ORDER BY Nombre";
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                _todosLosClientesExp.Add(new Cliente
                                {
                                    Id = (int)reader["Id"],
                                    Nombre = reader["Nombre"].ToString(),
                                    RFC = reader["RFC"] != DBNull.Value ? reader["RFC"].ToString() : ""
                                });
                            }
                        }
                    }
                }
            }
            catch { }
        }

        private void txtBuscarClienteExp_TextChanged(object sender, TextChangedEventArgs e)
        {
            string busqueda = txtBuscarClienteExp.Text.Trim().ToLower();

            if (string.IsNullOrWhiteSpace(busqueda))
            {
                popupClientesExp.IsOpen = false;
                pnlResumenExpediente.Visibility = Visibility.Collapsed;
                dgExpedienteOperaciones.ItemsSource = null;
                return;
            }

            var filtrados = _todosLosClientesExp
                .Where(c => c.Nombre.ToLower().Contains(busqueda) || (!string.IsNullOrEmpty(c.RFC) && c.RFC.ToLower().Contains(busqueda)))
                .Take(10)
                .ToList();

            if (filtrados.Count > 0 && txtBuscarClienteExp.IsFocused)
            {
                lstClientesExp.ItemsSource = filtrados;
                popupClientesExp.IsOpen = true;
            }
            else
            {
                popupClientesExp.IsOpen = false;
            }
        }

        private void lstClientesExp_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (lstClientesExp.SelectedItem is Cliente seleccionado)
            {
                txtBuscarClienteExp.TextChanged -= txtBuscarClienteExp_TextChanged;
                txtBuscarClienteExp.Text = seleccionado.Nombre;
                txtBuscarClienteExp.TextChanged += txtBuscarClienteExp_TextChanged;

                popupClientesExp.IsOpen = false;
                CargarExpedienteCliente(seleccionado.Id, seleccionado.Nombre, seleccionado.RFC);
            }
        }

        private void CargarExpedienteCliente(int clienteId, string nombre, string rfc)
        {
            List<OperacionExpediente> operaciones = new List<OperacionExpediente>();
            decimal montoTotal = 0;

            try
            {
                using (SqlConnection conn = _conexion.GetConnection())
                {
                    string query = @"SELECT o.FechaOperacion, o.FolioEscritura, o.TipoOperacion, o.Monto, U.Usuario
                                     FROM Operaciones o
                                     LEFT JOIN Usuarios U ON o.UsuarioId = U.Id
                                     WHERE o.ClienteId = @ClienteId
                                     ORDER BY o.FechaOperacion DESC";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@ClienteId", clienteId);
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                decimal monto = reader["Monto"] != DBNull.Value ? (decimal)reader["Monto"] : 0;
                                montoTotal += monto;

                                operaciones.Add(new OperacionExpediente
                                {
                                    FechaOperacion = (DateTime)reader["FechaOperacion"],
                                    FolioEscritura = reader["FolioEscritura"].ToString(),
                                    TipoOperacion = reader["TipoOperacion"].ToString(),
                                    Monto = monto,
                                    Usuario = reader["Usuario"] != DBNull.Value ? reader["Usuario"].ToString() : "N/A"
                                });
                            }
                        }
                    }
                }

                lblExpNombre.Text = nombre;
                lblExpRFC.Text = "RFC: " + (!string.IsNullOrEmpty(rfc) ? rfc : "N/D");
                lblExpTotalOps.Text = operaciones.Count.ToString();
                lblExpMontoTotal.Text = montoTotal.ToString("C");

                dgExpedienteOperaciones.ItemsSource = operaciones;
                pnlResumenExpediente.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al cargar expediente: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // --- DESCARGAR EXPEDIENTE PDF ---
        private void btnDescargarExpediente_Click(object sender, RoutedEventArgs e)
        {
            if (dgExpedienteOperaciones.ItemsSource is List<OperacionExpediente> operaciones && operaciones.Count > 0)
            {
                string nombreCliente = lblExpNombre.Text;
                string rfcCliente = lblExpRFC.Text.Replace("RFC: ", "");
                string totalOperaciones = lblExpTotalOps.Text;
                string montoHistorico = lblExpMontoTotal.Text;

                // Configuramos la ventana de guardado
                Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog
                {
                    FileName = $"Expediente_{rfcCliente}_{DateTime.Now:yyyyMMdd}",
                    DefaultExt = ".pdf",
                    Filter = "Documentos PDF (.pdf)|*.pdf"
                };

                if (dlg.ShowDialog() == true)
                {
                    try
                    {
                        // Llamamos al método estático directamente, sin hacer "new"
                        Services.PdfReporteService.GenerarExpedienteClientePDF(dlg.FileName, nombreCliente, rfcCliente, totalOperaciones, montoHistorico, operaciones);

                        MessageBox.Show("El reporte del expediente se ha generado y guardado exitosamente.", "PDF Generado", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Error al generar el PDF: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            else
            {
                MessageBox.Show("No hay operaciones en el expediente para generar un reporte.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        // --- 5. SECURITY LOGS (AUDIT) ---
        public class LogItem
        {
            public DateTime Fecha { get; set; }
            public string Usuario { get; set; }
            public string Accion { get; set; }
            public string Detalle { get; set; }

            public string AccionAmigable
            {
                get
                {
                    if (Accion == "INSERT Operacion") return "📝 Registro de Operación";
                    if (Accion == "UPDATE Operacion") return "✏️ Modificación de Operación";
                    if (Accion == "INSERT Cliente") return "👤 Nuevo Cliente";
                    if (Accion == "UPDATE Cliente") return "🔄 Actualización de Cliente";
                    return Accion;
                }
            }

            public string ColorAccion
            {
                get
                {
                    if (Accion.Contains("INSERT")) return "#15803D";
                    if (Accion.Contains("UPDATE")) return "#B45309";
                    return "#475569";
                }
            }
        }

        private void CargarLogs()
        {
            List<LogItem> logs = new List<LogItem>();
            try
            {
                using (SqlConnection conn = _conexion.GetConnection())
                {
                    string query = @"SELECT TOP 100 L.Fecha, U.Usuario, L.Accion, L.Detalle 
                                     FROM LogsSistema L
                                     LEFT JOIN Usuarios U ON L.UsuarioId = U.Id
                                     ORDER BY L.Fecha DESC";
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            logs.Add(new LogItem
                            {
                                Fecha = (DateTime)reader["Fecha"],
                                Usuario = reader["Usuario"] != DBNull.Value ? reader["Usuario"].ToString() : "Sistema",
                                Accion = reader["Accion"].ToString(),
                                Detalle = reader["Detalle"].ToString()
                            });
                        }
                    }
                }
                dgLogs.ItemsSource = logs;
            }
            catch { }
        }

        private void btnActualizarLogs_Click(object sender, RoutedEventArgs e) { CargarLogs(); }
    }
}