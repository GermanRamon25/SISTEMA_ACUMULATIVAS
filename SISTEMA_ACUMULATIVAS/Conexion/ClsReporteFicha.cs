using SISTEMA_ACUMULATIVAS.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace SISTEMA_ACUMULATIVAS.Services
{
    public static class ClsReporteFicha
    {


        public static string GenerarHtmlFicha(
            string nombreCliente,
            string rfc,
            string curp,
            decimal totalAcumulado,
            string motivoAviso,
            List<Operacion> operaciones)
        {
            // 1. Construir las filas dinámicas de la tabla de actos notariales
            StringBuilder filasHtml = new StringBuilder();
            if (operaciones != null && operaciones.Count > 0)
            {
                foreach (var op in operaciones)
                {
                    // --- ETIQUETA ROJA DE DETONANTE ---
                    string etiqueta = op.EsDetonante
                        ? $"<br/><span style='color: #DC2626; font-size: 10.5px; font-weight: bold;'>[{op.EtiquetaDetonante.ToUpper()}]</span>"
                        : "";

                    filasHtml.Append($@"
                        <tr>
                            <td>{op.FechaOperacion:dd/MM/yyyy}</td>
                            <td><strong>{op.FolioEscritura}</strong></td>
                            <td>{op.TipoOperacion}{etiqueta}</td>
                            <td style='text-align: right; font-weight: bold;'>{op.Monto:C}</td>
                        </tr>");
                }
            }
            else
            {
                filasHtml.Append("<tr><td colspan='4' style='text-align: center; color: #64748B;'>Sin operaciones registradas en el periodo.</td></tr>");
            }

            // 2. Estructura completa del documento con reglas de impresión y formato legal
            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'/>
    <style>
        /* --- CONFIGURACIÓN DE PÁGINA E IMPRESIÓN --- */
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

        /* --- ESTILOS VISUALES DEL DOCUMENTO --- */
        body {{ 
            font-family: 'Segoe UI', Arial, sans-serif; 
            margin: 20px; 
            color: #1E293B; 
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
            margin-bottom: 18px; 
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
            margin-bottom: 18px; 
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
            <td><strong>{nombreCliente}</strong></td>
        </tr>
        <tr>
            <td class='label'>RFC / CURP:</td>
            <td>{rfc} | {curp}</td>
        </tr>
        <tr>
            <td class='label'>Monto Total Acumulado (6 Meses):</td>
            <td><strong style='color: #0284C7; font-size: 14px;'>{totalAcumulado:C}</strong></td>
        </tr>
        <tr>
            <td class='label'>Motivo / Criterio del Aviso:</td>
            <td><strong style='color: #DC2626;'>{motivoAviso}</strong></td>
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
            {filasHtml}
        </tbody>
    </table>

    <div class='alert-box'>
        <strong>DISPOSICIÓN PARA PRESENTACIÓN DE AVISO (PORTAL SPPLD):</strong><br/>
        La presente ficha técnica certifica que el monto o la naturaleza del acto notarial actualiza la obligación de emitir el Aviso correspondiente a través del Portal de Prevención de Lavado de Dinero (SPPLD - SAT). El aviso deberá formalizarse a más tardar el <strong>día 17 del mes inmediato siguiente</strong> a la fecha de firma del instrumento notarial. La información descrita ha sido validada contra el protocolo notarial.
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
    }
}