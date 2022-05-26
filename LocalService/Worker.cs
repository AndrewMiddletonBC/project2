using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.IO;
using Amazon;
using Amazon.SQS;
using Amazon.SQS.Model;
using System.Xml;
using System.Text.Json;
using Amazon.Runtime.CredentialManagement;
using Amazon.Runtime;

namespace LocalService
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private IAmazonSQS sqs;

        private const string logPath = @"D:\Programming\WorkspaceCS455\Project2\LocalService\Log\InsuranceDataService.log";
        public const string DOWN_QUEUE_URL = "https://sqs.us-east-1.amazonaws.com/440725847939/Project2DownwardQueue";
        public const string UP_QUEUE_URL = "https://sqs.us-east-1.amazonaws.com/440725847939/Project2UpwardQueue";

        public Worker(ILogger<Worker> logger)
        {
            _logger = logger;
        }

        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            await base.StartAsync(cancellationToken);
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            await base.StopAsync(cancellationToken);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                Console.WriteLine("************************************");
                Console.WriteLine("Amazon SQS");
                Console.WriteLine("************************************\n");
                while (!stoppingToken.IsCancellationRequested)
                {
                    // initialize aws client
                    var chain = new CredentialProfileStoreChain(@"C:\Users\andre\.aws\credentials");
                    AWSCredentials awsCredentials;
                    sqs = null;
                    if (chain.TryGetAWSCredentials("default", out awsCredentials))
                    {
                        sqs = new AmazonSQSClient(awsCredentials, RegionEndpoint.USEast1);
                    }
                    if (sqs == null)
                    {
                        Console.WriteLine("Unable to initialize amazon sqs client.");
                        throw new Exception("No valid aws credentials");
                    }

                    // longpolling message request start
                    var receiveMessageRequest = new ReceiveMessageRequest
                    {
                        QueueUrl = DOWN_QUEUE_URL,
                        WaitTimeSeconds = 20
                    };

                    XmlDocument xmlDoc = new XmlDocument();
                    xmlDoc.Load(@"D:\Programming\WorkspaceCS455\Project2\LocalService\Data\InsuranceDatabase.xml");
                    Console.WriteLine("************************************\n");
                    var receiveMessageResponse = await sqs.ReceiveMessageAsync(receiveMessageRequest);
                    foreach (var message in receiveMessageResponse.Messages)
                    {

                        // read message id info
                        Console.WriteLine($"Reading message : {message.Body}");
                        WriteToLog(string.Format("Date: {0}\t Read Message: {1}", DateTime.Now, message.Body));
                        Patient? patient;
                        try
                        {
                            patient = JsonSerializer.Deserialize<Patient>(message.Body);
                        }
                        catch (JsonException ex)
                        {
                            Console.WriteLine(ex.Message);
                            continue;
                        }

                        // remove processed message from queue

                        Console.WriteLine("Removing message from downward queue...");
                        _ = sqs.DeleteMessageAsync(DOWN_QUEUE_URL, message.ReceiptHandle);

                        string id = patient.id;
                        Console.WriteLine(id);

                        string? outputMessage;
                        try
                        {
                            // get corresponding info from database
                            XmlElement root = xmlDoc.DocumentElement;
                            XmlNode item = root.SelectSingleNode("patient[@id=\"" + id + "\"]");
                            if (item != null)
                            {
                                XmlNode attribute = item.SelectSingleNode("policy");
                                string policy = attribute.Attributes["policyNumber"].Value;
                                string provider = item.InnerText;
                                Console.WriteLine(item.InnerText);
                                outputMessage = "{ \"PatientId\": \"" + id + "\", \"HasInsurance\": true, \"PolicyData\": { \"PolicyNumber\": \"" + policy + "\", \"Provider\": \"" + provider + "\" }}";
                            }
                            else
                            {
                                outputMessage = "{ \"PatientId\": \"" + id + "\", \"HasInsurance\": false }";
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.Message);
                            continue;
                        }

                        // send response for message
                        Console.WriteLine("Sending message to upward queue.");
                        var sqsMessageRequest = new SendMessageRequest
                        {
                            QueueUrl = UP_QUEUE_URL,
                            MessageBody = outputMessage
                        };

                        _ = sqs.SendMessageAsync(sqsMessageRequest);

                        WriteToLog(string.Format("Date: {0}\t Posted Message: {1}", DateTime.Now, outputMessage));

                    }
                    Console.WriteLine("Finished");
                    Console.ReadLine();
                    await Task.Delay(1000, stoppingToken);
                }
            }
            catch (Exception ex)
            {
                WriteToLog("Error: " + ex.Message);
            }
        }

        public void WriteToLog(string message)
        {
            using (StreamWriter writer = new StreamWriter(logPath, append: true))
            {
                writer.WriteLine(message);
            }
        }
    }

    public class Patient
    {
        public string id
        {
            get; set;
        }
    }
}