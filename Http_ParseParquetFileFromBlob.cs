using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Azure.Storage.Blobs;
using Parquet.Data;
using Parquet;
using System.Collections.Generic;

namespace Ndxc.Blog
{
    public static class Http_ParseParquetFileFromBlob
    {
        [FunctionName("Http_ParseParquetFileFromBlob")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            // Connect to Blob Storage using a connection string
            // Assumes there is a container called 'files'
            string connectionString = "<CONNECTION STRING>";
            BlobContainerClient container = new BlobContainerClient(connectionString, "files");

            // Initialise our list of objects
            List<UserData> userDataList = new List<UserData>();

            // Loop through each of the files in the container
            foreach (var file in container.GetBlobs())
            {

                // Initialise a BlobClient so we can worj with the named file
                var blockBlob = container.GetBlobClient(file.Name);

                // Initialise a unique temporary path to hold the Parquet file
                var tempPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

                // Download the file to the temporary path
                using (var fileStream = File.OpenWrite(tempPath + blockBlob.Name))
                {
                    blockBlob.DownloadTo(fileStream);
                }

                // Open file stream
                using (Stream fileStream = File.OpenRead(tempPath + blockBlob.Name))
                {
                    // Open parquet file reader
                    using (var parquetReader = new ParquetReader(fileStream))
                    {

                        // Enumerate through row groups in this file
                        for (int i = 0; i < parquetReader.RowGroupCount; i++)
                        {
                            // Create row group reader
                            using (ParquetRowGroupReader groupReader = parquetReader.OpenRowGroupReader(i))
                            {

                                // Deserialize the rows into an array
                                UserData[] v1structures = ParquetConvert.Deserialize<UserData>(fileStream, i);

                                // Add each item of our array to our overall list
                                foreach (var row in v1structures)
                                {
                                    userDataList.Add(row);
                                }
                            }
                        }
                    }
                }

            }

            // Check we have rows
            if (userDataList.Count > 0)
            {

                // Return the list as a JSON array
                string json = JsonConvert.SerializeObject(userDataList.ToArray(), Formatting.Indented);

                return new OkObjectResult(json);
            }
            else
            {

                return new BadRequestResult();
            }        
        }
    }

    //  Class that represents the Parquet data
    //  Members are public as NewtonSoft.JSON requires them to be for serialization
    class UserData {

        public DateTimeOffset? registration_dttm { get; set; }
        public int? id { get; set; }
        public string first_name { get; set; }
        public string last_name { get; set; }
        public string email { get; set; }
        public string gender { get; set; }
        public string ip_address { get; set; }
        public string cc { get; set; }
        public string country { get; set; }
        public string birthdate { get; set; }
        public double? salary { get; set; }
        public string title { get; set; }
        public string comments { get; set; }
    }
}
