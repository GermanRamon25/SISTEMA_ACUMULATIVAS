using SISTEMA_ACUMULATIVAS.Conexion;
using SISTEMA_ACUMULATIVAS.Models;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents; // Necesario para imprimir

namespace SISTEMA_ACUMULATIVAS.Views
{
    public partial class AvisoUifView : UserControl
    {
        private ClsConexion _conexion;

        // CONSTANTES DE LEY (Ajustables)

        // Para el ejemplo usaremos un valor de prueba o el real
        private const decimal UMBRAL_AVISO = 8000 * 113.14m;

        public AvisoUifView()
        {
            InitializeComponent();
            _conexion = new ClsConexion();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            CargarFechas();
        }

        private void CargarFechas()
        {
            if (cmbAnio.Items.Count > 0) return; // Evitar recarga

            // Llenar Años
            int anioActual = DateTime.Now.Year;
            cmbAnio.Items.Add(anioActual);
            cmbAnio.Items.Add(anioActual - 1);
            cmbAnio.SelectedIndex = 0;

            // Llenar Meses
            var meses = System.Globalization.CultureInfo.CurrentCulture.DateTimeFormat.MonthNames;
            // Quitamos el elemento vacío que a veces queda al final
            foreach (var mes in meses.Where(m => !string.IsNullOrEmpty(m)))
            {
                cmbMes.Items.Add(mes.ToUpper());
            }
            cmbMes.SelectedIndex = DateTime.Now.Month - 1;
        }

        private void btnBuscar_Click(object sender, RoutedEventArgs e)
        {
            if (cmbMes.SelectedIndex < 0 || cmbAnio.SelectedItem == null) return;

            int mes = cmbMes.SelectedIndex + 1;
            int anio = (int)cmbAnio.SelectedItem;

            CargarClientesAviso(mes, anio);
        }

        private void CargarClientesAviso(int mes, int anio)
        {
            List<ReporteAvisoItem> listaReporte = new List<ReporteAvisoItem>();
            PanelFicha.Visibility = Visibility.Hidden;
            txtInstruccion.Visibility = Visibility.Visible;
            btnImprimir.IsEnabled = false;

            try
            {
                using (SqlConnection conn = _conexion.GetConnection())
                {
                    // PASO 1: Identificar clientes que tuvieron movimiento en el MES seleccionado
                    // No tiene caso revisar clientes que no vinieron este mes.
                    string queryClientesMes = @"
                        SELECT DISTINCT ClienteId 
                        FROM Operaciones 
                        WHERE MONTH(FechaOperacion) = @Mes AND YEAR(FechaOperacion) = @Anio";

                    List<int> clientesActivosIds = new List<int>();

                    using (SqlCommand cmd = new SqlCommand(queryClientesMes, conn))
                    {
                        cmd.Parameters.AddWithValue("@Mes", mes);
                        cmd.Parameters.AddWithValue("@Anio", anio);
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read()) clientesActivosIds.Add((int)reader["ClienteId"]);
                        }
                    }

                    // PASO 2: Para cada cliente activo, revisar su acumulado de los últimos 6 meses
                    // Definimos el rango de 6 meses hacia atrás desde el fin del mes seleccionado
                    DateTime fechaFin = new DateTime(anio, mes, 1).AddMonths(1).AddDays(-1); // Fin de mes
                    DateTime fechaInicio = fechaFin.AddMonths(-5).AddDays(1); // 6 meses atrás (incluyendo el actual)
                    // Ajuste: Inicio de mes de hace 6 meses
                    fechaInicio = new DateTime(fechaInicio.Year, fechaInicio.Month, 1);

                    foreach (int idCliente in clientesActivosIds)
                    {
                        // Calcular Acumulado en el Rango
                        string queryAcumulado = @"
                            SELECT 
                                c.Nombre, c.RFC,
                                SUM(o.Monto) as Total,
                                COUNT(*) as CantidadOps
                            FROM Operaciones o
                            INNER JOIN Clientes c ON o.ClienteId = c.Id
                            WHERE o.ClienteId = @Id 
                              AND o.FechaOperacion >= @Inicio 
                              AND o.FechaOperacion <= @Fin
                            GROUP BY c.Nombre, c.RFC";

                        decimal totalPeriodo = 0;
                        string nombre = "";
                        string rfc = "";

                        using (SqlCommand cmd = new SqlCommand(queryAcumulado, conn))
                        {
                            cmd.Parameters.AddWithValue("@Id", idCliente);
                            cmd.Parameters.AddWithValue("@Inicio", fechaInicio);
                            cmd.Parameters.AddWithValue("@Fin", fechaFin);

                            using (SqlDataReader reader = cmd.ExecuteReader())
                            {
                                if (reader.Read())
                                {
                                    totalPeriodo = (decimal)reader["Total"];
                                    nombre = reader["Nombre"].ToString();
                                    rfc = reader["RFC"].ToString();
                                }
                            }
                        }

                        // SI SUPERA EL UMBRAL -> AGREGAR A LA LISTA
                        if (totalPeriodo >= UMBRAL_AVISO)
                        {
                            var reporteItem = new ReporteAvisoItem
                            {
                                ClienteId = idCliente,
                                NombreCliente = nombre,
                                RFC = rfc,
                                MontoTotalAcumulado = totalPeriodo,
                                MotivoAviso = "Acumulación > Umbral"
                            };

                            // Traer el detalle de las operaciones de ese periodo
                            reporteItem.OperacionesDetalle = ObtenerDetalleOperaciones(conn, idCliente, fechaInicio, fechaFin);

                            listaReporte.Add(reporteItem);
                        }
                    }
                }

