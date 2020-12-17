using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Newtonsoft.Json;
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace CloqPoc
{
   public interface ICLoq
   {
      void Start();
      void Tick();
   }

   [JsonObject(MemberSerialization.OptIn)]
   public class Cloq : ICLoq
   {
      private static ILogger _logger;
      [JsonProperty("value")]
      public int CurrentValue { get; set; }

      public void Start()
      {
         CurrentValue = 0;
         Entity.Current.SignalEntity<ICLoq>(Entity.Current.EntityId, DateTime.Now.AddSeconds(5), e => e.Tick());
      }

      public void Tick()
      {
         // A minute elapsed
         CurrentValue++;
         _logger.LogInformation($"{CurrentValue} ticks for {Entity.Current.EntityKey}");

         // Setup next Tick
         Entity.Current.SignalEntity<ICLoq>(Entity.Current.EntityId, DateTime.Now.AddSeconds(5), e => e.Tick());
      }

      [FunctionName(nameof(Cloq))]
      public static Task Run([EntityTrigger] IDurableEntityContext ctx, ILogger logger)
      {
         _logger = logger;
         return ctx.DispatchAsync<Cloq>();
      }
   }
}
