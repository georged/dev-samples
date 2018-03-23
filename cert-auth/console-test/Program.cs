using Microsoft.IdentityModel.Clients.ActiveDirectory;
using System;
using System.Configuration;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Xrm.Tools.WebAPI;

namespace console_test
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Press S for clientid/secret, C for certificate auth");
            var key = Console.ReadKey();
            Task.Run(async () =>
            {
                string clientId = ConfigurationManager.AppSettings["ClientId"];
                string clientSecret = ConfigurationManager.AppSettings["ClientSecret"];
                string apiUrl = ConfigurationManager.AppSettings["ApiUrl"];
                string thumb = ConfigurationManager.AppSettings["CertThumb"];

                var authParams = await AuthenticationParameters.CreateFromResourceUrlAsync(new Uri(apiUrl));
                var authContext = new AuthenticationContext(authParams.Authority);

                // clientid/secret
                var clientCreds = new ClientCredential(clientId, clientSecret);
                
                // certificate
                X509Store store = new X509Store(StoreLocation.CurrentUser);
                store.Open(OpenFlags.ReadOnly);
                var certs = store.Certificates.Find(X509FindType.FindByThumbprint, thumb, false);
                var cert = certs[0];
                var certCreds = new ClientAssertionCertificate(clientId, certs[0]);

                AuthenticationResult result;
                if(key.Key == ConsoleKey.S)
                    result = await authContext.AcquireTokenAsync(authParams.Resource,
                        clientCreds);
                else
                    result = await authContext.AcquireTokenAsync(authParams.Resource,
                        certCreds);

                CRMWebAPI api = new CRMWebAPI(apiUrl, result.AccessToken);
                dynamic whoAmI = await api.ExecuteFunction("WhoAmI");
                Console.WriteLine($"{whoAmI.OrganizationId},{whoAmI.BusinessUnitId},{whoAmI.UserId}");
                Console.ReadLine();
            }).Wait();
        }
    }
}
