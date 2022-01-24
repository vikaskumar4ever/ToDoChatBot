using Microsoft.Azure.Cosmos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace ToDoCoreBot.Utilities
{
    public class CosmosDBClient
    { 
        // The Cosmos client instance
        private CosmosClient cosmosClient;

        // The database we will create
        private Database database;

        // The container we will create.
        private Container container;

 
        public async Task GetStartedDemoAsync(string EndpointUri, string PrimaryKey, string databaseId, string containerId, string partitionKey)
        {
            // Create a new instance of the Cosmos Client
            this.cosmosClient = new CosmosClient(EndpointUri, PrimaryKey, new CosmosClientOptions() { ApplicationName = "CosmosDBDotnetQuickstart" });
            await this.CreateDatabaseAsync(databaseId);
            await this.CreateContainerAsync(containerId, partitionKey);
            //await this.ScaleContainerAsync();
            //await this.AddItemsToContainerAsync();
            //await this.QueryItemsAsync();
            //await this.ReplaceToDoTaskItemAsync();
            //await this.DeleteToDoTaskItemAsync();
            //await this.DeleteDatabaseAndCleanupAsync();
        }
        // </GetStartedDemoAsync>

        // <CreateDatabaseAsync>
        /// <summary>
        /// Create the database if it does not exist
        /// </summary>
        private async Task CreateDatabaseAsync(string databaseId)
        {
            // Create a new database
            this.database = await this.cosmosClient.CreateDatabaseIfNotExistsAsync(databaseId);
            Console.WriteLine("Created Database: {0}\n", this.database.Id);
        }

        private async Task CreateContainerAsync(string containerId, string partitionKey)
        {
            // Create a new container
            this.container = await this.database.CreateContainerIfNotExistsAsync(containerId, partitionKey, 400);
            Console.WriteLine("Created Container: {0}\n", this.container.Id);
        }

        public async Task<int> AddItemsToContainerAsync(string userId, string task)
        {
            // Create a ToDoTask object for the Andersen ToDoTask
            ToDoTask todotask = new ToDoTask
            {
                Id = userId,
                Task = task,
            };

            try
            {
                // Read the item to see if it exists.  
                ItemResponse<ToDoTask> todotaskResponse = await this.container.ReadItemAsync<ToDoTask>(todotask.Id, new PartitionKey(todotask.Task));
                Console.WriteLine("Item in database with id: {0} already exists\n", todotaskResponse.Resource.Id);
                return -1;
            }
            catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                // Create an item in the container representing the Andersen ToDoTask. Note we provide the value of the partition key for this item, which is "Andersen"
                ItemResponse<ToDoTask> todotaskResponse = await this.container.CreateItemAsync<ToDoTask>(todotask, new PartitionKey(todotask.Task));

                // Note that after creating the item, we can access the body of the item with the Resource property off the ItemResponse. We can also access the RequestCharge property to see the amount of RUs consumed on this request.
                Console.WriteLine("Created item in database with id: {0} Operation consumed {1} RUs.\n", todotaskResponse.Resource.Id, todotaskResponse.RequestCharge);
                return +1;
            }

        }
        

        public async Task<bool> CheckNewUserIdAsync(string userId, string EndpointUri, string PrimaryKey, string databaseId, string containerId, string partitionKey)
        {
            await GetStartedDemoAsync(EndpointUri, PrimaryKey, databaseId, containerId, partitionKey);
            var sqlQueryText = $"SELECT * FROM c WHERE c.id = '{userId}'";

            Console.WriteLine("Running query: {0}\n", sqlQueryText);

            QueryDefinition queryDefinition = new QueryDefinition(sqlQueryText);
            FeedIterator<ToDoTask> queryResultSetIterator = this.container.GetItemQueryIterator<ToDoTask>(queryDefinition);

            while (queryResultSetIterator.HasMoreResults)
            {
                FeedResponse<ToDoTask> currentResultSet = await queryResultSetIterator.ReadNextAsync();
                if (currentResultSet.Count > 0)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            return false;
        }

        public async Task<List<ToDoTask>> QueryItemsAsync(string userId, string EndpointUri, string PrimaryKey, string databaseId, string containerId, string partitionKey)
        {
            await GetStartedDemoAsync(EndpointUri, PrimaryKey, databaseId, containerId, partitionKey);

            var sqlQueryText = $"SELECT * FROM c where c.id ='{userId}' order by c._ts desc";

            Console.WriteLine("Running query: {0}\n", sqlQueryText);

            QueryDefinition queryDefinition = new QueryDefinition(sqlQueryText);
            FeedIterator<ToDoTask> queryResultSetIterator = this.container.GetItemQueryIterator<ToDoTask>(queryDefinition);

            List<ToDoTask> toDoTasks = new List<ToDoTask>();

            while (queryResultSetIterator.HasMoreResults)
            {
                FeedResponse<ToDoTask> currentResultSet = await queryResultSetIterator.ReadNextAsync();
                foreach (ToDoTask toDoTask in currentResultSet)
                {
                    toDoTasks.Add(toDoTask);
                    Console.WriteLine("\tRead {0}\n", toDoTask);
                }
            }
            return toDoTasks;
        }

        public async Task<List<ToDoTask>> QueryItemsAsync(string userId)
        {

            var sqlQueryText = $"SELECT * FROM c where c.id ='{userId}' order by c._ts asc";

            Console.WriteLine("Running query: {0}\n", sqlQueryText);

            QueryDefinition queryDefinition = new QueryDefinition(sqlQueryText);
            FeedIterator<ToDoTask> queryResultSetIterator = this.container.GetItemQueryIterator<ToDoTask>(queryDefinition);

            List<ToDoTask> toDoTasks = new List<ToDoTask>();

            while (queryResultSetIterator.HasMoreResults)
            {
                FeedResponse<ToDoTask> currentResultSet = await queryResultSetIterator.ReadNextAsync();
                foreach (ToDoTask toDoTask in currentResultSet)
                {
                    toDoTasks.Add(toDoTask);
                    Console.WriteLine("\tRead {0}\n", toDoTask);
                }
            }
            return toDoTasks;
        }

        public async Task<bool> DeleteTaskItemAsync(string partitionKey, string id)
        {
            var partitionKeyValue = partitionKey;
            var userId = id;

            // Delete an item. Note we must provide the partition key value and id of the item to delete
            try
            {
                ItemResponse<ToDoTask> toDoTaskResponse = await this.container.DeleteItemAsync<ToDoTask>(userId, new PartitionKey(partitionKeyValue));
                Console.WriteLine("Deleted Task [{0},{1}]\n", partitionKeyValue, userId);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return false;
            }
            
        }

    }
}
