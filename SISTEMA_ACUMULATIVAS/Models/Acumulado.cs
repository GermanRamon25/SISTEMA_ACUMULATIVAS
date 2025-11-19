using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SISTEMA_ACUMULATIVAS.Models
{
    public class Acumulado
    {
        // --- Datos directos de la Base de Datos ---
        public int ClienteId { get; set; }
        public string ClienteNombre { get; set; } // Viene del JOIN con Clientes
        public decimal MontoAcumulado { get; set; }
        public DateTime UltimaActualizacion { get; set; }

        // --- Datos Calculados para la Vista (Dashboard) ---

        // Porcentaje respecto al umbral de 8,000 UMAs (Identificación)
        public double PorcentajeUmbral { get; set; }

        // Texto para el DataGrid: "Normal", "Alerta", "AVISO URGENTE"
        public string EstadoAlerta { get; set; }
    }
}
