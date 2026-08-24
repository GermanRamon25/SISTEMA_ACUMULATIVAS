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
        private const int UMBRAL_IDENTIFICACION = 8000;
        private ClsConexion _conexion;

        // --- MODELOS INTERNOS PARA LAS GRÁFICAS MANUALES ESTILO CORPORATIVO ---
        public class ClienteGraficaItem
        {
            public string NombreCliente { get; set; }
            public decimal Monto { get; set; }
            public double AnchoBarraVirtual { get; set; }
            public Brush ColorBarra { get; set; }
            public Brush ColorTextoMonto { get; set; }
        }

        public class OperacionGraficaItem
        {
            public string TipoOperacion { get; set; }
            public int Cantidad { get; set; }
            public double AnchoBarraVirtual { get; set; }
            public Brush ColorBarra { get; set; }
        }

        public List<ClienteGraficaItem> ListaGraficaClientes { get; set; }
        public List<OperacionGraficaItem> ListaGraficaOperaciones { get; set; }

        public AcumuladosView()
        {
            InitializeComponent();
            _conexion = new ClsConexion();
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

        // --- 1. LÓGICA TABLA Y GRÁFICA DE CLIENTES (NUEVA REGLA DE NEGOCIO Y FECHA REAL) ---
        private void CargarTopClientesYAlertas()
        {
            List<Acumulado> listaAcumulados = new List<Acumulado>();
            ListaGraficaClientes = new List<ClienteGraficaItem>();

            ClsConfiguracion config = new ClsConfiguracion();
            decimal valorUmaActual = config.ObtenerUMA();
            if (valorUmaActual == 0) valorUmaActual = 113.14m;

            decimal montoUmbral = valorUmaActual * UMBRAL_IDENTIFICACION;

            if (lblInfoUma != null)
                lblInfoUma.Text = $"Umbrales (UMA Actual: {valorUmaActual:C})";

            if (lblMontoLimite != null)
                lblMontoLimite.Text = $"8,000 UMAs ({montoUmbral:C})";

            try
            {
                int clientesEnAlerta = 0;

                using (SqlConnection conn = _conexion.GetConnection())
                {
                    // --- CAMBIO APLICADO AQUÍ: Buscamos la MAX(FechaOperacion) real ---
                    string query = @"
                        SELECT 
                            c.Id, 
                            c.Nombre, 
                            a.TotalAcumulado, 
                            ISNULL((SELECT MAX(FechaOperacion) FROM Operaciones WHERE ClienteId = c.Id), a.UltimaActualizacion) AS UltimaActividadReal
                        FROM Acumulados a
                        INNER JOIN Clientes c ON a.ClienteId = c.Id
                        WHERE a.TotalAcumulado > 0";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                decimal total = (decimal)reader["TotalAcumulado"];
                                double porcentaje = (double)(total / montoUmbral);
                                string estado = total >= montoUmbral ? "⚠️ UMBRAL ALCANZADO" : (porcentaje > 0.8 ? "Cerca del Límite" : "Normal");

                                if (total >= montoUmbral) clientesEnAlerta++;

                                listaAcumulados.Add(new Acumulado
                                {
                                    ClienteId = (int)reader["Id"],
                                    ClienteNombre = reader["Nombre"].ToString(),
                                    MontoAcumulado = total,
                                    // --- CAMBIO APLICADO AQUÍ: Leemos 'UltimaActividadReal' ---
                                    UltimaActualizacion = (DateTime)reader["UltimaActividadReal"],
                                    PorcentajeUmbral = porcentaje,
                                    EstadoAlerta = estado
                                });
                            }
                        }
                    }
                }

                lblTotalAlertas.Text = $"{clientesEnAlerta} Clientes";

                // Ordenar toda la lista para la tabla (de mayor a menor)
                listaAcumulados = listaAcumulados.OrderByDescending(x => x.MontoAcumulado).ToList();
                dgAlertas.ItemsSource = listaAcumulados;

                // --- NUEVA REGLA PARA LA GRÁFICA: TOP 10 + LOS QUE EXCEDAN EL UMBRAL ---
                var top10 = listaAcumulados.Take(10).ToList();
                var excedenUmbral = listaAcumulados.Where(x => x.MontoAcumulado >= montoUmbral).ToList();

                // Combinamos ambas listas, quitamos duplicados y ordenamos
                var clientesParaGrafica = top10.Union(excedenUmbral)
                                               .OrderByDescending(x => x.MontoAcumulado)
                                               .ToList();

                // Determinamos el monto mayor para que ocupe el 100% de la barra
                decimal maxMonto = clientesParaGrafica.Any() ? clientesParaGrafica.Max(x => x.MontoAcumulado) : 1;
                if (maxMonto == 0) maxMonto = 1;

                double maxAnchoPixeles = 420; // Ajustado para que las barras tengan buen tamaño

                foreach (var cliente in clientesParaGrafica)
                {
                    bool supero = cliente.MontoAcumulado >= montoUmbral;

                    ListaGraficaClientes.Add(new ClienteGraficaItem
                    {
                        NombreCliente = cliente.ClienteNombre,
                        Monto = cliente.MontoAcumulado,
                        AnchoBarraVirtual = (double)(cliente.MontoAcumulado / maxMonto) * maxAnchoPixeles,
                        // Asignamos degradados premium
                        ColorBarra = supero ? ObtenerDegradadoRojo() : ObtenerDegradadoAzul(),
                        ColorTextoMonto = supero ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#B91C1C"))
                                                 : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0F172A"))
                    });
                }

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

        // --- 3. LÓGICA GRÁFICA TIPOS (BARRAS CORPORATIVAS) ---
        private void CargarGraficoOperaciones(int? clienteId, string nombreCliente = "")
        {
            ListaGraficaOperaciones = new List<OperacionGraficaItem>();
            var tempList = new List<OperacionGraficaItem>();

            try
            {
                using (SqlConnection conn = _conexion.GetConnection())
                {
                    string query = "";
                    SqlCommand cmd = new SqlCommand();
                    cmd.Connection = conn;

                    if (clienteId.HasValue)
                    {
                        query = @"SELECT TipoOperacion, COUNT(*) as Cantidad FROM Operaciones WHERE ClienteId = @Id AND FechaOperacion >= DATEADD(MONTH, -6, GETDATE()) GROUP BY TipoOperacion ORDER BY Cantidad DESC";
                        cmd.Parameters.AddWithValue("@Id", clienteId.Value);
                        txtTituloPastel.Text = $"Operaciones de: {nombreCliente}";
                        btnVerGlobal.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        query = @"SELECT TipoOperacion, COUNT(*) as Cantidad FROM Operaciones WHERE FechaOperacion >= DATEADD(MONTH, -6, GETDATE()) GROUP BY TipoOperacion ORDER BY Cantidad DESC";
                        txtTituloPastel.Text = "Distribución Global de Actividades";
                        btnVerGlobal.Visibility = Visibility.Collapsed;
                    }

                    cmd.CommandText = query;

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            tempList.Add(new OperacionGraficaItem
                            {
                                TipoOperacion = reader["TipoOperacion"].ToString(),
                                Cantidad = (int)reader["Cantidad"]
                            });
                        }
                    }
                }

                int maxCantidad = tempList.Any() ? tempList.Max(x => x.Cantidad) : 1;
                if (maxCantidad == 0) maxCantidad = 1;

                double maxAnchoPixeles = 350;
                Brush colorSecundario = ObtenerDegradadoPurpura();

                foreach (var op in tempList)
                {
                    op.AnchoBarraVirtual = ((double)op.Cantidad / maxCantidad) * maxAnchoPixeles;
                    op.ColorBarra = colorSecundario; // Un color distinto para separar métricas
                    ListaGraficaOperaciones.Add(op);
                }

                DataContext = null;
                DataContext = this;
            }
            catch { }
        }

        // --- GENERADORES DE DEGRADADOS PARA EL DISEÑO CORPORATIVO ---
        private LinearGradientBrush ObtenerDegradadoRojo()
        {
            LinearGradientBrush gradient = new LinearGradientBrush();
            gradient.StartPoint = new Point(0, 0);
            gradient.EndPoint = new Point(1, 0);
            gradient.GradientStops.Add(new GradientStop((Color)ColorConverter.ConvertFromString("#F87171"), 0.0));
            gradient.GradientStops.Add(new GradientStop((Color)ColorConverter.ConvertFromString("#DC2626"), 1.0));
            return gradient;
        }

        private LinearGradientBrush ObtenerDegradadoAzul()
        {
            LinearGradientBrush gradient = new LinearGradientBrush();
            gradient.StartPoint = new Point(0, 0);
            gradient.EndPoint = new Point(1, 0);
            gradient.GradientStops.Add(new GradientStop((Color)ColorConverter.ConvertFromString("#38BDF8"), 0.0));
            gradient.GradientStops.Add(new GradientStop((Color)ColorConverter.ConvertFromString("#0284C7"), 1.0));
            return gradient;
        }

        private LinearGradientBrush ObtenerDegradadoPurpura()
        {
            LinearGradientBrush gradient = new LinearGradientBrush();
            gradient.StartPoint = new Point(0, 0);
            gradient.EndPoint = new Point(1, 0);
            gradient.GradientStops.Add(new GradientStop((Color)ColorConverter.ConvertFromString("#818CF8"), 0.0));
            gradient.GradientStops.Add(new GradientStop((Color)ColorConverter.ConvertFromString("#4F46E5"), 1.0));
            return gradient;
        }
    }
}