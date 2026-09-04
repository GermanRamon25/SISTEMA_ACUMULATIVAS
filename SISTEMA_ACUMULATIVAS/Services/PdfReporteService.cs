using System;
using System.IO;
using System.Collections.Generic;
using SISTEMA_ACUMULATIVAS.Conexion;
using SISTEMA_ACUMULATIVAS.Models;

// iTextSharp
using iTextSharp.text;
using iTextSharp.text.pdf;
using iTextSharp.text.pdf.draw;

// Alias explícitos para compatibilidad con WPF
using PdfParagraph = iTextSharp.text.Paragraph;
using PdfRectangle = iTextSharp.text.Rectangle;

// Alias para acceder a la clase OperacionExpediente de la vista
using static SISTEMA_ACUMULATIVAS.Views.PanelControlView;

namespace SISTEMA_ACUMULATIVAS.Services
{
    public class PdfReporteService
    {
        public static void GenerarFichaUif(string rutaDestino, ReporteAvisoItem reporte)
        {
            // Hoja tamaño Carta con márgenes laterales de 36pt (0.5 pulgada)
            Document doc = new Document(PageSize.LETTER, 36f, 36f, 30f, 30f);

            using (FileStream fs = new FileStream(rutaDestino, FileMode.Create))
            {
                PdfWriter writer = PdfWriter.GetInstance(doc, fs);
                doc.Open();

                // ==========================================
                // 1. PALETA DE COLORES EXACTA
                // ==========================================
                BaseColor colorNotariaOscuro = new BaseColor(24, 43, 73);      // #182B49 (Azul muy oscuro)
                BaseColor colorAzulSubtitulo = new BaseColor(0, 122, 204);     // #007ACC (Azul celeste vivo)
                BaseColor colorGrisTexto = new BaseColor(108, 117, 125);       // #6C757D (Gris secundario)
                BaseColor colorBordeCuadricula = new BaseColor(222, 226, 230); // #DEE2E6 (Borde suave)

                // Caja Legal (Azul)
                BaseColor colorFondoLegal = new BaseColor(240, 247, 252);      // #F0F7FC
                BaseColor colorBarraLegal = new BaseColor(0, 122, 204);        // #007ACC

                // Datos Cliente
                BaseColor colorAzulMonto = new BaseColor(0, 102, 204);         // #0066CC
                BaseColor colorRojoCriterio = new BaseColor(204, 0, 0);        // #CC0000

                // Tabla Operaciones
                BaseColor colorHeaderTabla = new BaseColor(28, 45, 66);        // #1C2D42 (Azul noche)

                // Caja Disposición (Roja)
                BaseColor colorFondoAlerta = new BaseColor(253, 242, 242);     // #FDF2F2
                BaseColor colorBarraAlerta = new BaseColor(220, 53, 69);       // #DC3545
                BaseColor colorTextoAlerta = new BaseColor(140, 40, 40);

                // ==========================================
                // 2. TIPOGRAFÍAS
                // ==========================================
                Font fNotariaTitulo = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 15f, colorNotariaOscuro);
                Font fSubtituloDoc = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10.5f, colorAzulSubtitulo);
                Font fUbicacion = FontFactory.GetFont(FontFactory.HELVETICA, 8.5f, colorGrisTexto);

                Font fLegalTitulo = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 8.5f, colorNotariaOscuro);
                Font fLegalCuerpo = FontFactory.GetFont(FontFactory.HELVETICA, 8.2f, new BaseColor(60, 60, 60));

