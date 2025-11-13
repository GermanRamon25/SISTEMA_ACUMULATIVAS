using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SISTEMA_ACUMULATIVAS.Conexion
{
    public static class ClsSesion
    {
        public static int UsuarioId { get; set; } = 0; // 0 = Nadie logueado
        public static string NombreUsuario { get; set; } = "Sistema";
        public static string Rol { get; set; } = "N/A";

        public static void IniciarSesion(int id, string nombre, string rol)
        {
            UsuarioId = id;
            NombreUsuario = nombre;
            Rol = rol;
        }

        public static void CerrarSesion()
        {
            UsuarioId = 0;
            NombreUsuario = "Sistema";
            Rol = "N/A";
        }
    }
}
