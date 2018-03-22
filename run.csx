#r "Microsoft.WindowsAzure.Storage"
#r "Newtonsoft.Json"

using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using Microsoft.WindowsAzure.Storage; // Namespace for CloudStorageAccount
using Microsoft.WindowsAzure.Storage.Blob; // Namespace for Blob storage types
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Microsoft.WindowsAzure.Storage.Queue;



public async static void Run(CloudBlockBlob myBlob, string name, TraceWriter log)
{
    log.Info($"C# Blob trigger function Processed blob\n Name:{name} \n");
    await MakeAnalysisRequest(myBlob, log);
    log.Info("Async Done.");
}

static async Task MakeAnalysisRequest(CloudBlockBlob myBlob, TraceWriter log)
{
    string subscriptionKey = "";
    string uriBase = "";

    HttpClient client = new HttpClient();
    // Request headers.
    client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", subscriptionKey);

    // Request parameters. A third optional parameter is "details".
    string requestParameters = "visualFeatures=Categories,Description,Color&language=en";

    // Assemble the URI for the REST API Call.
    string uri = uriBase + "?" + requestParameters;
    HttpResponseMessage response;
    

    log.Info("Start...");
    // Save blob contents to a file.
    using (var memoryStream = new MemoryStream())
    {
        myBlob.DownloadToStream(memoryStream);
        memoryStream.Seek(0, SeekOrigin.Begin);
        byte[] byteData = memoryStream.ToArray();
        using (ByteArrayContent content = new ByteArrayContent(byteData))
        {
            // This example uses content type "application/octet-stream".
            // The other content types you can use are "application/json" and "multipart/form-data".
            content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

            // Execute the REST API call.
            response = await client.PostAsync(uri, content);

            // Get the JSON response.
            string contentString = await response.Content.ReadAsStringAsync();

            // Display the JSON response.
            // Create a message and add it to the queue.
            //CloudQueueMessage message = new CloudQueueMessage(JsonPrettyPrint(contentString));
            //queue.AddMessage(message);                    
            //log.Info("\nResponse:\n");
            //log.Info(JsonPrettyPrint(contentString));
            dynamic jsonData = JObject.Parse(contentString);
            log.Info("I saw: " + jsonData["description"]["captions"]);

            // Save the information to Queue
            // This has to be encrypted. 
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse("DefaultEndpointsProtocol=https;AccountName=skyeyeshandler2017;AccountKey=DsYGpc9HuiKB0utQcKeUjyhY3mdfY1PKDuhBwam5X/QUZLffn+et/+pfHJhH8L0+SVBUhd4DfrRdn1caKLf2hA==");

            // Create the queue client.
            CloudQueueClient queueClient = storageAccount.CreateCloudQueueClient();

            // Retrieve a reference to a queue.
            CloudQueue queue = queueClient.GetQueueReference("sms");

            // Create the queue if it doesn't already exist.
            queue.CreateIfNotExists();

            // Create a message and add it to the queue.
            CloudQueueMessage message = new CloudQueueMessage(jsonData["description"]["captions"]);
            queue.AddMessage(message);
        }
    }
}
