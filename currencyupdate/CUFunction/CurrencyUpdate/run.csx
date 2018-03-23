using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Client;
using Microsoft.Xrm.Sdk.WebServiceClient;
using Microsoft.Xrm.Tooling.Connector;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Configuration;
public static void Run(TimerInfo myTimer, TraceWriter log)
{
    log.Info($"C# Timer trigger function executed at: {DateTime.Now}");
    string connectionString = ConfigurationManager
                 .ConnectionStrings["o365"].ConnectionString;
    log.Info($"{connectionString}");
    UpdateCurrencies(connectionString, log);
}

public static void UpdateCurrencies(string connectionString, TraceWriter log)
{
    log.Info($"Connecting to Dynamics 365...");
    CrmServiceClient crmSvc = new CrmServiceClient(connectionString);

    if (crmSvc == null || !crmSvc.IsReady)
    {
        throw new ApplicationException("Couldn't connect:" + crmSvc.LastCrmError);
    }

    log.Info($"I'm ready, user id {crmSvc.GetMyCrmUserId()}");

    using (OrganizationServiceContext context =
        new OrganizationServiceContext(crmSvc))
    {
        log.Info($"Retrieving currencies...");
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

                    log.Info($"Updating currency {isoCode}, rate {exRate}");
                    cur["exchangerate"] = exRate;
                    context.UpdateObject(cur);
                }
            }

            log.Info($"Saving changes...");
            context.SaveChanges();
        }

    }
    log.Info("DONE!");
    //Console.ReadLine();
    return;
}

[DataContract]
public class OpenRates
{
    [DataMember]
    public string disclaimer { get; set; }

    [DataMember]
    public string license { get; set; }

    [DataMember]
    public int timestamp { get; set; }
    public DateTime stamp
    {
        get
        {
            return new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc)
                .AddSeconds(timestamp);
        }
    }

    [DataMember(Name = "base")]
    public string basecode { get; set; }

    [DataMember]
    public Dictionary<string, decimal> rates;

    internal static OpenRates GetRates()
    {
        string url = "https://openexchangerates.org/api/latest.json?app_id=[key]";
        string rates;
        using (WebClient wc = new WebClient())
        {
            rates = wc.DownloadString(url);
        }

        if (!string.IsNullOrWhiteSpace(rates))
        {
            using (Stream str = new MemoryStream(Encoding.UTF8.GetBytes(rates)))
            {
                var deserializer = new DataContractJsonSerializer(
                    typeof(OpenRates),
                    new DataContractJsonSerializerSettings()
                    {
                        UseSimpleDictionaryFormat = true
                    });

                return (OpenRates)deserializer.ReadObject(str);
            }
        }
        return null;
    }
}

public static void Connect()
{
    string clientId = "";
    string clientSecret = "";
    string authority = "https://login.microsoftonline.com/csvilt.onmicrosoft.com";
    string orgUrl = "https://contoso.crm.dynamics.com";

    ClientCredential creds = new ClientCredential(clientId, clientSecret);
    AuthenticationContext authContext = new AuthenticationContext(authority);
    AuthenticationResult authResult = authContext.AcquireToken(orgUrl, creds);

    var proxy = new OrganizationWebProxyClient(
        new Uri(orgUrl + "/xrmservices/2011/organization.svc/web?SdkClientVersion=8.2"),
        false);

    proxy.HeaderToken = authResult.AccessToken;
    CrmServiceClient crmSvc = new CrmServiceClient(proxy);
}
