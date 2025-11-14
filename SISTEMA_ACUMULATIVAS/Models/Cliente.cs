using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SISTEMA_ACUMULATIVAS.Models
{
    public class Cliente
    {
        public int Id { get; set; }
        public string Nombre { get; set; }
        public string RFC { get; set; }
        public string CURP { get; set; }
        public string TipoPersona { get; set; } // Almacenará "F" o "M"
        public DateTime FechaRegistro { get; set; }
        public bool Activo { get; set; }

        // Propiedad extra para el ComboBox (no va a la BD)
        public string TipoPersonaDisplay
        {
            get
            {
                if (TipoPersona == "F") return "Persona Física";
                if (TipoPersona == "M") return "Persona Moral";
                return "Indefinido";
            }
        }
    }
}