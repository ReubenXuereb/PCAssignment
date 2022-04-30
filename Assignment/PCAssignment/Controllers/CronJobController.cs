using Google.Cloud.Firestore;
using Google.Cloud.Storage.V1;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;
using UserModel = PCAssignment.Models.User;
using FileModel = PCAssignment.Models.Files;
using System;

namespace PCAssignment.Controllers
{
    public class CronJobController : Controller
    {
        string project = "";
        string bucketName = "";

        public CronJobController(IConfiguration configuration)
        {
            project = configuration["project"];
            bucketName = configuration["bucketName"];
        }

        public async Task<IActionResult> deleteFile()
        {
            var myStorage = StorageClient.Create();
            FirestoreDb db = FirestoreDb.Create(project);

            Query users = db.Collection("users");
            QuerySnapshot usersSnapshot = await users.GetSnapshotAsync();

            if(usersSnapshot != null)
            {
                foreach(DocumentSnapshot docSnapshot in usersSnapshot.Documents)
                {
                    UserModel user = docSnapshot.ConvertTo<UserModel>();
                    Query files = db.Collection("users").Document(user.Email).Collection("files");
                    QuerySnapshot filesSnapshot = await files.GetSnapshotAsync();

                    foreach(DocumentSnapshot fileSnapshot in filesSnapshot.Documents)
                    {
                        var fileDetails = fileSnapshot.ConvertTo<FileModel>();
                        DateTime dateSent = fileDetails.DateSent.ToDateTime();
                        int daysGoneBy = DateTime.UtcNow.Subtract(dateSent).Days;

                        if(daysGoneBy > 1)
                        {
                            string extend = System.IO.Path.GetExtension(fileDetails.AttachmentUri);
                            myStorage.DeleteObject(bucketName, fileDetails.Id + extend);

                            DocumentReference docRef = db.Collection("users").Document(user.Email).Collection("files").Document(fileDetails.Id);
                            await docRef.DeleteAsync();
                        }
                    }
                }
            }
            return Ok("Bucket Cleared");
        }

    }
}
