using Amazon.Lambda.Core;
using Amazon.Lambda.SQSEvents;

using System.Text.Json;

/*
 * Code by Andrew
 */

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace Project2InsuranceDataFunction;

public class Function
{
    /// <summary>
    /// Default constructor. This constructor is used by Lambda to construct the instance. When invoked in a Lambda environment
    /// the AWS credentials will come from the IAM role associated with the function and the AWS region will be set to the
    /// region the Lambda function is executed in.
    /// </summary>
    public Function()
    {

    }


    /// <summary>
    /// This method is called for every Lambda invocation. This method takes in an SQS event object and can be used 
    /// to respond to SQS messages.
    /// </summary>
    /// <param name="evnt"></param>
    /// <param name="context"></param>
    /// <returns></returns>
    public async Task FunctionHandler(SQSEvent evnt, ILambdaContext context)
    {
        foreach(var message in evnt.Records)
        {
            await ProcessMessageAsync(message, context);
        }
    }

    private async Task ProcessMessageAsync(SQSEvent.SQSMessage message, ILambdaContext context)
    {

        // deserialize response
        InsuranceResponseData? responseData = null;
        try
        {
            responseData = JsonSerializer.Deserialize<InsuranceResponseData>(message.Body);
        }
        catch (Exception ex)
        {
            // TODO: handle
            Console.WriteLine($"Error: {ex.Message}");
        }

        // validate data
        if (responseData == null || responseData.PatientId == null || responseData.HasInsurance == null)
        {
            Console.WriteLine("Error: Response message does not include vital fields");
            return; // TODO: confirm works for Task
        }
        
        // log data to cloudwatch
        if ((bool)responseData.HasInsurance)
        {
            Console.WriteLine($"Patient with ID {responseData.PatientId}:  PolicyNumber={responseData.PolicyData.PolicyNumber} Provider={responseData.PolicyData.Provider}");
        }
        else
        {
            Console.WriteLine($"Patient with ID {responseData.PatientId} does not have medical insurance.");
        }

    }
}