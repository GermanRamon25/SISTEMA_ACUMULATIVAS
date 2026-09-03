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

        // === DATOS DE LA NOTARÍA ===
        public static string NumeroNotaria { get; set; } = "";
        public static string NombreTitular { get; set; } = "";
        public static string DireccionCompleta { get; set; } = "";
        public static string TelefonoNotaria { get; set; } = "";
        public static string EmailNotaria { get; set; } = "";

        public static void IniciarSesion(int id, string nombre, string rol)
        {
            UsuarioId = id;
            NombreUsuario = nombre;
            Rol = rol;
        }


        public static void CargarDatosNotaria(string numero, string titular, string direccion, string telefono, string email)
        {
            NumeroNotaria = numero;
            NombreTitular = titular;
            DireccionCompleta = direccion;
            TelefonoNotaria = telefono;
            EmailNotaria = email;
        }

        public static void CerrarSesion()
        {
            UsuarioId = 0;
            NombreUsuario = "Sistema";
            Rol = "N/A";
            NumeroNotaria = null;
            NombreTitular = null;
            DireccionCompleta = null;
            TelefonoNotaria = null;
            EmailNotaria = null;
        }

    }
}
