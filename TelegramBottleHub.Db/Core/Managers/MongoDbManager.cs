using Microsoft.Extensions.Configuration;
using MongoDB.Driver;
using TelegramBottleHub.General.Managers.Core;

namespace TelegramBottleHub.Db.Core.Managers
{
    public sealed class MongoDbManager : ManagerCore
    {
        private const string MongoDbConnectionStringKey = "BottleDbConnectionString";
        private const string MongoDbName = "BottleDb";

        private MongoClient _mongoClient;

        private MongoClient MongoClient
        {
            get
            {
                return _mongoClient ??
                    (_mongoClient = new MongoClient(Configuration.GetConnectionString(MongoDbConnectionStringKey)));
            }
        }

        public IMongoDatabase Database => MongoClient.GetDatabase(MongoDbName);

        public MongoDbManager(IConfigurationRoot configuration) : base(configuration)
        {            
        }

        public override void Dispose()
        {            
        }
    }
}