                dgClientesAviso.ItemsSource = listaReporte;

                if (listaReporte.Count == 0)
                {
                    MessageBox.Show("No se encontraron clientes que superen el umbral de aviso en este periodo.", "Sin Avisos", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al generar reporte: " + ex.Message);
            }
        }

        private List<Operacion> ObtenerDetalleOperaciones(SqlConnection conn, int clienteId, DateTime inicio, DateTime fin)
        {
            List<Operacion> lista = new List<Operacion>();
            string query = @"
                SELECT FolioEscritura, TipoOperacion, Monto, FechaOperacion 
                FROM Operaciones 
                WHERE ClienteId = @Id AND FechaOperacion >= @Inicio AND FechaOperacion <= @Fin
                ORDER BY FechaOperacion DESC";

            using (SqlCommand cmd = new SqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@Id", clienteId);
                cmd.Parameters.AddWithValue("@Inicio", inicio);
                cmd.Parameters.AddWithValue("@Fin", fin);
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        lista.Add(new Operacion
                        {
                            FolioEscritura = reader["FolioEscritura"].ToString(),
                            TipoOperacion = reader["TipoOperacion"].ToString(),
                            Monto = (decimal)reader["Monto"],
                            FechaOperacion = (DateTime)reader["FechaOperacion"]
                        });
                    }
                }
            }
            return lista;
        }

        private void dgClientesAviso_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dgClientesAviso.SelectedItem is ReporteAvisoItem item)
            {
                // Mostrar la Ficha
                PanelFicha.Visibility = Visibility.Visible;
                txtInstruccion.Visibility = Visibility.Hidden;
                btnImprimir.IsEnabled = true;

                // Llenar datos UI
                lblClienteNombre.Text = item.NombreCliente;
                lblClienteRFC.Text = item.RFC;
                lblTotalMonto.Text = item.MontoTotalAcumulado.ToString("C");

                dgDetalleOperaciones.ItemsSource = item.OperacionesDetalle;
            }
        }

        private void btnImprimir_Click(object sender, RoutedEventArgs e)
        {
            PrintDialog printDialog = new PrintDialog();
            if (printDialog.ShowDialog() == true)
            {
                // Imprimir solo el Panel de la Ficha
                // Usamos un truco visual para imprimir el panel
                printDialog.PrintVisual(PanelFicha, "Ficha Aviso UIF");
            }
        }
    }
}