                Font fGridEtiqueta = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 9f, new BaseColor(70, 80, 95));
                Font fGridValor = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 9.5f, BaseColor.BLACK);
                Font fGridMonto = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 11f, colorAzulMonto);
                Font fGridCriterio = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 9.5f, colorRojoCriterio);

                Font fSeccionTitulo = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10f, colorNotariaOscuro);
                Font fHeaderTabla = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 9f, BaseColor.WHITE);
                Font fCeldaTabla = FontFactory.GetFont(FontFactory.HELVETICA, 8.5f, BaseColor.BLACK);
                Font fDetonante = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 8f, colorRojoCriterio);

                Font fAlertaTitulo = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 8.5f, colorBarraAlerta);
                Font fAlertaCuerpo = FontFactory.GetFont(FontFactory.HELVETICA, 8.2f, colorTextoAlerta);
                Font fAlertaBold = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 8.2f, colorTextoAlerta);

                Font fFirmaTitular = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 9.5f, BaseColor.BLACK);
                Font fFirmaSub = FontFactory.GetFont(FontFactory.HELVETICA, 8.5f, colorGrisTexto);
                Font fFooter = FontFactory.GetFont(FontFactory.HELVETICA, 7.5f, new BaseColor(120, 130, 140));

                // Datos Notaría
                string numNotaria = !string.IsNullOrWhiteSpace(ClsSesion.NumeroNotaria) ? ClsSesion.NumeroNotaria : "28";
                string titular = !string.IsNullOrWhiteSpace(ClsSesion.NombreTitular) ? ClsSesion.NombreTitular.ToUpper() : "TITULAR NO CONFIGURADO";
                string direccion = !string.IsNullOrWhiteSpace(ClsSesion.DireccionCompleta) ? ClsSesion.DireccionCompleta : "GUASAVE SINALOA 81077";

                // ==========================================
                // 3. ENCABEZADO
                // ==========================================
                PdfParagraph pNotaria = new PdfParagraph($"NOTARÍA PÚBLICA NO. {numNotaria}", fNotariaTitulo) { Alignment = Element.ALIGN_CENTER };
                PdfParagraph pSub = new PdfParagraph("Ficha Informativa de Operación Vulnerable y Acumulación", fSubtituloDoc) { Alignment = Element.ALIGN_CENTER };
                PdfParagraph pDir = new PdfParagraph($"{direccion} | Control Interno LFPIORPI", fUbicacion) { Alignment = Element.ALIGN_CENTER, SpacingAfter = 10f };

                doc.Add(pNotaria);
                doc.Add(pSub);
                doc.Add(pDir);

                // Línea divisoria superior
                LineSeparator lineaSuperior = new LineSeparator(1f, 100f, colorAzulSubtitulo, Element.ALIGN_CENTER, -2);
                doc.Add(new Chunk(lineaSuperior));
                doc.Add(new PdfParagraph(" ") { Font = FontFactory.GetFont(FontFactory.HELVETICA, 4f) }); // Espaciador

                // ==========================================
                // 4. CAJA DE FUNDAMENTO LEGAL (BARRA AZUL)
                // ==========================================
                PdfPTable tblLegal = new PdfPTable(1) { WidthPercentage = 100f, SpacingBefore = 8f, SpacingAfter = 12f };
                PdfPCell cellLegal = new PdfPCell
                {
                    BackgroundColor = colorFondoLegal,
                    Border = PdfRectangle.LEFT_BORDER,       // Solo borde izquierdo
                    BorderWidthLeft = 3.5f,
                    BorderColorLeft = colorBarraLegal,
                    Padding = 8f
                };

                PdfParagraph pFundamento = new PdfParagraph();
                pFundamento.Add(new Chunk("FUNDAMENTO LEGAL (LFPIORPI):\n", fLegalTitulo));
                pFundamento.Add(new Chunk("Conforme a lo dispuesto por el artículo 17, fracción XII, y artículo 18 de la Ley Federal para la Prevención e Identificación de Operaciones con Recursos de Procedencia Ilícita, así como los artículos 27 y 30 de sus Reglas de Carácter General, se emite la presente ficha técnica relativa al registro de actos, acumulación de montos y seguimiento de umbrales en Unidades de Medida y Actualización (UMA).", fLegalCuerpo));
                pFundamento.Alignment = Element.ALIGN_JUSTIFIED;

                cellLegal.AddElement(pFundamento);
                tblLegal.AddCell(cellLegal);
                doc.Add(tblLegal);

                // ==========================================
                // 5. TABLA DE DATOS DEL CLIENTE (CUADRÍCULA)
                // ==========================================
                PdfPTable tblCliente = new PdfPTable(2) { WidthPercentage = 100f, SpacingAfter = 14f };
                tblCliente.SetWidths(new float[] { 32f, 68f });

                // Fila 1: Cliente / Razón Social
                AgregarCeldaGrid(tblCliente, "Cliente / Razón Social:", fGridEtiqueta, colorBordeCuadricula);
                AgregarCeldaGrid(tblCliente, reporte.NombreCliente, fGridValor, colorBordeCuadricula);

                // Fila 2: RFC / CURP
                AgregarCeldaGrid(tblCliente, "RFC / CURP:", fGridEtiqueta, colorBordeCuadricula);
                AgregarCeldaGrid(tblCliente, $"{reporte.RFC} | {reporte.CURP}", fGridValor, colorBordeCuadricula);

                // Fila 3: Monto Total Acumulado
                AgregarCeldaGrid(tblCliente, "Monto Total Acumulado (6 Meses):", fGridEtiqueta, colorBordeCuadricula);
                AgregarCeldaGrid(tblCliente, reporte.MontoTotalAcumulado.ToString("C2"), fGridMonto, colorBordeCuadricula);

                // Fila 4: Motivo / Criterio
                AgregarCeldaGrid(tblCliente, "Motivo / Criterio del Aviso:", fGridEtiqueta, colorBordeCuadricula);
                AgregarCeldaGrid(tblCliente, reporte.MotivoAviso, fGridCriterio, colorBordeCuadricula);

                doc.Add(tblCliente);

                // ==========================================
                // 6. TABLA DESGLOSE DE OPERACIONES
                // ==========================================
                PdfParagraph pSeccion = new PdfParagraph("Desglose de Operaciones en el Periodo", fSeccionTitulo) { SpacingAfter = 6f };
                doc.Add(pSeccion);

                PdfPTable tblOperaciones = new PdfPTable(4) { WidthPercentage = 100f, SpacingAfter = 14f };
                tblOperaciones.SetWidths(new float[] { 15f, 12f, 50f, 23f });

                // Cabeceras oscuras
                string[] encabezados = { "Fecha", "No.Escritura", "Tipo de Operación Notarial", "Monto" };
                foreach (var h in encabezados)
                {
                    PdfPCell cellH = new PdfPCell(new Phrase(h, fHeaderTabla))
                    {
                        BackgroundColor = colorHeaderTabla,
                        Border = PdfRectangle.NO_BORDER,
                        PaddingTop = 6f,
                        PaddingBottom = 6f,
                        PaddingLeft = 5f,
                        PaddingRight = 5f,
                        HorizontalAlignment = (h == "Monto" ? Element.ALIGN_RIGHT : (h == "No.Escritura" ? Element.ALIGN_CENTER : Element.ALIGN_LEFT))
                    };
                    tblOperaciones.AddCell(cellH);
                }

                // Celdas de operaciones
                if (reporte.OperacionesDetalle != null && reporte.OperacionesDetalle.Count > 0)
                {
                    foreach (var op in reporte.OperacionesDetalle)
                    {
                        // Fecha
                        tblOperaciones.AddCell(CrearCeldaOperacion(op.FechaOperacion.ToString("dd/MM/yyyy"), fCeldaTabla, Element.ALIGN_LEFT, colorBordeCuadricula));

                        // Folio
                        tblOperaciones.AddCell(CrearCeldaOperacion(op.FolioEscritura, fCeldaTabla, Element.ALIGN_CENTER, colorBordeCuadricula));

                        // Tipo de Operación + Detonante
                        PdfPCell cellDesc = new PdfPCell
                        {
                            Border = PdfRectangle.BOTTOM_BORDER,
                            BorderColorBottom = colorBordeCuadricula,
                            PaddingTop = 5f,
                            PaddingBottom = 5f,
                            PaddingLeft = 5f
                        };
                        cellDesc.AddElement(new PdfParagraph(op.TipoOperacion, fCeldaTabla));
                        if (op.EsDetonante)
                        {
                            cellDesc.AddElement(new PdfParagraph($"[{op.EtiquetaDetonante}]", fDetonante));
                        }
                        tblOperaciones.AddCell(cellDesc);

                        // Monto en negrita
                        tblOperaciones.AddCell(CrearCeldaOperacion(op.Monto.ToString("C2"), fGridValor, Element.ALIGN_RIGHT, colorBordeCuadricula));
                    }
                }

                doc.Add(tblOperaciones);

                // ==========================================
                // 7. CAJA DE ALERTA SPPLD (BARRA ROJA)
                // ==========================================
                PdfPTable tblAlerta = new PdfPTable(1) { WidthPercentage = 100f, SpacingAfter = 35f };
                PdfPCell cellAlerta = new PdfPCell
                {
                    BackgroundColor = colorFondoAlerta,
                    Border = PdfRectangle.LEFT_BORDER,       // Solo borde izquierdo
                    BorderWidthLeft = 3.5f,
                    BorderColorLeft = colorBarraAlerta,
                    Padding = 8f
                };

                PdfParagraph pAlerta = new PdfParagraph();
                pAlerta.Add(new Chunk("DISPOSICIÓN PARA PRESENTACIÓN DE AVISO (PORTAL SPPLD):\n", fAlertaTitulo));
                pAlerta.Add(new Chunk("La presente ficha técnica certifica que el monto o la naturaleza del acto notarial actualiza la obligación de emitir el Aviso correspondiente a través del Portal de Prevención de Lavado de Dinero (SPPLD - SAT). El aviso deberá formalizarse a más tardar el ", fAlertaCuerpo));
                pAlerta.Add(new Chunk("día 17 del mes inmediato siguiente", fAlertaBold));
                pAlerta.Add(new Chunk(" a la fecha del instrumento notarial. La información descrita ha sido validada contra el protocolo notarial.", fAlertaCuerpo));
                pAlerta.Alignment = Element.ALIGN_JUSTIFIED;

                cellAlerta.AddElement(pAlerta);
                tblAlerta.AddCell(cellAlerta);
                doc.Add(tblAlerta);

                // ==========================================
                // 8. BLOQUE DE FIRMA
                // ==========================================
                PdfPTable tblFirma = new PdfPTable(1) { WidthPercentage = 45f, HorizontalAlignment = Element.ALIGN_CENTER, SpacingAfter = 25f };
                PdfPCell cellFirma = new PdfPCell
                {
                    Border = PdfRectangle.TOP_BORDER,
                    BorderWidthTop = 1f,
                    BorderColorTop = new BaseColor(180, 190, 200),
                    HorizontalAlignment = Element.ALIGN_CENTER,
                    PaddingTop = 6f
                };
                cellFirma.AddElement(new PdfParagraph($"LIC. {titular}", fFirmaTitular) { Alignment = Element.ALIGN_CENTER });
                cellFirma.AddElement(new PdfParagraph($"Notario Público  No. {numNotaria}", fFirmaSub) { Alignment = Element.ALIGN_CENTER });
                tblFirma.AddCell(cellFirma);
                doc.Add(tblFirma);

                // ==========================================
                // 9. PIE DE PÁGINA
                // ==========================================
                LineSeparator lineaPie = new LineSeparator(0.5f, 100f, colorBordeCuadricula, Element.ALIGN_CENTER, -2);
                doc.Add(new Chunk(lineaPie));

                PdfParagraph pFooter = new PdfParagraph($"{DateTime.Now.Year} SISTEMA DE ACUMULATIVAS AG | Control de Umbrales y Acumulaciones Notariales", fFooter)
                {
                    Alignment = Element.ALIGN_CENTER,
                    SpacingBefore = 4f
                };
                doc.Add(pFooter);

                doc.Close();
            }
        }

        // =========================================================================
        // NUEVO MÉTODO: GENERAR EXPEDIENTE ÚNICO DE CLIENTE
        // =========================================================================
        public static void GenerarExpedienteClientePDF(string rutaDestino, string nombre, string rfc, string totalOps, string montoTotal, List<OperacionExpediente> operaciones)
        {
            Document doc = new Document(PageSize.LETTER, 36f, 36f, 30f, 30f);

            using (FileStream fs = new FileStream(rutaDestino, FileMode.Create))
            {
                PdfWriter writer = PdfWriter.GetInstance(doc, fs);
                doc.Open();

                // 1. PALETA DE COLORES EXACTA
                BaseColor colorNotariaOscuro = new BaseColor(24, 43, 73);
                BaseColor colorAzulSubtitulo = new BaseColor(0, 122, 204);
                BaseColor colorGrisTexto = new BaseColor(108, 117, 125);
                BaseColor colorBordeCuadricula = new BaseColor(222, 226, 230);
                BaseColor colorHeaderTabla = new BaseColor(28, 45, 66);
                BaseColor colorVerdeMonto = new BaseColor(21, 128, 61);

                // 2. TIPOGRAFÍAS
                Font fNotariaTitulo = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 15f, colorNotariaOscuro);
                Font fSubtituloDoc = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10.5f, colorAzulSubtitulo);
                Font fUbicacion = FontFactory.GetFont(FontFactory.HELVETICA, 8.5f, colorGrisTexto);
                Font fGridEtiqueta = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 9f, new BaseColor(70, 80, 95));
                Font fGridValor = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 9.5f, BaseColor.BLACK);
                Font fGridMonto = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 11f, colorVerdeMonto);
                Font fHeaderTabla = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 9f, BaseColor.WHITE);
                Font fCeldaTabla = FontFactory.GetFont(FontFactory.HELVETICA, 8.5f, BaseColor.BLACK);
                Font fFirmaTitular = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 9.5f, BaseColor.BLACK);
                Font fFirmaSub = FontFactory.GetFont(FontFactory.HELVETICA, 8.5f, colorGrisTexto);
                Font fFooter = FontFactory.GetFont(FontFactory.HELVETICA, 7.5f, new BaseColor(120, 130, 140));

                // Datos Notaría
                string numNotaria = !string.IsNullOrWhiteSpace(ClsSesion.NumeroNotaria) ? ClsSesion.NumeroNotaria : "28";
                string titular = !string.IsNullOrWhiteSpace(ClsSesion.NombreTitular) ? ClsSesion.NombreTitular.ToUpper() : "TITULAR NO CONFIGURADO";
                string direccion = !string.IsNullOrWhiteSpace(ClsSesion.DireccionCompleta) ? ClsSesion.DireccionCompleta : "GUASAVE SINALOA 81077";

                // 3. ENCABEZADO
                PdfParagraph pNotaria = new PdfParagraph($"NOTARÍA PÚBLICA NO. {numNotaria}", fNotariaTitulo) { Alignment = Element.ALIGN_CENTER };
                PdfParagraph pSub = new PdfParagraph("Expediente Único de Cliente (Historial Transaccional)", fSubtituloDoc) { Alignment = Element.ALIGN_CENTER };
                PdfParagraph pDir = new PdfParagraph($"{direccion} | Control Interno LFPIORPI", fUbicacion) { Alignment = Element.ALIGN_CENTER, SpacingAfter = 10f };

                doc.Add(pNotaria);
                doc.Add(pSub);
                doc.Add(pDir);

                // Línea divisoria superior
                LineSeparator lineaSuperior = new LineSeparator(1f, 100f, colorAzulSubtitulo, Element.ALIGN_CENTER, -2);
                doc.Add(new Chunk(lineaSuperior));
                doc.Add(new PdfParagraph(" ") { Font = FontFactory.GetFont(FontFactory.HELVETICA, 4f) });

                // 4. TABLA DE RESUMEN
                PdfPTable tblCliente = new PdfPTable(2) { WidthPercentage = 100f, SpacingBefore = 10f, SpacingAfter = 15f };
                tblCliente.SetWidths(new float[] { 30f, 70f });

                AgregarCeldaGrid(tblCliente, "Cliente / Razón Social:", fGridEtiqueta, colorBordeCuadricula);
                AgregarCeldaGrid(tblCliente, nombre, fGridValor, colorBordeCuadricula);

                AgregarCeldaGrid(tblCliente, "RFC:", fGridEtiqueta, colorBordeCuadricula);
                AgregarCeldaGrid(tblCliente, rfc, fGridValor, colorBordeCuadricula);

                AgregarCeldaGrid(tblCliente, "Total de Escrituras:", fGridEtiqueta, colorBordeCuadricula);
                AgregarCeldaGrid(tblCliente, totalOps, fGridValor, colorBordeCuadricula);

                AgregarCeldaGrid(tblCliente, "Monto Histórico Acumulado:", fGridEtiqueta, colorBordeCuadricula);
                AgregarCeldaGrid(tblCliente, montoTotal, fGridMonto, colorBordeCuadricula);

                doc.Add(tblCliente);

                // 5. TABLA DESGLOSE DE OPERACIONES
                PdfPTable tblOps = new PdfPTable(5) { WidthPercentage = 100f, SpacingAfter = 20f };
                tblOps.SetWidths(new float[] { 13f, 13f, 40f, 17f, 17f });

                string[] headers = { "Fecha", "No. Escritura", "Tipo de Operación", "Monto", "Responsable" };
                foreach (string h in headers)
                {
                    PdfPCell cell = new PdfPCell(new Phrase(h, fHeaderTabla))
                    {
                        BackgroundColor = colorHeaderTabla,
                        Border = PdfRectangle.NO_BORDER,
                        PaddingTop = 6f,
                        PaddingBottom = 6f,
                        PaddingLeft = 5f,
                        PaddingRight = 5f,
                        HorizontalAlignment = h == "Monto" ? Element.ALIGN_RIGHT : (h == "No. Escritura" ? Element.ALIGN_CENTER : Element.ALIGN_LEFT)
                    };
                    tblOps.AddCell(cell);
                }

                foreach (var op in operaciones)
                {
                    tblOps.AddCell(CrearCeldaOperacion(op.FechaOperacion.ToString("dd/MM/yyyy"), fCeldaTabla, Element.ALIGN_CENTER, colorBordeCuadricula));
                    tblOps.AddCell(CrearCeldaOperacion(op.FolioEscritura, fCeldaTabla, Element.ALIGN_CENTER, colorBordeCuadricula));
                    tblOps.AddCell(CrearCeldaOperacion(op.TipoOperacion, fCeldaTabla, Element.ALIGN_LEFT, colorBordeCuadricula));
                    tblOps.AddCell(CrearCeldaOperacion(op.Monto.ToString("C2"), fGridValor, Element.ALIGN_RIGHT, colorBordeCuadricula));
                    tblOps.AddCell(CrearCeldaOperacion(op.Usuario, fCeldaTabla, Element.ALIGN_CENTER, colorBordeCuadricula));
                }

                doc.Add(tblOps);

                // 6. FIRMA Y PIE DE PÁGINA
                PdfPTable tblFirma = new PdfPTable(1) { WidthPercentage = 45f, HorizontalAlignment = Element.ALIGN_CENTER, SpacingBefore = 30f, SpacingAfter = 15f };
                PdfPCell cellFirma = new PdfPCell
                {
                    Border = PdfRectangle.TOP_BORDER,
                    BorderWidthTop = 1f,
                    BorderColorTop = colorGrisTexto,
                    HorizontalAlignment = Element.ALIGN_CENTER,
                    PaddingTop = 6f
                };
                cellFirma.AddElement(new PdfParagraph($"LIC. {titular}", fFirmaTitular) { Alignment = Element.ALIGN_CENTER });
                cellFirma.AddElement(new PdfParagraph($"Notario Público No. {numNotaria}", fFirmaSub) { Alignment = Element.ALIGN_CENTER });
                tblFirma.AddCell(cellFirma);
                doc.Add(tblFirma);

                LineSeparator lineaPie = new LineSeparator(0.5f, 100f, colorBordeCuadricula, Element.ALIGN_CENTER, -2);
                doc.Add(new Chunk(lineaPie));

                PdfParagraph pFooter = new PdfParagraph($"{DateTime.Now.Year} SISTEMA DE ACUMULATIVAS AG | Expediente generado el {DateTime.Now:dd/MM/yyyy HH:mm:ss}", fFooter)
                {
                    Alignment = Element.ALIGN_CENTER,
                    SpacingBefore = 4f
                };
                doc.Add(pFooter);

                doc.Close();
            }
        }

        // =========================================================================
        // MÉTODOS DE AYUDA PARA CREAR TABLAS
        // =========================================================================
        private static void AgregarCeldaGrid(PdfPTable table, string texto, Font fuente, BaseColor colorBorde)
        {
            PdfPCell cell = new PdfPCell(new Phrase(texto, fuente))
            {
                Border = PdfRectangle.BOX,
                BorderColor = colorBorde,
                BorderWidth = 0.8f,
                PaddingTop = 5.5f,
                PaddingBottom = 5.5f,
                PaddingLeft = 8f,
                PaddingRight = 8f,
                VerticalAlignment = Element.ALIGN_MIDDLE
            };
            table.AddCell(cell);
        }

        private static PdfPCell CrearCeldaOperacion(string texto, Font fuente, int alineacion, BaseColor colorBorde)
        {
            return new PdfPCell(new Phrase(texto, fuente))
            {
                HorizontalAlignment = alineacion,
                VerticalAlignment = Element.ALIGN_MIDDLE,
                Border = PdfRectangle.BOTTOM_BORDER,
                BorderColorBottom = colorBorde,
                BorderWidthBottom = 0.8f,
                PaddingTop = 5f,
                PaddingBottom = 5f,
                PaddingLeft = 5f,
                PaddingRight = 5f
            };
        }
    }
}