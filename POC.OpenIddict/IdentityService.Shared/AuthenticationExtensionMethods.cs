using System.Reflection;
using System.Security.Cryptography.X509Certificates;

namespace IdentityService.Shared
{
    public static class AuthenticationExtensionMethods
    {

        public static X509Certificate2 TokenEncryptionCertificate()
        {
            var bytes = ReadResourceAsBytes("Encryption");
            return new X509Certificate2(bytes, "pa$$word");
        }

        public static X509Certificate2 TokenSigningCertificate()
        {
            var bytes = ReadResourceAsBytes("Signing");
            return new X509Certificate2(bytes, "pa$$word");
        }

        private static byte[] ReadResourceAsBytes(string name)
        {
            var resource = Assembly.GetAssembly(typeof(AuthenticationExtensionMethods))
               .GetManifestResourceStream($"IdentityService.Shared.Certificates.{name}.pfx");
            var ms = new MemoryStream();
            resource.CopyTo(ms);
            return ms.ToArray();
        }

    }
}
