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
                                    esAvisoObligatorio = (int)reader["TieneOperacionObligatoria"] == 1;
                                }
                            }
                        }

                        if (totalPeriodo >= UMBRAL_AVISO_GENERAL || esAvisoObligatorio)
                        {
                            string motivo = "";
                            if (esAvisoObligatorio)
                                motivo = "Operación Societaria (Aviso Obligatorio)";
                            else
                                motivo = "Acumulación > Umbral General (8,000 UMAs)";

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

                // Llamada directa al método interno
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
                    <td><strong>{op.FolioEscritura}</strong></td>
                    <td>{op.TipoOperacion}</td>
                    <td style='text-align: right; font-weight: bold;'>{op.Monto:C}</td>
                </tr>";
            }

            return $@"
    <!DOCTYPE html>
    <html>
    <head>
        <meta charset='UTF-8'>
        <style>
            @page {{
                size: letter portrait;
                margin: 15mm 15mm 15mm 15mm;
            }}

            @media print {{
                body {{
                    -webkit-print-color-adjust: exact;
                    print-color-adjust: exact;
                }}
            }}

            body {{ 
                background-color: #ffffff; 
                color: #1E293B; 
                font-family: 'Segoe UI', Arial, sans-serif; 
                padding: 20px; 
                font-size: 13px; 
                line-height: 1.5; 
            }}
            .header {{ 
                text-align: center; 
                border-bottom: 2px solid #0284C7; 
                padding-bottom: 12px; 
                margin-bottom: 18px; 
            }}
            .header h1 {{ 
                margin: 0; 
                font-size: 18px; 
                color: #0F172A; 
                text-transform: uppercase; 
                letter-spacing: 0.5px;
            }}
            .header h2 {{ 
                margin: 4px 0 0 0; 
                font-size: 13px; 
                color: #0284C7; 
                font-weight: 600; 
            }}
            .header p {{ 
                margin: 2px 0 0 0; 
                font-size: 11px; 
                color: #64748B; 
            }}
            .legal-box {{ 
                background-color: #F8FAFC; 
                border-left: 4px solid #0284C7; 
                padding: 10px 14px; 
                margin-bottom: 18px; 
                font-size: 11.5px; 
                color: #334155; 
                text-align: justify;
            }}
            .info-table {{ 
                width: 100%; 
                border-collapse: collapse; 
                margin-bottom: 20px; 
            }}
            .info-table td {{ 
                padding: 6px 10px; 
                border-bottom: 1px solid #E2E8F0; 
                font-size: 12.5px; 
            }}
            .info-table .label {{ 
                font-weight: bold; 
                width: 32%; 
                color: #475569; 
                background-color: #F8FAFC; 
            }}
            .table-grid {{ 
                width: 100%; 
                border-collapse: collapse; 
                margin-top: 8px; 
                margin-bottom: 20px; 
            }}
            .table-grid th {{ 
                background-color: #1E293B; 
                color: white; 
                padding: 8px 10px; 
                font-size: 12px; 
                text-align: left; 
            }}
            .table-grid td {{ 
                padding: 8px 10px; 
                border-bottom: 1px solid #CBD5E1; 
                font-size: 12px; 
            }}
            .alert-box {{ 
                background-color: #FEF2F2; 
                border-left: 4px solid #DC2626; 
                padding: 10px 14px; 
                margin-bottom: 25px; 
                font-size: 11.5px; 
                color: #7F1D1D; 
                text-align: justify;
            }}
            .footer {{ 
                margin-top: 35px; 
                text-align: center; 
                font-size: 12px; 
            }}
            .signature-line {{ 
                width: 260px; 
                border-top: 1px solid #64748B; 
                margin: 45px auto 8px auto; 
            }}
            .system-foot {{ 
                margin-top: 20px; 
                font-size: 10px; 
                color: #94A3B8; 
                border-top: 1px solid #E2E8F0; 
                padding-top: 6px; 
            }}
        </style>
    </head>
    <body>
        <div class='header'>
            <h1>Notaría Pública No. 215</h1>
            <h2>Ficha Informativa de Operación Vulnerable y Acumulación</h2>
            <p>Guasave, Sinaloa | Control Interno LFPIORPI</p>
        </div>

        <div class='legal-box'>
            <strong>FUNDAMENTO LEGAL (LFPIORPI):</strong><br/>
            Conforme a lo dispuesto por el artículo 17, fracción XII, y artículo 18 de la Ley Federal para la Prevención e Identificación de Operaciones con Recursos de Procedencia Ilícita, así como los artículos 27 y 30 de sus Reglas de Carácter General, se emite la presente ficha técnica relativa al registro de actos, acumulación de montos y seguimiento de umbrales en Unidades de Medida y Actualización (UMA).
        </div>

        <table class='info-table'>
            <tr>
                <td class='label'>Cliente / Razón Social:</td>
                <td><strong>{item.NombreCliente}</strong></td>
            </tr>
            <tr>
                <td class='label'>RFC / CURP:</td>
                <td>{item.RFC} | {item.CURP}</td>
            </tr>
            <tr>
                <td class='label'>Monto Total Acumulado (6 Meses):</td>
                <td><strong style='color: #0284C7; font-size: 14px;'>{item.MontoTotalAcumulado:C}</strong></td>
            </tr>
            <tr>
                <td class='label'>Motivo / Criterio del Aviso:</td>
                <td><strong style='color: #DC2626;'>{item.MotivoAviso}</strong></td>
            </tr>
        </table>

        <h3 style='font-size: 13px; color: #1E293B; margin-bottom: 6px;'>Desglose de Operaciones en el Periodo</h3>
        <table class='table-grid'>
            <thead>
                <tr>
                    <th>Fecha</th>
                    <th>Folio</th>
                    <th>Tipo de Operación Notarial</th>
                    <th style='text-align: right;'>Monto</th>
                </tr>
            </thead>
            <tbody>
                {filasTabla}
            </tbody>
        </table>

        <div class='alert-box'>
            <strong>DISPOSICIÓN PARA PRESENTACIÓN DE AVISO (PORTAL SPPLD):</strong><br/>
            La presente ficha técnica certifica que el monto o la naturaleza del acto notarial actualiza la obligación de emitir el Aviso correspondiente a través del Portal de Prevención de Lavado de Dinero (SPPLD - SAT). El aviso deberá formalizarse a más tardar el <strong>día 17 del mes inmediato siguiente</strong> a la fecha del instrumento notarial. La información descrita ha sido validada contra el protocolo notarial.
        </div>

        <div class='footer'>
            <div class='signature-line'></div>
            <strong>LIC. SERGIO AGUILASOCHO GARCÍA</strong><br/>
            <span>Notario Público Titular No. 215</span>
            
            <div class='system-foot'>
                2026 SISTEMA DE ACUMULATIVAS | Control de Umbrales y Acumulaciones Notariales
            </div>
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