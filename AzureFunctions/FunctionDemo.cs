using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using System;
using Microsoft.Azure.Documents;
using System.Collections.Generic;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.Documents.Linq;
using System.IO;
using Newtonsoft.Json;
namespace AzureFunctions
{
    public class Rating
    {
        public string id { get; set; }
        public string userId { get; set; }
        public string productId { get; set; }
        public DateTime timestamp { get; set; }
        public string locationName { get; set; }
        public int rating { get; set; }
        public string userNotes { get; set; }


    }

    public class RatingPost
    {
        public string userId { get; set; }
        public string productId { get; set; }
        public string locationName { get; set; }
        public int rating { get; set; }
        public string userNotes { get; set; }


    }
    public static class FunctionDemo
    {
        //Reusable instance of DocumentClient which represents the connection to a DocumentDB endpoint
        private static DocumentClient client;

        [FunctionName("FnGetProduct")]
        public static async Task<HttpResponseMessage> Run([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)]HttpRequestMessage req, TraceWriter log)
        {
            log.Info("C# HTTP trigger function processed a request.");

            // parse query parameter
            string productId = req.GetQueryNameValuePairs()
                .FirstOrDefault(q => string.Compare(q.Key, "productId", true) == 0)
                .Value;

            if (productId == null)
            {
                // Get request body
                dynamic data = await req.Content.ReadAsAsync<object>();
                productId = data?.productId;
            }

            return productId == null
                ? req.CreateResponse(HttpStatusCode.BadRequest, "Please pass a name on the query string or in the request body")
                : req.CreateResponse(HttpStatusCode.OK, "The product name for your product id " + productId + " is Starfruit Explosion.");
        }

        [FunctionName("CreateRating")]
        public static async Task<HttpResponseMessage> CreateRating([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)]HttpRequestMessage req, [DocumentDB("openhackdb", "openhackcollection", CreateIfNotExists = true, ConnectionStringSetting = "CosmosDB")] IAsyncCollector<Rating> documents, TraceWriter log)
        {
            log.Info("C# HTTP trigger function processed a request.");

            // parse query parameter
            dynamic postdata = await req.Content.ReadAsStringAsync();
            RatingPost payload = JsonConvert.DeserializeObject<RatingPost>(postdata);






            bool blnValid = true;
            string strResponse = "";
            if ((Convert.ToInt32(payload.rating) < 0) && (Convert.ToInt32(payload.rating) > 5))
            {
                blnValid = false;
                strResponse = "rating should be between 0 and 5";
            }

            if (blnValid == false)
            {
                return req.CreateResponse(HttpStatusCode.BadRequest, strResponse);
            }


            string id = Guid.NewGuid().ToString();
            DateTime timestamp = DateTime.UtcNow;

            var web_response = new WebClient().DownloadString("https://serverlessohuser.trafficmanager.net/api/GetUser?userId=" + payload.userId.ToString());

            if (web_response == "Please pass a valid userId on the query string")
            {
                return req.CreateResponse(HttpStatusCode.BadRequest, "userId not found");
            }
            if (web_response == "User does not exist")
            {
                return req.CreateResponse(HttpStatusCode.NotFound, "User does not exist");
            }

            /*
            User objUser = JsonConvert.DeserializeObject<User>(web_response);
            userId = objUser.userId;
            objUser = null;
            */
            web_response = new WebClient().DownloadString("https://serverlessohproduct.trafficmanager.net/api/GetProduct?productId=" + payload.productId.ToString());

            if (web_response == "Please pass a valid productId on the query string")
            {
                return req.CreateResponse(HttpStatusCode.NotFound, "productId not found");
            }



            /*
            Product objProduct = JsonConvert.DeserializeObject<Product>(web_response);
            productId = objProduct.productId;
            objUser = null;
            */

            Rating objRating = new Rating();
            objRating.id = id;
            objRating.userId = payload.userId.ToString();
            objRating.productId = payload.productId.ToString();
            objRating.timestamp = timestamp;
            objRating.locationName = payload.locationName.ToString();
            objRating.rating = Convert.ToInt32(payload.rating);
            objRating.userNotes = payload.userNotes.ToString();

            string JSONResponse = JsonConvert.SerializeObject(objRating);
            dynamic data = JsonConvert.DeserializeObject(JSONResponse);
            await documents.AddAsync(objRating);


            return req.CreateResponse(HttpStatusCode.OK, JSONResponse);



        }



        [FunctionName("GetRating")]
        public static HttpResponseMessage GetRating(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)]HttpRequestMessage req,
            [DocumentDB(
                databaseName: "openhackdb",
                collectionName: "openhackcollection",
                ConnectionStringSetting = "CosmosDB",
                Id = "{Query.id}")] Rating toDoItem,
            TraceWriter log)
        {
            log.Info("C# HTTP trigger function processed a request.");
            if (toDoItem == null)
            {
                return req.CreateResponse(HttpStatusCode.NotFound, "ratingId not found");
            }
            else
            {
                return req.CreateResponse(HttpStatusCode.OK, toDoItem);
            }

        }

        [FunctionName("GetRatings")]
        public static HttpResponseMessage GetRatings(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = "GetRatings/{userId}")]
                HttpRequestMessage req,
            [DocumentDB(
                databaseName: "openhackdb",
                collectionName: "openhackcollection",
                ConnectionStringSetting = "CosmosDB",
                SqlQuery = "SELECT * FROM c where c.userId = {userId}")]
                IEnumerable<Rating> toDoItems,
            TraceWriter log)
        {
            log.Info("Stating New Update: C# HTTP trigger function processed a request. Changes done for staging only");

            int intCount = toDoItems.ToArray().Length;

            if (intCount == 0)
            {
                return req.CreateResponse(HttpStatusCode.NotFound, "userId not found");
            }
            else
            {
                return req.CreateResponse(HttpStatusCode.OK, JsonConvert.SerializeObject(toDoItems));
            }
        }


    }
}
