using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SISTEMA_ACUMULATIVAS.Conexion
{
    public class ClsConfiguracion
    {
        private ClsConexion _conexion = new ClsConexion();

        // Obtener el valor de la UMA desde la BD
        public decimal ObtenerUMA()
        {
            decimal uma = 0;
            using (SqlConnection conn = _conexion.GetConnection())
            {
                string query = "SELECT Valor FROM Configuracion WHERE Clave = 'UMA'";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    object result = cmd.ExecuteScalar();
                    if (result != null) uma = Convert.ToDecimal(result);
                }
            }
            return uma;
        }

        // Actualizar el valor de la UMA
        public void ActualizarUMA(decimal nuevoValor)
        {
            using (SqlConnection conn = _conexion.GetConnection())
            {
                // Usamos CONTEXT_INFO para que el Trigger de auditoría sepa quién hizo el cambio (si agregamos trigger a config)
                // Pero por simplicidad, aquí es update directo.
                string query = "UPDATE Configuracion SET Valor = @val WHERE Clave = 'UMA'";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@val", nuevoValor);
                    cmd.ExecuteNonQuery();
                }
            }
        }
    }
}