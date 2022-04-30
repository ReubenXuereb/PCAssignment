using Google.Cloud.Diagnostics.AspNetCore3;
using Google.Cloud.Firestore;
using Google.Cloud.PubSub.V1;
using Google.Cloud.Storage.V1;
using Google.Protobuf;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using PCAssignment.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UserModel = PCAssignment.Models.User;

namespace PCAssignment.Controllers
{
    public class UsersController : Controller
    {
        string project_id = "";
        string bucketName = "";
        private readonly ILogger<UsersController> _logger;
        private readonly IExceptionLogger _exceptionLogger;

        public UsersController(IConfiguration configuration, ILogger<UsersController> logger, [FromServices] IExceptionLogger exceptionLogger)
        {
            project_id = configuration["project"];
            bucketName = configuration["bucketName"];
            _logger = logger;
            _exceptionLogger = exceptionLogger;
        }
        

        //user details
        //gets details

        [Authorize]
        public async Task<IActionResult> Index()
        {
     
            FirestoreDb db = FirestoreDb.Create(project_id);

            DocumentReference docRef = db.Collection("users").Document(User.Claims.ElementAt(4).Value);
            DocumentSnapshot snapshot = await docRef.GetSnapshotAsync();
            UserModel myUser = new UserModel();
            myUser.Email = User.Claims.ElementAt(4).Value;
            if (snapshot.Exists)
            {
                //Console.WriteLine("Document data for {0} document:", snapshot.Id);
                //Dictionary<string, object> userFromDb = snapshot.ToDictionary();

                //myUser.FirstName = userFromDb["firstName"].ToString();
                //myUser.LastName = userFromDb["lastName"].ToString();

                myUser = snapshot.ConvertTo<UserModel>();
            }
            return View(myUser);
        }



        //Add user details
        //posts details
        [Authorize]
        public async Task<IActionResult> Register(UserModel user, int dropDownCredits)
        {
            FirestoreDb db = FirestoreDb.Create(project_id);
            DocumentReference docRef = db.Collection("users").Document(User.Claims.ElementAt(4).Value);

            user.Email = User.Claims.ElementAt(4).Value;
            user.AvailableCredits += dropDownCredits;
            _logger.LogInformation("Added more Credits");
            await docRef.SetAsync(user);

            return RedirectToAction("Index");
        }


        [HttpGet]
        [Authorize]
        public IActionResult Send()
        {
            return View();
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Send(Files file, IFormFile attachment, [FromServices] IExceptionLogger exceptionLogger)
        {
            
            file.Id = Guid.NewGuid().ToString();
    
            try
            {
                if (attachment != null)
                {
                    var storage = StorageClient.Create();
                    using (Stream myUploadingFile = attachment.OpenReadStream())
                    {
                        storage.UploadObject(bucketName, file.Id + System.IO.Path.GetExtension(attachment.FileName), null, myUploadingFile);
                    }

                    file.AttachmentUri = $"https://storage.googleapis.com/{bucketName}/{file.Id + System.IO.Path.GetExtension(attachment.FileName)}";

                    FirestoreDb db = FirestoreDb.Create(project_id);
                    DocumentReference docRef = db.Collection("users").Document(User.Claims.ElementAt(4).Value).Collection("files").Document(file.Id);
                    await docRef.SetAsync(file);


                    //fix to convert pdf properly
                    string fileDoc = "";
                    if (attachment.Length > 0)
                    {
                        using (var memoryStream = new MemoryStream())
                        {
                            attachment.CopyTo(memoryStream);
                            var fileToBytes = memoryStream.ToArray();
                            fileDoc = Convert.ToBase64String(fileToBytes);
                        }
                    }

                    file.AttachmentUri = fileDoc;

                    //pub/sub part of assignment
                    TopicName topic = new TopicName(project_id, "pcassignment");

                    PublisherClient client = PublisherClient.Create(topic);

                    string mail_serialized = JsonConvert.SerializeObject(file);

                    PubsubMessage message = new PubsubMessage
                    {
                        Data = ByteString.CopyFromUtf8(mail_serialized)
                    };

                    //subtracting tokens
                    DocumentReference docRef2 = db.Collection("users").Document(User.Claims.ElementAt(4).Value);
                    DocumentSnapshot snapshot = await docRef2.GetSnapshotAsync();
                    UserModel userModel = new UserModel();

                    if (snapshot.Exists)
                    {
                        userModel = snapshot.ConvertTo<UserModel>();
                    }

                    userModel.AvailableCredits -= 1;
                    _logger.LogInformation("1 credit has been removed");

                    await docRef2.SetAsync(userModel);

                    await client.PublishAsync(message);
                }
            }catch (Exception ex)
            {
                _exceptionLogger.Log(ex);
                throw new Exception("Failed to upload file");
            }

            return RedirectToAction("List");

            //FirestoreDb db = FirestoreDb.Create(project_id);
            //DocumentReference docRef = db.Collection("users").Document(User.Claims.ElementAt(4).Value).Collection("files").Document(file.Id);
            //await docRef.SetAsync(file);


            ////pub/sub part of assignment
            //TopicName topic = new TopicName(project_id, "pcassignment");

            //PublisherClient client = PublisherClient.Create(topic);

            //string mail_serialized = JsonConvert.SerializeObject(file);

            //PubsubMessage message = new PubsubMessage
            //{
            //    Data = ByteString.CopyFromUtf8(mail_serialized)
            //};

            //await client.PublishAsync(message);

        }

        [Authorize]
        public async Task<IActionResult> List()
        {
            FirestoreDb db = FirestoreDb.Create(project_id);

            Query allFilesQuery = db.Collection("users").Document(User.Claims.ElementAt(4).Value).Collection("files").OrderByDescending("DateSent");
            QuerySnapshot allFilesQuerySnapshot = await allFilesQuery.GetSnapshotAsync();
            List<Files> files = new List<Files>();

            foreach (DocumentSnapshot documentSnapshot in allFilesQuerySnapshot.Documents)
            {
                files.Add(documentSnapshot.ConvertTo<Files>());
            }

            return View(files);
        }


    }
}
