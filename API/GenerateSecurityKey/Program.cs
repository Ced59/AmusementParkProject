using System.Security.Cryptography;

namespace GenerateSecurityKey
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine(GenerateSecureSecretKey());
        }



        public static string GenerateSecureSecretKey()
        {
            var randomBytes = new byte[64]; 
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomBytes); 
            }
            return Convert.ToBase64String(randomBytes); 
        }

    }
}
