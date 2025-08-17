using System.Security.Cryptography;
using System.Text;

namespace TicketSystem.Services
{
    public class AccountCreationService
    {


        public string GenerateEmail(string firstName, string lastName, string domain = "example.com")
        {
            var baseEmail = $"{firstName.ToLower()}.{lastName.ToLower()}@{domain}";
            return baseEmail;
        }


        // Rastgele şifre üretir
        public string GeneratePassword(int length = 10)
        {
            const string valid = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890!@#$%^&*";
            StringBuilder res = new StringBuilder();
            using (var rng = new RNGCryptoServiceProvider())
            {
                byte[] uintBuffer = new byte[sizeof(uint)];

                while (length-- > 0)
                {
                    rng.GetBytes(uintBuffer);
                    uint num = BitConverter.ToUInt32(uintBuffer, 0);
                    res.Append(valid[(int)(num % (uint)valid.Length)]);
                }
            }
            return res.ToString();
        }
    }

}

