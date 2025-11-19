using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SISTEMA_ACUMULATIVAS.Models
{
    public class Operacion
    {
        // Propiedades que mapean directamente a la Base de Datos
        public int Id { get; set; }
        public int ClienteId { get; set; }
        public string TipoOperacion { get; set; }
        public decimal Monto { get; set; }
        public DateTime FechaOperacion { get; set; }
        public string FolioEscritura { get; set; }
        public string Descripcion { get; set; }
        public int UsuarioId { get; set; } // Para registrar quién lo hizo

        // Propiedades EXTRA (No están en la tabla Operaciones, pero sirven para mostrar datos en la Vista)
        public string ClienteNombre { get; set; } // Lo llenaremos con el JOIN
    }
}
