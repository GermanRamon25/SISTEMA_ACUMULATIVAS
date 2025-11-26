using SISTEMA_ACUMULATIVAS.Conexion;
using SISTEMA_ACUMULATIVAS.Models;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace SISTEMA_ACUMULATIVAS.Views
{
    public partial class AvisoUifView : UserControl
    {
        private ClsConexion _conexion;

        // Umbral General (Inmuebles, Poderes, etc.) ~ $905,120.00
        private const decimal UMBRAL_AVISO_GENERAL = 8000 * 113.14m;

        public AvisoUifView()
        {
            InitializeComponent();
            _conexion = new ClsConexion();
            InitializeWebView();
        }

        private async void InitializeWebView()
        {
            await webView.EnsureCoreWebView2Async(null);
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            CargarFechas();
        }

        private void CargarFechas()
        {
            if (cmbAnio.Items.Count > 0) return;

            int anioActual = DateTime.Now.Year;
            cmbAnio.Items.Add(anioActual);
            cmbAnio.Items.Add(anioActual - 1);
            cmbAnio.SelectedIndex = 0;

            var meses = System.Globalization.CultureInfo.CurrentCulture.DateTimeFormat.MonthNames;
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
            webView.Visibility = Visibility.Hidden;
            txtInstruccion.Visibility = Visibility.Visible;
            btnImprimir.IsEnabled = false;

            try
            {
                using (SqlConnection conn = _conexion.GetConnection())
                {
                    // 1. Detectar clientes que operaron en el mes seleccionado
                    string queryClientesMes = @"SELECT DISTINCT ClienteId FROM Operaciones WHERE MONTH(FechaOperacion) = @Mes AND YEAR(FechaOperacion) = @Anio";
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

                    // Ventana de tiempo (6 meses atrás)
                    DateTime fechaFin = new DateTime(anio, mes, 1).AddMonths(1).AddDays(-1);
                    DateTime fechaInicio = new DateTime(fechaFin.AddMonths(-5).Year, fechaFin.AddMonths(-5).Month, 1);

                    foreach (int idCliente in clientesActivosIds)
                    {
                        // --- CAMBIO CRÍTICO EN LA CONSULTA SQL ---
                        // Ahora verificamos si tiene operaciones de "Compraventa de acciones" usando CASE WHEN
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
                            WHERE o.ClienteId = @Id AND o.FechaOperacion >= @Inicio AND o.FechaOperacion <= @Fin
                            GROUP BY c.Nombre, c.RFC, c.CURP";

                        decimal totalPeriodo = 0;
                        string nombre = "";
                        string rfc = "";
                        string curp = "";
                        bool esAvisoObligatorio = false;

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
                                    curp = reader["CURP"] != DBNull.Value ? reader["CURP"].ToString() : "N/A";

                                    // Leemos la bandera que nos dice si hay acciones/constitución
                                    esAvisoObligatorio = (int)reader["TieneOperacionObligatoria"] == 1;
                                }
                            }
                        }

                        // --- LÓGICA DE NEGOCIO MODIFICADA ---
                        // Avisamos SI supera el monto acumulado O SI es una operación obligatoria (Acciones)
                        if (totalPeriodo >= UMBRAL_AVISO_GENERAL || esAvisoObligatorio)
                        {
                            string motivo = "";
                            if (esAvisoObligatorio)
                                motivo = "Operación Societaria (Aviso Obligatorio)";
                            else
                                motivo = "Acumulación > Umbral General";

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
                if (listaReporte.Count == 0) MessageBox.Show("No hay avisos sujetos a reporte en este periodo.", "Sin Avisos");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message);
            }
        }

        private List<Operacion> ObtenerDetalleOperaciones(SqlConnection conn, int clienteId, DateTime inicio, DateTime fin)
        {
            List<Operacion> lista = new List<Operacion>();
            string query = @"SELECT FolioEscritura, TipoOperacion, Monto, FechaOperacion FROM Operaciones WHERE ClienteId = @Id AND FechaOperacion >= @Inicio AND FechaOperacion <= @Fin ORDER BY FechaOperacion DESC";
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
                webView.Visibility = Visibility.Visible;
                txtInstruccion.Visibility = Visibility.Hidden;
                btnImprimir.IsEnabled = true;

                string htmlContent = GenerarHtmlFicha(item);
                webView.NavigateToString(htmlContent);
            }
        }

        private string GenerarHtmlFicha(ReporteAvisoItem item)
        {
            string filasTabla = "";
            foreach (var op in item.OperacionesDetalle)
            {
                filasTabla += $@"
                    <tr>
                        <td>{op.FechaOperacion:dd/MM/yyyy}</td>
                        <td>{op.FolioEscritura}</td>
                        <td>{op.TipoOperacion}</td>
                        <td style='text-align:right;'>{op.Monto:C}</td>
                    </tr>";
            }

            return $@"
            <html>
            <head>
                <meta charset='UTF-8'>
                <style>
                    body {{ 
                        background-color: #ffffff; 
                        color: #000000; 
                        font-family: 'Segoe UI', sans-serif; 
                        padding: 40px; 
                        font-size: 16px; 
                    }}
                    h1 {{ font-size: 26px; margin: 0 0 20px 0; text-transform: uppercase; }}
                    .info-grid {{ display: grid; grid-template-columns: 200px auto; gap: 10px; margin-bottom: 30px; }}
                    .label {{ font-weight: bold; font-size: 18px; color: #444; }}
                    .value {{ font-size: 18px; }}
                    .total {{ color: #DC3545; font-weight: bold; font-size: 22px; }}
                    table {{ width: 100%; border-collapse: collapse; margin-bottom: 20px; }}
                    th {{ background-color: #f2f2f2; text-align: left; padding: 12px; border-bottom: 2px solid #ccc; font-size: 16px; }}
                    td {{ padding: 12px; border-bottom: 1px solid #eee; font-size: 15px; }}
                    .nota {{ background-color: #f9f9f9; border: 1px solid #ddd; padding: 15px; font-style: italic; font-size: 14px; color: #555; }}
                </style>
            </head>
            <body>
                <div style='border-bottom: 3px solid #007BFF; padding-bottom: 10px; margin-bottom: 20px;'>
                    <h1>Ficha Informativa de Operación Vulnerable</h1>
                </div>

                <div class='info-grid'>
                    <div class='label'>Cliente:</div>
                    <div class='value'>{item.NombreCliente}</div>
                    <div class='label'>RFC:</div>
                    <div class='value'>{item.RFC}</div>
                    <div class='label'>CURP:</div>
                    <div class='value'>{item.CURP}</div>
                    <div class='label'>Total Acumulado:</div>
                    <div class='value total'>{item.MontoTotalAcumulado:C}</div>
                    <div class='label'>Motivo del Aviso:</div>
                    <div class='value' style='color:#d9534f; font-weight:bold;'>{item.MotivoAviso}</div>
                </div>

                <h3 style='font-size: 20px;'>Desglose de Operaciones</h3>
                <table>
                    <thead>
                        <tr>
                            <th>Fecha</th>
                            <th>Folio</th>
                            <th>Tipo de Operación</th>
                            <th style='text-align:right;'>Monto</th>
                        </tr>
                    </thead>
                    <tbody>
                        {filasTabla}
                    </tbody>
                </table>

                <div class='nota'>
                    <strong>Nota Interna:</strong> Esta información debe ser verificada antes de su captura en el portal SPPLD.
                </div>
            </body>
            </html>";
        }

        private async void btnImprimir_Click(object sender, RoutedEventArgs e)
        {
            if (webView != null && webView.CoreWebView2 != null)
            {
                await webView.CoreWebView2.ExecuteScriptAsync("window.print();");
            }
        }
    }
}