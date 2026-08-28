using Microsoft.Win32;
using SISTEMA_ACUMULATIVAS.Conexion;
using SISTEMA_ACUMULATIVAS.Models;
using SISTEMA_ACUMULATIVAS.Services;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace SISTEMA_ACUMULATIVAS.Views
{
    public partial class AvisoUifView : UserControl
    {
        private ClsConexion _conexion;
        private const decimal UMBRAL_AVISO_GENERAL = 8000 * 113.14m;

        public AvisoUifView()
        {
            InitializeComponent();
            _conexion = new ClsConexion();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            CargarPeriodos();
        }

        private void CargarPeriodos()
        {
            // 1. Cargar Años dinámicos (desde 2010 hasta 10 años en el futuro)
            int anioActual = DateTime.Now.Year;
            List<int> anios = new List<int>();

            int anioInicio = 2010;          // Años hacia atrás
            int anioFin = anioActual + 10;  // Años hacia el futuro

            for (int anio = anioFin; anio >= anioInicio; anio--)
            {
                anios.Add(anio);
            }

            cmbAnio.ItemsSource = anios;
            cmbAnio.SelectedItem = anioActual; // Selecciona el año actual

            // 2. Seleccionar el mes actual por defecto
            if (cmbMes.SelectedIndex < 0)
            {
                cmbMes.SelectedIndex = DateTime.Now.Month - 1;
            }
        }

        private void btnBuscar_Click(object sender, RoutedEventArgs e)
        {
            if (cmbMes.SelectedIndex < 0 || cmbAnio.SelectedItem == null)
            {
                MessageBox.Show("Seleccione un mes y un año válidos.", "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            int mes = cmbMes.SelectedIndex + 1;
            int anio = (int)cmbAnio.SelectedItem;

            CargarClientesAviso(mes, anio);
        }

        private void CargarClientesAviso(int mes, int anio)
        {
            List<ReporteAvisoItem> listaReporte = new List<ReporteAvisoItem>();
            scrollFicha.Visibility = Visibility.Collapsed;
            txtInstruccion.Visibility = Visibility.Visible;
            btnImprimir.IsEnabled = false;

            try
            {
                using (SqlConnection conn = _conexion.GetConnection())
                {
                    // 1. Clientes del usuario que operaron en el mes/año seleccionado
                    string queryClientesMes = @"
                        SELECT DISTINCT ClienteId 
                        FROM Operaciones 
                        WHERE MONTH(FechaOperacion) = @Mes 
                          AND YEAR(FechaOperacion) = @Anio 
                          AND UsuarioId = @UsuarioId";

                    List<int> clientesActivosIds = new List<int>();

                    using (SqlCommand cmd = new SqlCommand(queryClientesMes, conn))
                    {
                        cmd.Parameters.AddWithValue("@Mes", mes);
                        cmd.Parameters.AddWithValue("@Anio", anio);
                        cmd.Parameters.AddWithValue("@UsuarioId", ClsSesion.UsuarioId);

                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                clientesActivosIds.Add((int)reader["ClienteId"]);
                            }
                        }
                    }

                    DateTime fechaFin = new DateTime(anio, mes, 1).AddMonths(1).AddDays(-1);
                    DateTime fechaInicio = new DateTime(fechaFin.AddMonths(-5).Year, fechaFin.AddMonths(-5).Month, 1);

                    foreach (int idCliente in clientesActivosIds)
                    {
                        // 2. Acumulación de montos solo de este usuario
                        string queryAcumulado = @"
                            SELECT 
                                c.Nombre, 
                                c.RFC, 
                                c.CURP, 
                                SUM(o.Monto) as Total,
                                MAX(CASE 
                                    WHEN o.TipoOperacion LIKE '%Compraventa de acciones%' THEN 1 
                                    WHEN o.TipoOperacion LIKE '%Constitución%' THEN 1 
                                    ELSE 0 
                                END) as TieneOperacionObligatoria
                            FROM Operaciones o
                            INNER JOIN Clientes c ON o.ClienteId = c.Id
                            WHERE o.ClienteId = @Id 
                              AND o.UsuarioId = @UsuarioId
                              AND o.FechaOperacion >= @Inicio 
                              AND o.FechaOperacion <= @Fin
                            GROUP BY c.Nombre, c.RFC, c.CURP";

                        decimal totalPeriodo = 0;
                        string nombre = "";
                        string rfc = "";
                        string curp = "";
                        bool esAvisoObligatorio = false;

                        using (SqlCommand cmd = new SqlCommand(queryAcumulado, conn))
                        {
                            cmd.Parameters.AddWithValue("@Id", idCliente);
                            cmd.Parameters.AddWithValue("@UsuarioId", ClsSesion.UsuarioId);
                            cmd.Parameters.AddWithValue("@Inicio", fechaInicio);
                            cmd.Parameters.AddWithValue("@Fin", fechaFin);

                            using (SqlDataReader reader = cmd.ExecuteReader())
                            {
                                if (reader.Read())
                                {
                                    totalPeriodo = (decimal)reader["Total"];
                                    nombre = reader["Nombre"].ToString();
                                    rfc = reader["RFC"].ToString();
                                    curp = reader["CURP"] != DBNull.Value ? reader["CURP"].ToString() : "N/A";
                                    esAvisoObligatorio = (int)reader["TieneOperacionObligatoria"] == 1;
                                }
                            }
                        }

                        if (totalPeriodo >= UMBRAL_AVISO_GENERAL || esAvisoObligatorio)
                        {
                            string motivo = esAvisoObligatorio
                                ? "Operación Societaria (Aviso Obligatorio)"
                                : "Acumulación > Umbral General (8,000 UMAs)";

                            var reporteItem = new ReporteAvisoItem
                            {
                                ClienteId = idCliente,
                                NombreCliente = nombre,
                                RFC = rfc,
                                CURP = curp,
                                MontoTotalAcumulado = totalPeriodo,
                                MotivoAviso = motivo,
                                OperacionesDetalle = ObtenerDetalleOperaciones(conn, idCliente, fechaInicio, fechaFin)
                            };
                            listaReporte.Add(reporteItem);
                        }
                    }
                }

                dgClientesAviso.ItemsSource = listaReporte;
                if (listaReporte.Count == 0)
                {
                    MessageBox.Show("No hay avisos sujetos a reporte en este periodo para su usuario.", "Sin Avisos", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message, "Error BD", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private List<Operacion> ObtenerDetalleOperaciones(SqlConnection conn, int clienteId, DateTime inicio, DateTime fin)
        {
            List<Operacion> lista = new List<Operacion>();

            // 3. Desglose de actos notariales filtrado por usuario
            string query = @"
                SELECT FolioEscritura, TipoOperacion, Monto, FechaOperacion 
                FROM Operaciones 
                WHERE ClienteId = @Id 
                  AND UsuarioId = @UsuarioId
                  AND FechaOperacion >= @Inicio 
                  AND FechaOperacion <= @Fin 
                ORDER BY FechaOperacion ASC";

            using (SqlCommand cmd = new SqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@Id", clienteId);
                cmd.Parameters.AddWithValue("@UsuarioId", ClsSesion.UsuarioId);
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

            decimal sumaAcumulada = 0;
            bool yaSuperoUmbral = false;

            foreach (var op in lista)
            {
                sumaAcumulada += op.Monto;

                if (op.TipoOperacion.IndexOf("Compraventa de acciones", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    op.TipoOperacion.IndexOf("Constitución", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    op.EsDetonante = true;
                    op.EtiquetaDetonante = "Aviso Obligatorio";
                }
                else if (!yaSuperoUmbral && sumaAcumulada >= UMBRAL_AVISO_GENERAL)
                {
                    op.EsDetonante = true;
                    op.EtiquetaDetonante = "Supera Umbral Acumulado";
                    yaSuperoUmbral = true;
                }
            }

            return lista.OrderByDescending(x => x.FechaOperacion).ToList();
        }

        private void dgClientesAviso_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dgClientesAviso.SelectedItem is ReporteAvisoItem item)
            {
                txtInstruccion.Visibility = Visibility.Collapsed;
                scrollFicha.Visibility = Visibility.Visible;
                btnImprimir.IsEnabled = true;

                // 1. Datos de la Notaría desde ClsSesion
                string numNotaria = !string.IsNullOrWhiteSpace(ClsSesion.NumeroNotaria) ? ClsSesion.NumeroNotaria : "---";
                string titular = !string.IsNullOrWhiteSpace(ClsSesion.NombreTitular) ? ClsSesion.NombreTitular.ToUpper() : "TITULAR NO CONFIGURADO";
                string direccion = !string.IsNullOrWhiteSpace(ClsSesion.DireccionCompleta) ? ClsSesion.DireccionCompleta : "Ubicación no configurada";

                txtFichaTituloNotaria.Text = $"NOTARÍA PÚBLICA NO. {numNotaria}";
                txtFichaDireccion.Text = $"{direccion} | Control Interno LFPIORPI";
                txtFichaTitularFirma.Text = $"LIC. {titular}";
                txtFichaSubtituloFirma.Text = $"Notario Público Titular No. {numNotaria}";

                // 2. Datos del Cliente seleccionado
                txtFichaCliente.Text = item.NombreCliente;
                txtFichaRfcCurp.Text = $"{item.RFC} | {item.CURP}";
                txtFichaMonto.Text = item.MontoTotalAcumulado.ToString("C2");
                txtFichaCriterio.Text = item.MotivoAviso;

                // 3. Desglose de Operaciones
                dgOperacionesFicha.ItemsSource = item.OperacionesDetalle;
            }
        }

        private void btnImprimir_Click(object sender, RoutedEventArgs e)
        {
            ReporteAvisoItem itemSeleccionado = dgClientesAviso.SelectedItem as ReporteAvisoItem;
            if (itemSeleccionado == null)
            {
                MessageBox.Show("Seleccione un cliente de la lista para generar el reporte.", "Atención", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                SaveFileDialog saveDialog = new SaveFileDialog
                {
                    Filter = "Documento PDF (*.pdf)|*.pdf",
                    FileName = $"Ficha_UIF_{itemSeleccionado.RFC}_{DateTime.Now:yyyyMMdd}.pdf",
                    Title = "Guardar Ficha Informativa UIF"
                };

                if (saveDialog.ShowDialog() == true)
                {
                    // Genera el archivo PDF directamente con diseño estructurado
                    PdfReporteService.GenerarFichaUif(saveDialog.FileName, itemSeleccionado);

                    var respuesta = MessageBox.Show("Ficha generada exitosamente.\n\n¿Desea abrir el archivo ahora?", "Éxito", MessageBoxButton.YesNo, MessageBoxImage.Information);

                    if (respuesta == MessageBoxResult.Yes)
                    {
                        Process.Start(new ProcessStartInfo(saveDialog.FileName) { UseShellExecute = true });
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al generar el PDF: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}