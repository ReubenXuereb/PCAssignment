using Google.Cloud.SecretManager.V1;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using PCAssignment.Models;
using StackExchange.Redis;
using System;
using System.Collections.Generic;

namespace PCAssignment.Controllers
{
    
    public class AdminController : Controller
    {
        string connectionString = "";
        string projectId = "";

        public AdminController(IConfiguration configuration)
        {
            //connectionString = configuration["redis"];
            //getting redis secret key from cloud
            projectId = configuration["project"];
            SecretManagerServiceClient client = SecretManagerServiceClient.Create();
            SecretVersionName secretVersionName = new SecretVersionName(projectId, "MySecrets", "1");
            AccessSecretVersionResponse result = client.AccessSecretVersion(secretVersionName);
            String payload = result.Payload.Data.ToStringUtf8();
            dynamic myObject = JsonConvert.DeserializeObject(payload);
            string redis = Convert.ToString(myObject["redis"]);

            connectionString = redis;
        }




        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Create(Credits credit)
        {
            try
            {
                ConnectionMultiplexer cm = ConnectionMultiplexer.Connect(connectionString);
                var db = cm.GetDatabase();
                var myCredits = db.StringGet("credits");

                List<Credits> creditsList = new List<Credits>();
                if (myCredits.IsNullOrEmpty)
                {
                    creditsList = new List<Credits>();
                }
                else
                {
                    creditsList = JsonConvert.DeserializeObject<List<Credits>>(myCredits);
                }

                creditsList.Add(credit);

                var myJsonString = JsonConvert.SerializeObject(creditsList);
                db.StringSet("credits", myJsonString);

                ViewBag.Message = "Saved in cache";
            }
            catch (Exception error)
            {
                ViewBag.Error = "Not saved in cache";
            }

            return View();
        }
    }
}
