using ExcelDataReader;
using Microsoft.Win32;
using SISTEMA_ACUMULATIVAS.Conexion;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;

namespace SISTEMA_ACUMULATIVAS
{
    public partial class ImportarClientesWindow : Window
    {
        private DataTable dtClientes;

        public ImportarClientesWindow()
        {
            InitializeComponent();
        }

        // 1. DESCARGAR PLANTILLA CSV (Sin dependencias externas)
        private void BtnDescargarPlantilla_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                Filter = "Archivo CSV delimitado por comas (*.csv)|*.csv",
                FileName = "Plantilla_Importacion_Clientes.csv",
                Title = "Guardar Plantilla de Importación"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {
                    using (var writer = new StreamWriter(saveFileDialog.FileName, false, new UTF8Encoding(true)))
                    {
                        writer.WriteLine("Nombre,RFC,CURP,TipoPersona");
                        writer.WriteLine("JUAN PEREZ LOPEZ,PELJ850101XYZ,PELJ850101HDFRRN01,F");
                        writer.WriteLine("CONSTRUCTORA DEL NORTE SA DE CV,CNO120304ABC,,M");
                    }

                    MessageBox.Show("Plantilla CSV generada correctamente. Puedes completarla directamente en Excel.",
                                    "Plantilla Generada", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error al exportar plantilla: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        // 2. SELECCIONAR Y LEER ARCHIVO (.XLSX, .XLS, .CSV)
        private void BtnSeleccionarArchivo_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "Archivos compatibles (*.xlsx;*.xls;*.csv)|*.xlsx;*.xls;*.csv|Archivos de Excel (*.xlsx;*.xls)|*.xlsx;*.xls|Archivos CSV (*.csv)|*.csv",
                Title = "Seleccionar Archivo de Clientes"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    string rutaArchivo = openFileDialog.FileName;
                    TxtRutaArchivo.Text = rutaArchivo;
                    string extension = Path.GetExtension(rutaArchivo).ToLower();

                    if (extension == ".csv")
                    {
                        dtClientes = LeerArchivoCSV(rutaArchivo);
                    }
                    else // .xlsx o .xls
                    {
                        dtClientes = LeerArchivoExcel(rutaArchivo);
                    }

                    if (dtClientes != null && dtClientes.Rows.Count > 0)
                    {
                        DgVistaPrevia.ItemsSource = dtClientes.DefaultView;
                        TxtConteo.Text = $"Registros leídos: {dtClientes.Rows.Count}";
                        BtnGuardar.IsEnabled = true;
                    }
                    else
                    {
                        MessageBox.Show("El archivo seleccionado no contiene filas válidas.", "Archivo Vacío", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error al procesar el archivo: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        // LECTURA DE EXCEL CON EXCELDATAREADER
        private DataTable LeerArchivoExcel(string ruta)
        {
            using (var stream = File.Open(ruta, FileMode.Open, FileAccess.Read))
            {
                using (var reader = ExcelReaderFactory.CreateReader(stream))
                {
                    var conf = new ExcelDataSetConfiguration
                    {
                        ConfigureDataTable = _ => new ExcelDataTableConfiguration
                        {
                            UseHeaderRow = true
                        }
                    };

                    var result = reader.AsDataSet(conf);
                    return result.Tables.Count > 0 ? result.Tables[0] : null;
                }
            }
        }

        // LECTURA NATIVA DE ARCHIVOS CSV
        private DataTable LeerArchivoCSV(string ruta)
        {
            DataTable dt = new DataTable();

            using (var reader = new StreamReader(ruta, Encoding.UTF8))
            {
                string lineaEncabezado = reader.ReadLine();
                if (string.IsNullOrWhiteSpace(lineaEncabezado)) return dt;

                string[] encabezados = lineaEncabezado.Split(',');
                foreach (string col in encabezados)
                {
                    dt.Columns.Add(col.Trim());
                }

                while (!reader.EndOfStream)
                {
                    string linea = reader.ReadLine();
                    if (!string.IsNullOrWhiteSpace(linea))
                    {
                        string[] valores = linea.Split(',');
                        dt.Rows.Add(valores);
                    }
                }
            }

            return dt;
        }
        private void BtnGuardar_Click(object sender, RoutedEventArgs e)
        {
            if (dtClientes == null || dtClientes.Rows.Count == 0) return;

            int clientesNuevos = 0;
            int operacionesRegistradas = 0;
            int filasOmitidas = 0;

            ClsConexion conexionService = new ClsConexion();

            try
            {
                using (SqlConnection con = conexionService.GetConnection())
                {
                    if (con.State != ConnectionState.Open) con.Open();

                    // 1. Cargar catálogo de clientes existentes de este usuario en memoria (Nombre -> Id)
                    Dictionary<string, int> mapaClientes = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                    string qClientes = "SELECT Id, Nombre FROM Clientes WHERE UsuarioId = @UsuarioId AND Activo = 1";
                    using (SqlCommand cmdCli = new SqlCommand(qClientes, con))
                    {
                        cmdCli.Parameters.AddWithValue("@UsuarioId", ClsSesion.UsuarioId);
                        using (SqlDataReader rdr = cmdCli.ExecuteReader())
                        {
                            while (rdr.Read())
                            {
                                string nom = rdr["Nombre"].ToString().Trim();
                                if (!mapaClientes.ContainsKey(nom))
                                {
                                    mapaClientes.Add(nom, (int)rdr["Id"]);
                                }
                            }
                        }
                    }

                    // 2. Mapear nombres de columnas del Excel (soporta espacios o variaciones de encabezado)
                    string colOtorgante = null;
                    string colOperacion = null;
                    string colEscritura = null;
                    string colFecha = null;
                    string colDescripcion = null;

                    foreach (DataColumn col in dtClientes.Columns)
                    {
                        string nombreCol = col.ColumnName.Trim().ToUpper();
                        if (nombreCol == "OTORGANTE" || nombreCol == "CLIENTE") colOtorgante = col.ColumnName;
                        else if (nombreCol.StartsWith("OPERACIÓN") || nombreCol.StartsWith("OPERACION")) colOperacion = col.ColumnName;
                        else if (nombreCol.StartsWith("NO. ESCRITURA") || nombreCol.StartsWith("ESCRITURA")) colEscritura = col.ColumnName;
                        else if (nombreCol.StartsWith("FECHA")) colFecha = col.ColumnName;
                        else if (nombreCol.StartsWith("VOLUMEN") || nombreCol.StartsWith("FOLIO")) colDescripcion = col.ColumnName;
                    }

                    if (colOtorgante == null)
                    {
                        MessageBox.Show("No se encontró la columna OTORGANTE en el archivo.", "Formato Inválido", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    // Consultas de inserción
                    string sqlInsertCliente = @"
                INSERT INTO Clientes (Nombre, RFC, CURP, TipoPersona, UsuarioId, FechaRegistro, Activo)
                VALUES (@Nombre, 'PENDIENTE', NULL, @TipoPersona, @UsuarioId, GETDATE(), 1);
                SELECT SCOPE_IDENTITY();";

                    string sqlInsertOperacion = @"
                INSERT INTO Operaciones (ClienteId, TipoOperacion, Monto, FechaOperacion, FolioEscritura, Descripcion, UsuarioId)
                VALUES (@ClienteId, @TipoOperacion, @Monto, @FechaOperacion, @FolioEscritura, @Descripcion, @UsuarioId);";

                    // 3. Procesar cada escritura del índice
                    foreach (DataRow fila in dtClientes.Rows)
                    {
                        string otorgante = fila[colOtorgante]?.ToString()?.Trim();
                        if (string.IsNullOrWhiteSpace(otorgante) || otorgante.Length < 3 || otorgante.Equals("OTORGANTE", StringComparison.OrdinalIgnoreCase))
                        {
                            filasOmitidas++;
                            continue;
                        }

                        // A) Obtener o crear el ClienteId
                        int clienteId;
                        if (!mapaClientes.TryGetValue(otorgante, out clienteId))
                        {
                            string upper = otorgante.ToUpper();
                            bool esMoral = upper.Contains("S.A.") || upper.Contains("S. DE R.L.") || upper.Contains("SAPI") ||
                                           upper.Contains("SOCIEDAD") || upper.Contains("ASOCIACION") || upper.Contains("S.C.");

                            using (SqlCommand cmdInsCli = new SqlCommand(sqlInsertCliente, con))
                            {
                                cmdInsCli.Parameters.AddWithValue("@Nombre", otorgante);
                                cmdInsCli.Parameters.AddWithValue("@TipoPersona", esMoral ? "M" : "F");
                                cmdInsCli.Parameters.AddWithValue("@UsuarioId", ClsSesion.UsuarioId);

                                clienteId = Convert.ToInt32(cmdInsCli.ExecuteScalar());
                                mapaClientes.Add(otorgante, clienteId);
                                clientesNuevos++;
                            }
                        }

                        // B) Extraer datos del acto notarial
                        string operacion = colOperacion != null ? fila[colOperacion]?.ToString()?.Trim() : "ACTO NOTARIAL";
                        if (string.IsNullOrWhiteSpace(operacion)) operacion = "ACTO NOTARIAL";

                        string escritura = colEscritura != null ? fila[colEscritura]?.ToString()?.Trim() : "S/N";
                        if (double.TryParse(escritura, out double numEsc)) escritura = numEsc.ToString("0");

                        DateTime fechaOperacion = DateTime.Now;
                        if (colFecha != null && DateTime.TryParse(fila[colFecha]?.ToString(), out DateTime fParsed))
                        {
                            fechaOperacion = fParsed;
                        }

                        string descExtra = "";
                        if (dtClientes.Columns.Contains("VOLUMEN")) descExtra += "Vol: " + fila["VOLUMEN"]?.ToString()?.Trim() + " ";
                        if (dtClientes.Columns.Contains("LIBRO")) descExtra += "Libro: " + fila["LIBRO"]?.ToString()?.Trim() + " ";
                        if (dtClientes.Columns.Contains("FOLIO")) descExtra += "Folio: " + fila["FOLIO"]?.ToString()?.Trim();

                        // C) Registrar la operación
                        using (SqlCommand cmdInsOp = new SqlCommand(sqlInsertOperacion, con))
                        {
                            cmdInsOp.Parameters.AddWithValue("@ClienteId", clienteId);
                            cmdInsOp.Parameters.AddWithValue("@TipoOperacion", operacion);
                            cmdInsOp.Parameters.AddWithValue("@Monto", 0.00m); // Importe base para captura posterior
                            cmdInsOp.Parameters.AddWithValue("@FechaOperacion", fechaOperacion);
                            cmdInsOp.Parameters.AddWithValue("@FolioEscritura", string.IsNullOrWhiteSpace(escritura) ? "S/N" : escritura);
                            cmdInsOp.Parameters.AddWithValue("@Descripcion", descExtra.Trim());
                            cmdInsOp.Parameters.AddWithValue("@UsuarioId", ClsSesion.UsuarioId);

                            cmdInsOp.ExecuteNonQuery();
                            operacionesRegistradas++;
                        }
                    }
                }

                MessageBox.Show($"Importación finalizada con éxito:\n\n" +
                                $"• Nuevos Clientes Registrados: {clientesNuevos}\n" +
                                $"• Operaciones / Escrituras Insertadas: {operacionesRegistradas}\n" +
                                $"• Filas omitidas (vacías): {filasOmitidas}",
                                "Proceso Completado", MessageBoxButton.OK, MessageBoxImage.Information);

                this.DialogResult = true;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al registrar operaciones: " + ex.Message, "Error BD", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void BtnCancelar_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private bool EsPersonaMoral(string nombre)
        {
            if (string.IsNullOrWhiteSpace(nombre)) return false;

            // Normalizar: mayúsculas, sin comillas, sin puntos y sin comas
            string limpio = nombre.ToUpper()
                .Replace("\"", "")
                .Replace(".", "")
                .Replace(",", "")
                .Trim();

            // 1. Siglas y términos mercantiles/societarios más comunes en actas notariales
            string[] patronesMorales = new string[]
            {
        // Sociedades Anónimas y Bursátiles
        "SA DE CV", "S A DE C V", "SAPI DE CV", "S A P I", "SA PROMOTORA", "SAPI", "SAB DE CV",
        
        // Sociedades de Responsabilidad Limitada y Civiles
        "S DE RL DE CV", "S DE RL", "SRL DE CV", "SRL", "SC DE RL", "S C DE R L", "SC", "S C",
        
        // Sociedades de Producción Rural (comunes en Sinaloa/agrícolas)
        "SPR DE RL", "S DE PR DE RL", "SPR DE RI", "S DE PR DE RI", "SPR DE EL", "S DE PR DE EL", "SPR",
        
        // Asociaciones, Cooperativas y Fundaciones
        "AC", "A C", "IAP", "I A P", "SCL", "S C L",
        
        // Palabras completas inequívocas
        "SOCIEDAD", "ASOCIACION", "ASOCIACIÓN", "AGRÍCOLA", "AGRICOLA", "COOPERATIVA",
        "CONSTRUCTORA", "INMOBILIARIA", "PRODUCTORES", "EJIDO", "COMISARIADO",
        "MODULO DE RIEGO", "MÓDULO DE RIEGO", "CANALEROS", "MUNICIPIO", "GOBIERNO",
        "BANCO", "GRUPO FINANCIERO", "SOFOM", "UNION DE CREDITO", "UNIÓN DE CRÉDITO",
        "INSTITUTO", "COLEGIO", "FUNDACION", "FUNDACIÓN"
            };

            foreach (var patron in patronesMorales)
            {
                // Coincidencia exacta de palabra/sigla aislada o frase
                if (limpio.Contains(patron))
                {
                    return true;
                }
            }

            return false;
        }
    }
}   