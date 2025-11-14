// Archivo: AcumuladosView.xaml.cs
// REEMPLAZA TODO EL ARCHIVO CON ESTO

using System;
using System.Windows.Controls;
// Necesitaremos estos para los gráficos
using LiveCharts;
using LiveCharts.Wpf;

namespace SISTEMA_ACUMULATIVAS.Views
{
    public partial class AcumuladosView : UserControl
    {
        // --- Propiedades para los Gráficos (Frontend) ---

        // Gráfico de Barras (Clientes)
        public SeriesCollection SeriesClientes { get; set; }
        public string[] LabelsClientes { get; set; }

        // Gráfico de Pastel (Tipos)
        public SeriesCollection SeriesTipoOperacion { get; set; }


        public AcumuladosView()
        {
            InitializeComponent();

            // --- Datos de Ejemplo (Frontend) ---
            // (Borraremos esto cuando conectemos la BD)

            // Ejemplo Gráfico de Barras
            SeriesClientes = new SeriesCollection
            {
                new ColumnSeries
                {
                    Title = "Acumulado",
                    Values = new ChartValues<double> { 1200000, 950000, 800000, 650000, 400000 }
                }
            };
            LabelsClientes = new[] { "Cliente A", "Cliente B", "Cliente C", "Cliente D", "Cliente E" };

            // Ejemplo Gráfico de Pastel
            SeriesTipoOperacion = new SeriesCollection
            {
                new PieSeries { Title = "Inmuebles", Values = new ChartValues<double> { 45 }, DataLabels = true },
                new PieSeries { Title = "Poderes", Values = new ChartValues<double> { 25 }, DataLabels = true },
                new PieSeries { Title = "Sociedades", Values = new ChartValues<double> { 20 }, DataLabels = true },
                new PieSeries { Title = "Mutuos", Values = new ChartValues<double> { 10 }, DataLabels = true }
            };

            // Enlazar los datos al contexto de la vista
            DataContext = this;
        }
    }
}