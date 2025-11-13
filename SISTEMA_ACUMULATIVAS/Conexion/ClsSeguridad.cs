using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace SISTEMA_ACUMULATIVAS.Conexion
{
    public class ClsSeguridad
    {
        private const int SaltSize = 16;
        private const int HashSize = 20;
        private const int Iterations = 10000;

        public static void CrearPasswordHash(string password, out byte[] hash, out byte[] salt)
        {
            using (var rng = new RNGCryptoServiceProvider())
            {
                salt = new byte[SaltSize];
                rng.GetBytes(salt);
            }

            using (var pbkdf2 = new Rfc2898DeriveBytes(password, salt, Iterations))
            {
                hash = pbkdf2.GetBytes(HashSize);
            }
        }

        public static bool VerificarPasswordHash(string password, byte[] hashGuardado, byte[] saltGuardado)
        {
            using (var pbkdef2 = new Rfc2898DeriveBytes(password, saltGuardado, Iterations))
            {
                byte[] hashNuevo = pbkdef2.GetBytes(HashSize);
                if (hashNuevo.Length != hashGuardado.Length) return false;
                for (int i = 0; i < hashNuevo.Length; i++)
                {
                    if (hashNuevo[i] != hashGuardado[i])
                        return false;
                }
                return true;
            }
        }
    }
}
