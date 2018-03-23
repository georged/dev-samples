using Microsoft.Xrm.Sdk;
using System.Globalization;

namespace QueueReader
{
    class Program
    {
        static void Main(string[] args)
        {
            Balloon.Show("Hello", "world");

            Consumer consumer = new Consumer();
            consumer.CreateQueueClient();
            consumer.RemoteCall += Consumer_RemoteCall;
            consumer.ProcessMessages();
        }

        private static void Consumer_RemoteCall(object sender, RemoteExecutionContext context)
        {
            Entity entity = context.InputParameters["Target"] as Entity;
            if (entity != null)
            {
                if (context.PrimaryEntityName == "account")
                {
                    var name = entity.GetAttributeValue<string>("name");
                    Balloon.Show("New account!", name);
                }
                else if (context.PrimaryEntityName == "contact")
                {
                    var name = entity.GetAttributeValue<string>("fullname");
                    Balloon.Show("New contact!", name);
                }
                else if (context.PrimaryEntityName == "opportunity")
                {
                    var est = entity.GetAttributeValue<Money>("estimatedvalue");
                    string value = est == null ? "unknown value":est.Value.ToString("C", CultureInfo.CurrentCulture);
                    Balloon.Show(entity.GetAttributeValue<string>("name"), value);
                }
            }
        }
    }
}
