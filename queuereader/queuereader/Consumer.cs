using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;
using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QueueReader
{

    public delegate void RemoteExecutionEventHandler(object sender, RemoteExecutionContext context);
    public class Consumer
    {
        private const String MyQueuePath = "[queue name]";
        private QueueClient queueClient;

        public event RemoteExecutionEventHandler RemoteCall;
        public Consumer()
        {
        }

        public void CreateQueueClient()
        {
            string cs = "Endpoint=sb://[namespace].servicebus.windows.net/;SharedAccessKeyName=listen;SharedAccessKey=[access key]";
            queueClient = QueueClient.CreateFromConnectionString(cs, MyQueuePath, ReceiveMode.ReceiveAndDelete);
        }

        public void ProcessMessages()
        {
            // Get receive mode (PeekLock or ReceiveAndDelete) from queueClient.
            ReceiveMode mode = this.queueClient.Mode;
            while (true)
            {
                // Retrieve a message from the queue.
                Console.WriteLine("Waiting for a message from the queue... ");
                BrokeredMessage message;
                try
                {
                    message = this.queueClient.Receive();
                    // Check if the message received.
                    if (message != null)
                    {
                        try
                        {
                            // Verify EntityLogicalName and RequestName message properties
                            // to only process specific message sent from Microsoft Dynamics CRM.
                            string keyRoot = "http://schemas.microsoft.com/xrm/2011/Claims/";
                            string entityLogicalNameKey = "EntityLogicalName";
                            string requestNameKey = "RequestName";
                            object entityLogicalNameValue;
                            object requestNameValue;
                            message.Properties.TryGetValue(keyRoot + entityLogicalNameKey, out entityLogicalNameValue);
                            message.Properties.TryGetValue(keyRoot + requestNameKey, out requestNameValue);

                            // Filter message with specific message properties. i.e. EntityLogicalName=letter and RequestName=Create
                            if (entityLogicalNameValue != null && requestNameValue != null)
                            {
                                Console.WriteLine("--------------------------------");
                                Console.WriteLine(string.Format("Message received: Id = {0}", message.MessageId));
                                // Display message properties that are set on the brokered message.
                                Utility.PrintMessageProperties(message.Properties);
                                
                                // Display body details.
                                var body = message.GetBody<RemoteExecutionContext>();

                                Utility.Print(body);
                                Console.WriteLine("--------------------------------");

                                if(RemoteCall != null)
                                {
                                    RemoteCall(this, body);
                                }
                            }
                            else
                            {
                                continue;
                            }
                            // If receive mode is PeekLock then set message complete to remove message from queue.
                            if (mode == ReceiveMode.PeekLock)
                            {
                                message.Complete();
                            }
                        }
                        catch (System.Exception ex)
                        {
                            // Indicate a problem, unlock message in queue.
                            if (mode == ReceiveMode.PeekLock)
                            {
                                message.Abandon();
                            }
                            Console.WriteLine(ex.Message);
                            continue;
                        }
                    }
                    else
                    {
                        break;
                    }
                }
                catch (System.TimeoutException e)
                {
                    Console.WriteLine(e.Message);
                    continue;
                }
                catch (System.ServiceModel.FaultException e)
                {
                    Console.WriteLine(e.Message);
                    continue;
                }
            }
        }
    }

    internal static class Utility
    {
        public static void Print(RemoteExecutionContext context)
        {
            if (context == null)
            {
                Console.WriteLine("Context is null.");
                return;
            }

            Console.WriteLine("UserId: {0}", context.UserId);
            Console.WriteLine("OrganizationId: {0}", context.OrganizationId);
            Console.WriteLine("OrganizationName: {0}", context.OrganizationName);
            Console.WriteLine("MessageName: {0}", context.MessageName);
            Console.WriteLine("Stage: {0}", context.Stage);
            Console.WriteLine("Mode: {0}", context.Mode);
            Console.WriteLine("PrimaryEntityName: {0}", context.PrimaryEntityName);
            Console.WriteLine("SecondaryEntityName: {0}", context.SecondaryEntityName);

            Console.WriteLine("BusinessUnitId: {0}", context.BusinessUnitId);
            Console.WriteLine("CorrelationId: {0}", context.CorrelationId);
            Console.WriteLine("Depth: {0}", context.Depth);
            Console.WriteLine("InitiatingUserId: {0}", context.InitiatingUserId);
            Console.WriteLine("IsExecutingOffline: {0}", context.IsExecutingOffline);
            Console.WriteLine("IsInTransaction: {0}", context.IsInTransaction);
            Console.WriteLine("IsolationMode: {0}", context.IsolationMode);
            Console.WriteLine("Mode: {0}", context.Mode);
            Console.WriteLine("OperationCreatedOn: {0}", context.OperationCreatedOn.ToString());
            Console.WriteLine("OperationId: {0}", context.OperationId);
            Console.WriteLine("PrimaryEntityId: {0}", context.PrimaryEntityId);
            Console.WriteLine("OwningExtension LogicalName: {0}", context.OwningExtension.LogicalName);
            Console.WriteLine("OwningExtension Name: {0}", context.OwningExtension.Name);
            Console.WriteLine("OwningExtension Id: {0}", context.OwningExtension.Id);
            Console.WriteLine("SharedVariables: {0}", (context.SharedVariables == null ? "NULL" :
                SerializeParameterCollection(context.SharedVariables)));
            Console.WriteLine("InputParameters: {0}", (context.InputParameters == null ? "NULL" :
                SerializeParameterCollection(context.InputParameters)));
            Console.WriteLine("OutputParameters: {0}", (context.OutputParameters == null ? "NULL" :
                SerializeParameterCollection(context.OutputParameters)));
            Console.WriteLine("PreEntityImages: {0}", (context.PreEntityImages == null ? "NULL" :
                SerializeEntityImageCollection(context.PreEntityImages)));
            Console.WriteLine("PostEntityImages: {0}", (context.PostEntityImages == null ? "NULL" :
                SerializeEntityImageCollection(context.PostEntityImages)));
        }

        #region Private methods.
        private static string SerializeEntity(Entity e)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(Environment.NewLine);
            sb.Append(" LogicalName: " + e.LogicalName);
            sb.Append(Environment.NewLine);
            sb.Append(" EntityId: " + e.Id);
            sb.Append(Environment.NewLine);
            sb.Append(" Attributes: [");
            foreach (KeyValuePair<string, object> parameter in e.Attributes)
            {
                sb.Append(parameter.Key + ": " + parameter.Value + "; ");
            }
            sb.Append("]");
            return sb.ToString();
        }

        private static string SerializeParameterCollection(ParameterCollection parameterCollection)
        {
            StringBuilder sb = new StringBuilder();
            foreach (KeyValuePair<string, object> parameter in parameterCollection)
            {
                if (parameter.Value != null && parameter.Value.GetType() == typeof(Entity))
                {
                    Entity e = (Entity)parameter.Value;
                    sb.Append(parameter.Key + ": " + SerializeEntity(e));
                }
                else
                {
                    sb.Append(parameter.Key + ": " + parameter.Value + "; ");
                }
            }
            return sb.ToString();
        }
        private static string SerializeEntityImageCollection(EntityImageCollection entityImageCollection)
        {
            StringBuilder sb = new StringBuilder();
            foreach (KeyValuePair<string, Entity> entityImage in entityImageCollection)
            {
                sb.Append(Environment.NewLine);
                sb.Append(entityImage.Key + ": " + SerializeEntity(entityImage.Value));
            }
            return sb.ToString();
        }
        #endregion

        internal static void PrintMessageProperties(IDictionary<string, object> iDictionary)
        {
            if (iDictionary.Count == 0)
            {
                Console.WriteLine("No Message properties found.");
                return;
            }
            foreach (var item in iDictionary)
            {
                String key = (item.Key != null) ? item.Key.ToString() : "";
                String value = (item.Value != null) ? item.Value.ToString() : "";
                Console.WriteLine(key + " " + value);
            }
        }
    }
}
