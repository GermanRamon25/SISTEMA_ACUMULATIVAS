using System;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using ExcelDataReader;
using Microsoft.Win32;
using SISTEMA_ACUMULATIVAS.Conexion;

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

        // 3. INSERCIÓN MASIVA EN SQL SERVER CON VALIDACIÓN UNIVERSAL Y FECHA HISTÓRICA
        private void BtnGuardar_Click(object sender, RoutedEventArgs e)
        {
            if (dtClientes == null || dtClientes.Rows.Count == 0) return;

            int insertados = 0;
            int omitidos = 0;
            ClsConexion conexionService = new ClsConexion();

            // Expresiones regulares oficiales de México
            Regex regexRfc = new Regex(@"^[A-ZÑ&]{3,4}\d{6}[A-Z0-9]{3}$", RegexOptions.Compiled);
            Regex regexCurp = new Regex(@"^[A-Z]{4}\d{6}[HM][A-Z]{5}[A-Z0-9]\d$", RegexOptions.Compiled);

            try
            {
                using (SqlConnection con = conexionService.GetConnection())
                {
                    // Si @FechaRegistro es nulo, SQL Server usa GETDATE() por defecto
                    string query = @"
                IF NOT EXISTS (SELECT 1 FROM Clientes WHERE RFC = @RFC)
                BEGIN
                    INSERT INTO Clientes (Nombre, RFC, CURP, TipoPersona, FechaRegistro, Activo)
                    VALUES (@Nombre, @RFC, @CURP, @TipoPersona, ISNULL(@FechaRegistro, GETDATE()), 1);
                END";

                    foreach (DataRow fila in dtClientes.Rows)
                    {
                        string rfcEncontrado = null;
                        string curpEncontrado = null;
                        string tipoPersonaEncontrado = null;
                        string nombreCandidato = null;
                        DateTime? fechaRegistroEncontrada = null;
                        int mayorLongitudTexto = 0;

                        // 1. Escanear todas las celdas de la fila dinámicamente
                        for (int i = 0; i < dtClientes.Columns.Count; i++)
                        {
                            string valor = fila[i]?.ToString()?.Trim();
                            if (string.IsNullOrWhiteSpace(valor)) continue;

                            string valorLimpio = valor.ToUpper().Replace(" ", "").Replace("-", "");

                            // ¿Es Fecha de Registro histórica? (ej: 15/03/2024, 2024-05-10)
                            if (DateTime.TryParse(valor, out DateTime fechaParsed))
                            {
                                if (fechaParsed.Year >= 1990 && fechaParsed <= DateTime.Now)
                                {
                                    fechaRegistroEncontrada = fechaParsed;
                                    continue;
                                }
                            }

                            // ¿Es CURP? (18 caracteres)
                            if (valorLimpio.Length == 18 && regexCurp.IsMatch(valorLimpio))
                            {
                                curpEncontrado = valorLimpio;
                                continue;
                            }

                            // ¿Es RFC? (12 caracteres Moral o 13 Física)
                            if ((valorLimpio.Length == 12 || valorLimpio.Length == 13) && regexRfc.IsMatch(valorLimpio))
                            {
                                rfcEncontrado = valorLimpio;
                                continue;
                            }

                            // ¿Es Tipo de Persona explícito?
                            if (valorLimpio == "F" || valorLimpio == "M" ||
                                valorLimpio.StartsWith("FISICA") || valorLimpio.StartsWith("FÍSICA") ||
                                valorLimpio.StartsWith("MORAL"))
                            {
                                tipoPersonaEncontrado = valorLimpio.StartsWith("M") ? "M" : "F";
                                continue;
                            }

                            // Si no es Fecha, RFC, CURP ni Tipo, y no es número (#), evaluamos si es el Nombre
                            if (!int.TryParse(valor, out _) && valor.Length > mayorLongitudTexto)
                            {
                                if (!valor.ToUpper().Contains("PADRÓN") &&
                                    !valor.ToUpper().Contains("LISTADO") &&
                                    !valor.ToUpper().Contains("REGISTRO DE CONTRIBUYENTES") &&
                                    !valor.ToUpper().Contains("NOMBRE / RAZÓN"))
                                {
                                    nombreCandidato = valor;
                                    mayorLongitudTexto = valor.Length;
                                }
                            }
                        }

                        // 2. Si no contiene RFC válido y Nombre, se omite la fila
                        if (string.IsNullOrWhiteSpace(rfcEncontrado) || string.IsNullOrWhiteSpace(nombreCandidato))
                        {
                            omitidos++;
                            continue;
                        }

                        // 3. Determinar tipo de persona si no venía en el archivo
                        if (string.IsNullOrWhiteSpace(tipoPersonaEncontrado))
                        {
                            tipoPersonaEncontrado = (rfcEncontrado.Length == 12) ? "M" : "F";
                        }

                        // 4. Inserción con parámetro de fecha
                        using (SqlCommand cmd = new SqlCommand(query, con))
                        {
                            cmd.Parameters.AddWithValue("@Nombre", nombreCandidato);
                            cmd.Parameters.AddWithValue("@RFC", rfcEncontrado);
                            cmd.Parameters.AddWithValue("@CURP", string.IsNullOrWhiteSpace(curpEncontrado) ? (object)DBNull.Value : curpEncontrado);
                            cmd.Parameters.AddWithValue("@TipoPersona", tipoPersonaEncontrado);
                            cmd.Parameters.AddWithValue("@FechaRegistro", fechaRegistroEncontrada.HasValue ? (object)fechaRegistroEncontrada.Value : DBNull.Value);

                            int filasAfectadas = cmd.ExecuteNonQuery();
                            if (filasAfectadas > 0)
                            {
                                insertados++;
                            }
                            else
                            {
                                omitidos++;
                            }
                        }
                    }
                }

                MessageBox.Show($"Importación finalizada:\n\n• Nuevos Clientes Registrados: {insertados}\n• Omitidos (Ya registrados o encabezados): {omitidos}",
                                "Proceso Completado", MessageBoxButton.OK, MessageBoxImage.Information);

                this.DialogResult = true;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al guardar en la base de datos: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnCancelar_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}