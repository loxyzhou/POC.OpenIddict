using System.Security.Cryptography.X509Certificates;
using System.Text.RegularExpressions;

namespace IdentityService.Shared
{
    public static class CertificateExtension
    {
        public static X509Certificate2 GetCertificate(string thumbprint)
        {
            var cert = GetLocalMachineCertificate(thumbprint, false);
            if (cert == null)
            {
                cert = GetLocalMachineCertificate(thumbprint, true);
            }

            if (cert == null)
            {
                throw new FileNotFoundException($"The certificate with thumbprint: '{thumbprint}' not found in local machine cert store.");
            }

            return cert;
        }

        private static X509Certificate2? GetLocalMachineCertificate(string thumbprint, bool isRootStore = false)
        {
            thumbprint = Regex.Replace(thumbprint, @"[^\da-fA-F]", string.Empty).ToUpper();

            var store = new X509Store(isRootStore == true ? StoreName.Root : StoreName.My, StoreLocation.LocalMachine);

            try
            {
                store.Open(OpenFlags.ReadOnly);

                var certCollection = store.Certificates;
                var signingCert = certCollection.Find(X509FindType.FindByThumbprint, thumbprint, false);
                if (signingCert.Count == 0)
                {
                    return null;
                }

                return signingCert[0];
            }
            finally
            {
                store.Close();
            }
        }
    }
}
