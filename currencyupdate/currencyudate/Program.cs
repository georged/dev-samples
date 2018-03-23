using AlexaCRM.Currency;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Client;
using Microsoft.Xrm.Sdk.WebServiceClient;
using Microsoft.Xrm.Tooling.Connector;
using System;
using System.Configuration;
using System.Linq;

namespace CurrencyUpdate
{
    class Program
    {
        static void Main(string[] args)
        {
            string connectionString = ConfigurationManager
                .ConnectionStrings["o365"].ConnectionString;

            UpdateCurrencies(connectionString);
        }

        public static void UpdateCurrencies(string connectionString)
        {
            Console.WriteLine($"Connecting to Dynamics 365...");
            CrmServiceClient crmSvc = new CrmServiceClient(connectionString);

            if (crmSvc == null || !crmSvc.IsReady)
            {
                throw new ApplicationException("Couldn't connect:" + crmSvc.LastCrmError);
            }

            Console.WriteLine($"I'm ready, user id {crmSvc.GetMyCrmUserId()}");

            using (OrganizationServiceContext context = 
                new OrganizationServiceContext(crmSvc))
            {
                Console.WriteLine($"Retrieving currencies...");
                OpenRates openfx = OpenRates.GetRates();

                if (openfx != null)
                {
                    var baseIso =
                        (from currency in context.CreateQuery("transactioncurrency")
                         join org in context.CreateQuery("organization")
                         on currency.GetAttributeValue<Guid>("transactioncurrencyid") 
                            equals org.GetAttributeValue<EntityReference>("basecurrencyid").Id
                         select currency.GetAttributeValue<string>("isocurrencycode"))
                         .FirstOrDefault();

                    var currencies = context.CreateQuery("transactioncurrency");
                    foreach (Entity cur in currencies)
                    {
                        var isoCode = cur.GetAttributeValue<string>("isocurrencycode");

                        // update exchange rate
                        if (isoCode != baseIso)
                        {
                            var exRate =
                                openfx.rates[isoCode] / openfx.rates[baseIso];

                            Console.WriteLine($"Updating currency {isoCode}, rate {exRate}");
                            cur["exchangerate"] = exRate;
                            context.UpdateObject(cur);
                        }
                    }

                    Console.WriteLine($"Saving changes...");
                    context.SaveChanges();
                }

            }
            Console.WriteLine("DONE!");
            //Console.ReadLine();
            return;
        }
    }
}
