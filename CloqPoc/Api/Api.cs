using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System.Net.Http.Formatting;
using Newtonsoft.Json;
using System.Text;

namespace CloqPoc
{
   public static class Api
   {
      [FunctionName("PostCloq")]
      public static async Task<HttpResponseMessage> CloqPost(
         [HttpTrigger(AuthorizationLevel.Function, "post", Route = "cloq/{name}")] HttpRequestMessage req,
         [DurableClient] IDurableClient client,
         string name,
         ILogger logger)
      {
         var entity = await client.ReadEntityStateAsync<Cloq>(new EntityId(nameof(Cloq), name));
         if (await EntityExists(client, name))
         {
            return req.CreateResponse(HttpStatusCode.Conflict);
         }
         logger.LogInformation("Called PostCloq");
         await client.SignalEntityAsync<ICLoq>(name, x => x.Start());
         return req.CreateResponse(HttpStatusCode.Accepted);
      }

      [FunctionName("GetCloq")]
      public static async Task<HttpResponseMessage> CloqGet(
         [HttpTrigger(AuthorizationLevel.Function, "get", Route = "cloq/{name}")] HttpRequestMessage req,
         [DurableClient] IDurableClient client,
         string name,
         ILogger logger)
      {
         logger.LogInformation("Called GetCloq");
         var val = await client.ReadEntityStateAsync<Cloq>(new EntityId(nameof(Cloq), name));

         if (!val.EntityExists)
         {
            return req.CreateResponse(HttpStatusCode.NotFound);
         }

         return new HttpResponseMessage(HttpStatusCode.OK)
         {
            Content = new StringContent(JsonConvert.SerializeObject(val.EntityState), Encoding.UTF8, "application/json"),
         };
      }

      [FunctionName("DeleteCloq")]
      public static async Task<HttpResponseMessage> CloqDelete(
         [HttpTrigger(AuthorizationLevel.Function, "delete", Route = "cloq/{name}")] HttpRequestMessage req,
         [DurableClient] IDurableClient client,
         string name,
         ILogger logger)
      {
         logger.LogInformation("Called GetCloq");

         if (!await EntityExists(client, name))
         {
            return req.CreateResponse(HttpStatusCode.NotFound);
         }

         // TerminateAsync seems to aggressive (post will no longer work with same Entity ID)
         // Purge does what we want, except that it doesn't clear future "SignalEntity" calls
         // Because of this the Cloq entity will exist again within 5 seconds in our case... 
         // Do we keep a "deleted" state in our entity, or is there a smarter way? 
         await client.PurgeInstanceHistoryAsync($"@cloq@{name}");
         //await client.TerminateAsync($"@cloq@{name}", "reason");

         return req.CreateResponse(HttpStatusCode.Accepted);
      }

      private static async Task<bool> EntityExists(IDurableClient client, string name)
      {
         var entity = await client.ReadEntityStateAsync<Cloq>(new EntityId(nameof(Cloq), name));
         return entity.EntityExists;
      }
   }
}
