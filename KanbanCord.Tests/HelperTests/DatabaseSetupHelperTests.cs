using KanbanCord.Core.Helpers;
using Mongo2Go;
using MongoDB.Driver;

namespace KanbanCord.Tests.HelperTests
{
    public class DatabaseSetupHelperTests : IDisposable
    {
        private readonly MongoDbRunner _runner;
        private readonly IMongoDatabase _database;

        public DatabaseSetupHelperTests()
        {
            _runner = MongoDbRunner.Start();

            _database = new MongoClient(_runner.ConnectionString)
                .GetDatabase("KanbanCord");
        }

        [Fact]
        public async Task CreateRequiredCollectionsAsync_ShouldCreateMissingCollections()
        {
            // Act
            await DatabaseSetupHelper.CreateRequiredCollectionsAsync(_database, new NoOpLogger());

            // Assert
            var collectionNames = await _database.ListCollectionNamesAsync();
            var collectionList = await collectionNames.ToListAsync();

            var requiredCollections = Enum.GetValues(typeof(RequiredCollections))
                .Cast<RequiredCollections>()
                .Select(x => x.ToString())
                .ToArray();

            foreach (var collection in requiredCollections)
            {
                Assert.Contains(collection, collectionList);
            }
        }

        [Fact]
        public async Task CreateRequiredCollectionsAsync_ShouldNotCreateExistingCollections()
        {
            var requiredCollections = Enum.GetValues(typeof(RequiredCollections))
                .Cast<RequiredCollections>()
                .Select(x => x.ToString())
                .ToArray();

            foreach (var collectionName in requiredCollections)
            {
                await _database.CreateCollectionAsync(collectionName);
            }

            // Act
            await DatabaseSetupHelper.CreateRequiredCollectionsAsync(_database, new NoOpLogger());

            // Assert
            var collectionNames = await _database.ListCollectionNamesAsync();
            var collectionList = await collectionNames.ToListAsync();

            foreach (var collectionName in requiredCollections)
            {
                Assert.Contains(collectionName, collectionList);
            }
        }

        public void Dispose()
        {
            _runner.Dispose();
        }

        // A simple no-op logger to satisfy the ILogger parameter
        private class NoOpLogger : Microsoft.Extensions.Logging.ILogger
        {
            public IDisposable BeginScope<TState>(TState state) => throw new NotImplementedException();
            public bool IsEnabled(Microsoft.Extensions.Logging.LogLevel logLevel) => true;
            public void Log<TState>(Microsoft.Extensions.Logging.LogLevel logLevel, 
                Microsoft.Extensions.Logging.EventId eventId, TState state, 
                Exception? exception, Func<TState, Exception, string> formatter) { }
        }
    }
}
