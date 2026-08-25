using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SISTEMA_ACUMULATIVAS.Models
{
    public class NotariaModel
    {
        public int Id { get; set; }
        public string NombreTitular { get; set; }
        public string NumeroNotaria { get; set; }
        public string DireccionCompleta { get; set; }
        public string Telefono { get; set; }
        public string EmailContacto { get; set; }
    }
}