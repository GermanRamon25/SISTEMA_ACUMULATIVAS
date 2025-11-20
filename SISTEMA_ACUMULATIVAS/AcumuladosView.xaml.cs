using LiveCharts;
using LiveCharts.Wpf;
using SISTEMA_ACUMULATIVAS.Conexion;
using SISTEMA_ACUMULATIVAS.Models;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace SISTEMA_ACUMULATIVAS.Views
{
    public partial class AcumuladosView : UserControl
    {
        private const decimal VALOR_UMA = 113.14m;
        private const int UMBRAL_IDENTIFICACION = 8000;

        private ClsConexion _conexion;

        // Propiedades Gráfico 1 (Columnas - Montos)
        public SeriesCollection SeriesClientes { get; set; }
        public string[] LabelsClientes { get; set; }
        public Func<double, string> Formatter { get; set; }

        // Propiedades Gráfico 2 (Filas - Tipos de Operación)
        public SeriesCollection SeriesTipoOperacion { get; set; }
        public string[] LabelsTipos { get; set; } // NUEVO: Etiquetas para el eje Y
        public Func<double, string> FormatterCantidad { get; set; } // NUEVO: Formato entero

        public AcumuladosView()
        {
            InitializeComponent();
            _conexion = new ClsConexion();
            Formatter = value => value.ToString("C0"); // Moneda
            FormatterCantidad = value => value.ToString("N0"); // Números enteros
            DataContext = this;
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            CargarDashboard();
        }

        private void btnActualizar_Click(object sender, RoutedEventArgs e)
        {
            CargarDashboard();
        }

        private void CargarDashboard()
        {
            CargarTopClientesYAlertas();
            CargarGraficoOperaciones(null);
        }

        // --- 1. LÓGICA TABLA Y BARRAS (CLIENTES) ---
        private void CargarTopClientesYAlertas()
        {
            List<Acumulado> listaAcumulados = new List<Acumulado>();
            decimal montoUmbral = VALOR_UMA * UMBRAL_IDENTIFICACION;

            try
            {
                using (SqlConnection conn = _conexion.GetConnection())
                {
                    string query = @"
                        SELECT TOP 20 c.Id, c.Nombre, a.TotalAcumulado, a.UltimaActualizacion
                        FROM Acumulados a
                        INNER JOIN Clientes c ON a.ClienteId = c.Id
                        WHERE a.TotalAcumulado > 0
                        ORDER BY a.TotalAcumulado DESC";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                decimal total = (decimal)reader["TotalAcumulado"];
                                double porcentaje = (double)(total / montoUmbral);
                                string estado = total >= montoUmbral ? "⚠️ UMBRAL ALCANZADO" : (porcentaje > 0.8 ? "Cerca del Límite" : "Normal");

                                listaAcumulados.Add(new Acumulado
                                {
                                    ClienteId = (int)reader["Id"],
                                    ClienteNombre = reader["Nombre"].ToString(),
                                    MontoAcumulado = total,
                                    UltimaActualizacion = (DateTime)reader["UltimaActualizacion"],
                                    PorcentajeUmbral = porcentaje,
                                    EstadoAlerta = estado
                                });
                            }
                        }
                    }
                }

                dgAlertas.ItemsSource = listaAcumulados;

                var top10 = listaAcumulados.Take(10).ToList();
                SeriesClientes = new SeriesCollection
                {
                    new ColumnSeries
                    {
                        Title = "Monto",
                        Values = new ChartValues<decimal>(top10.Select(x => x.MontoAcumulado)),
                        DataLabels = true,
                        LabelPoint = point => point.Y.ToString("C0"),
                        Fill = System.Windows.Media.Brushes.DodgerBlue // Color profesional
                    }
                };
                LabelsClientes = top10.Select(x => x.ClienteNombre).ToArray();

                DataContext = null;
                DataContext = this;
            }
            catch (Exception ex) { MessageBox.Show("Error carga: " + ex.Message); }
        }

        // --- 2. LÓGICA FILTRO ---
        private void dgAlertas_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dgAlertas.SelectedItem is Acumulado seleccionado)
            {
                CargarGraficoOperaciones(seleccionado.ClienteId, seleccionado.ClienteNombre);
            }
        }

        private void btnVerGlobal_Click(object sender, RoutedEventArgs e)
        {
            dgAlertas.SelectedItem = null;
            CargarGraficoOperaciones(null);
        }

        // --- 3. LÓGICA GRÁFICO TIPOS (AHORA BARRAS HORIZONTALES) ---
        private void CargarGraficoOperaciones(int? clienteId, string nombreCliente = "")
        {
            SeriesTipoOperacion = new SeriesCollection();
            List<string> etiquetas = new List<string>();

            try
            {
                using (SqlConnection conn = _conexion.GetConnection())
                {
                    string query = "";
                    SqlCommand cmd = new SqlCommand();
                    cmd.Connection = conn;

                    if (clienteId.HasValue)
                    {
                        query = @"SELECT TipoOperacion, COUNT(*) as Cantidad FROM Operaciones WHERE ClienteId = @Id AND FechaOperacion >= DATEADD(MONTH, -6, GETDATE()) GROUP BY TipoOperacion ORDER BY Cantidad ASC"; // Orden ASC para que se vea de arriba a abajo en el gráfico
                        cmd.Parameters.AddWithValue("@Id", clienteId.Value);
                        txtTituloPastel.Text = $"Operaciones de: {nombreCliente}";
                        btnVerGlobal.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        query = @"SELECT TipoOperacion, COUNT(*) as Cantidad FROM Operaciones WHERE FechaOperacion >= DATEADD(MONTH, -6, GETDATE()) GROUP BY TipoOperacion ORDER BY Cantidad ASC";
                        txtTituloPastel.Text = "Operaciones por Tipo (Global)";
                        btnVerGlobal.Visibility = Visibility.Collapsed;
                    }

                    cmd.CommandText = query;

                    // Usamos una sola serie para todas las barras (mismo color)
                    var valores = new ChartValues<int>();

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string tipo = reader["TipoOperacion"].ToString();
                            // Recortar texto si es exageradamente largo (opcional)
                            if (tipo.Length > 35) tipo = tipo.Substring(0, 35) + "...";

                            etiquetas.Add(tipo);
                            valores.Add((int)reader["Cantidad"]);
                        }
                    }

                    // Configurar el gráfico de Filas
                    SeriesTipoOperacion.Add(new RowSeries
                    {
                        Title = "Cantidad",
                        Values = valores,
                        DataLabels = true,
                        LabelPoint = point => point.X.ToString("N0"), // Mostrar número entero
                        Fill = (SolidColorBrush)new BrushConverter().ConvertFrom("#FF28A745"), // Verde profesional
                        RowPadding = 10
                    });

                    LabelsTipos = etiquetas.ToArray();
                }

                DataContext = null;
                DataContext = this;
            }
            catch { }
        }
    }
}