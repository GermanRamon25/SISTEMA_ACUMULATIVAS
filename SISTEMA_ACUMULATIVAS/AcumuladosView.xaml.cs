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

namespace SISTEMA_ACUMULATIVAS.Views
{
    public partial class AcumuladosView : UserControl
    {
        private const decimal VALOR_UMA = 113.14m;
        private const int UMBRAL_IDENTIFICACION = 8000;

        private ClsConexion _conexion;

        public SeriesCollection SeriesClientes { get; set; }
        public string[] LabelsClientes { get; set; }
        public SeriesCollection SeriesTipoOperacion { get; set; }
        public Func<double, string> Formatter { get; set; }

        public AcumuladosView()
        {
            InitializeComponent();
            _conexion = new ClsConexion();
            Formatter = value => value.ToString("C");
            DataContext = this;

            // NOTA: Ya no cargamos datos aquí en el constructor.
            // Esperamos al evento 'Loaded' para que se refresque siempre.
        }

        // ESTE ES EL EVENTO MÁGICO
        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            // Se ejecuta cada vez que haces clic en la pestaña
            CargarDashboard();
        }

        // EVENTO DEL BOTÓN MANUAL
        private void btnActualizar_Click(object sender, RoutedEventArgs e)
        {
            CargarDashboard();
            MessageBox.Show("Datos actualizados correctamente.", "Refresco", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void CargarDashboard()
        {
            CargarTopClientesYAlertas();
            CargarGraficoPastel();
        }

        private void CargarTopClientesYAlertas()
        {
            List<Acumulado> listaAcumulados = new List<Acumulado>();
            decimal montoUmbral = VALOR_UMA * UMBRAL_IDENTIFICACION;

            try
            {
                using (SqlConnection conn = _conexion.GetConnection())
                {
                    // Join para traer nombre del cliente
                    string query = @"
                        SELECT TOP 20 
                            c.Id, c.Nombre, a.TotalAcumulado, a.UltimaActualizacion
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
                                string estado = "Normal";

                                if (total >= montoUmbral) estado = "⚠️ UMBRAL ALCANZADO";
                                else if (porcentaje > 0.8) estado = "Cerca del Límite";

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

                // Refrescar Gráfico de Barras
                var top10 = listaAcumulados.Take(10).ToList();
                SeriesClientes = new SeriesCollection
                {
                    new ColumnSeries
                    {
                        Title = "Monto Acumulado",
                        Values = new ChartValues<decimal>(top10.Select(x => x.MontoAcumulado)),
                        DataLabels = true,
                        LabelPoint = point => point.Y.ToString("C0")
                    }
                };
                LabelsClientes = top10.Select(x => x.ClienteNombre).ToArray();

                // Forzar a la UI a notar el cambio de propiedades
                DataContext = null;
                DataContext = this;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al cargar acumulados: " + ex.Message);
            }
        }

        private void CargarGraficoPastel()
        {
            SeriesTipoOperacion = new SeriesCollection();
            try
            {
                using (SqlConnection conn = _conexion.GetConnection())
                {
                    string query = @"
                        SELECT TipoOperacion, COUNT(*) as Cantidad 
                        FROM Operaciones 
                        WHERE FechaOperacion >= DATEADD(MONTH, -6, GETDATE())
                        GROUP BY TipoOperacion";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                string tipo = reader["TipoOperacion"].ToString();
                                int cantidad = (int)reader["Cantidad"];
                                string nombreCorto = tipo.Length > 15 ? tipo.Substring(0, 15) + "..." : tipo;

                                SeriesTipoOperacion.Add(new PieSeries
                                {
                                    Title = nombreCorto,
                                    Values = new ChartValues<int> { cantidad },
                                    DataLabels = true,
                                    LabelPoint = chartPoint => string.Format("{0} ({1:P})", chartPoint.SeriesView.Title, chartPoint.Participation)
                                });
                            }
                        }
                    }
                }
            }
            catch { }
        }
    }
}