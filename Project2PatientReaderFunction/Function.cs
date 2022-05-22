using Amazon.Lambda.Core;
using Amazon.Lambda.S3Events;
using Amazon.S3;
using Amazon.S3.Util;
using Amazon.S3.Model;
using Amazon.SQS;
using Amazon.SQS.Model;

using System.Xml;

/*
 * Code by Andrew
 */

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace Project2PatientReaderFunction;

public class Function
{
    IAmazonS3 S3Client { get; set; }
    IAmazonSQS SQSClient { get; set; }

    /// <summary>
    /// Default constructor. This constructor is used by Lambda to construct the instance. When invoked in a Lambda environment
    /// the AWS credentials will come from the IAM role associated with the function and the AWS region will be set to the
    /// region the Lambda function is executed in.
    /// </summary>
    public Function()
    {
        S3Client = new AmazonS3Client();
        SQSClient = new AmazonSQSClient();
    }

    /// <summary>
    /// Constructs an instance with a preconfigured S3 client. This can be used for testing the outside of the Lambda environment.
    /// </summary>
    /// <param name="s3Client"></param>
    /// <param name="sqsClient"></param>
    public Function(IAmazonS3 s3Client, IAmazonSQS sqsClient)
    {
        this.S3Client = s3Client;
        this.SQSClient = sqsClient;
    }

    /// <summary>
    /// This method is called for every Lambda invocation. This method takes in an S3 event object and can be used 
    /// to respond to S3 notifications.
    /// </summary>
    /// <param name="evnt"></param>
    /// <param name="context"></param>
    /// <returns></returns>
    public async Task<string?> FunctionHandler(S3Event evnt, ILambdaContext context)
    {
        S3EventNotification.S3Entity s3Entity = evnt.Records?[0].S3;
        if (s3Entity == null)
        {
            return null;
        }


        Console.WriteLine("Bucket: {0}", s3Entity.Bucket.Name);
        Console.WriteLine("File: {0}", s3Entity.Object.Key);

        // Retrieve string contents of object added to s3 bucket
        string? s3contents = null;
        try
        {
            // execute contents request
            Task<GetObjectResponse> response = this.S3Client.GetObjectAsync(s3Entity.Bucket.Name, s3Entity.Object.Key);
            if (response != null)
            {
                using StreamReader responseReader = new(response.Result.ResponseStream);
                // store contents response
                s3contents = responseReader.ReadToEnd();
                responseReader.Close();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error reading object contents {s3Entity.Object.Key} from bucket {s3Entity.Bucket.Name}.");
            Console.WriteLine(ex.Message);
        }

        // handle invalid contents and type tag
        if (s3contents == null)
        {
            Console.WriteLine("File not found");
            return null;
        }
        else if (s3contents.Length == 0)
        {
            Console.WriteLine("File is Empty");
            return null;
        }

        // debug file contents logging
        Console.WriteLine(s3contents);

        // setup db objects
        PatientData patientData = new();
        try
        {
            XmlDocument doc = new();
            doc.LoadXml(s3contents);
            patientData.Id = doc.SelectSingleNode("patient/id").InnerText;
            patientData.Name = doc.SelectSingleNode("patient/name").InnerText;
        }
        catch (XmlException ex)
        {
            Console.WriteLine(ex.Message);
            return null;
        }

        // handle invalid patient id or name
        if (patientData.Id == null || patientData.Name == null)
        {
            Console.WriteLine("Vital patient information absent");
            return null;
        }

        const string QUEUE_URL = "https://sqs.us-east-1.amazonaws.com/440725847939/Project2DownwardQueue";
        string downwardMessage = $"{{id:\"{patientData.Id}\"}}";

        Console.WriteLine($"message: {downwardMessage}");
        
        try
        {
            SendMessageRequest request = new()
            {
                QueueUrl = QUEUE_URL,
                MessageBody = downwardMessage
            };
            Task<SendMessageResponse> response = SQSClient.SendMessageAsync(request);
            response.Wait();
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error putting message to downward queue");
            Console.WriteLine(ex.Message);
        }

        return null;
    }
}