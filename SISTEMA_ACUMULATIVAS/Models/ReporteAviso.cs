using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SISTEMA_ACUMULATIVAS.Models
{
    // Representa al CLIENTE que debe ser reportado
    public class ReporteAvisoItem
    {
        public int ClienteId { get; set; }
        public string NombreCliente { get; set; }
        public string RFC { get; set; }
        public decimal MontoTotalAcumulado { get; set; }
        public string MotivoAviso { get; set; } // Ej: "Acumulación 6 meses"

        // Lista de operaciones que causaron la alerta
        public List<Operacion> OperacionesDetalle { get; set; }

        public ReporteAvisoItem()
        {
            OperacionesDetalle = new List<Operacion>();
        }
    }
}
