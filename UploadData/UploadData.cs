using Amazon.S3;
using Amazon.S3.Model;
using System.Text.RegularExpressions;
using Amazon;

/*
 * Code by Thien and Andrew, adapted from Project 1
 */

namespace Project2;
class UploadData
{
	// global application variables
	private const string BucketName = "andrew-thien-project-2-bucket";
	private static readonly RegionEndpoint BucketRegion = RegionEndpoint.USEast1;
	private static IAmazonS3? S3Client;

	static void Main(string[] args)
	{
		if (args.Length == 0)
			throw new ArgumentException("Missing file argument.");
		Console.WriteLine(args[0]);  // debug print to console to show the path of the file being uploaded to s3
		S3Client = new AmazonS3Client(BucketRegion);
		PutObjectWithTagsTestAsync(args).Wait();
	}
	static async Task PutObjectWithTagsTestAsync(string[] args)
	{
		try
		{
			// parse command line arguments
			string[] splitString = Regex.Split(args[0], @"\\");

			// compile request for adding the file to s3
			var putRequest = new PutObjectRequest
			{
				BucketName = BucketName,
				Key = splitString[splitString.Length - 1],
				FilePath = args[0]
			};
			// make the request
			PutObjectResponse response = await S3Client.PutObjectAsync(putRequest);
			Console.WriteLine(response.ToString());

		}
		catch (AmazonS3Exception e)
		{
			Console.WriteLine(
					"Error encountered ***. Message:'{0}' when writing an object"
					, e.Message);
		}
		catch (Exception e)
		{
			Console.WriteLine(
				"Encountered an error. Message:'{0}' when writing an object"
				, e.Message);
		}
	}
}
