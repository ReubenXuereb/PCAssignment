using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Google.Cloud.Functions.Framework;
using Google.Events.Protobuf.Cloud.PubSub.V1;
using CloudNative.CloudEvents;
using RestSharp;
using Newtonsoft.Json;
using Microsoft.Extensions.Logging;
using Google.Cloud.Storage.V1;
using Google.Cloud.Firestore;
using CloudProject.Models;

namespace pcAssignmentFunction
{
   public class Function : ICloudEventFunction<MessagePublishedData>
    {
        private readonly ILogger _logger;

        public Function(ILogger<Function> logger) =>
            _logger = logger;

        public Task HandleAsync(CloudEvent cloudEvent, MessagePublishedData data, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Accessed the Cloud Function");
            string jsonData = data.Message?.TextData;   
            _logger.LogInformation("Message data:" + jsonData);

            dynamic docObject = JsonConvert.DeserializeObject(jsonData);

            string text = docObject.Text.ToString();
            _logger.LogInformation("Text: " + text);

            string dateSent = docObject.DateSent.ToString();
            _logger.LogInformation("Text was sent at: " + dateSent);

            string recipientEmail = docObject.Recipient.ToString();
            _logger.LogInformation("Recieved by user with email: " + recipientEmail);

            string docAttachment = docObject.AttachmentUri.ToString();
            _logger.LogInformation("URI of doc:  " + docAttachment);

            RestClient client = new RestClient("https://getoutpdf.com/api/convert/document-to-pdf");
            RestRequest request = new RestRequest();

            var api_key = "e7f2936fbdd9132cb7a8e3f01ddf4ee43b6ae228b82344a3f155ebb1a99bfcf1";   
            request.AddParameter("api_key", api_key);
            request.AddParameter("document", docAttachment);
            request.Method = Method.Post;
            
            Task<RestResponse> t = client.PostAsync(request);
            t.Wait();

            var response = t.Result;

            if(response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                _logger.LogInformation("Result of Response:" + response.Content);
                dynamic jsonConvert = JsonConvert.DeserializeObject(response.Content);

                string attachmentName = Guid.NewGuid().ToString();
                _logger.LogInformation("Guid Attachment File Name: " + attachmentName);
                
                string base64PDF = jsonConvert["pdf_base64"];
                _logger.LogInformation("PDF base64: " + base64PDF);

                Byte[] ConvertToFile = Convert.FromBase64String(base64PDF);
                Stream stream = new MemoryStream(ConvertToFile);
                _logger.LogInformation("PDF base64: " + stream);

                var clientStorage = StorageClient.Create();

                using(Stream FileToUpload = stream)
                {
                    clientStorage.UploadObject("pcassignment", attachmentName + ".pdf", null, FileToUpload);
                    _logger.LogInformation("Object Uploaded");
                }

                string pdfURI = "https://storage.googleapis.com/pcassignment/" + attachmentName + ".pdf";
                _logger.LogInformation("Attatchment Uri: " + pdfURI);
                _logger.LogInformation("Updated Database");

                FunctionModel functionModel = new FunctionModel();
                functionModel.Id = attachmentName;
                functionModel.Text = text;
                functionModel.Recipient = recipientEmail;
                functionModel.DateSent = functionModel.DateSent;
                functionModel.AttachmentUri = pdfURI;

                FirestoreDb db = FirestoreDb.Create("prefab-kit-340811");
                DocumentReference docRef = db.Collection("users").Document(recipientEmail).Collection("files").Document(attachmentName);
                docRef.SetAsync(functionModel);

                _logger.LogInformation("Conversion Completed Successfully");
            }
            return Task.CompletedTask;
        }
    }
}
