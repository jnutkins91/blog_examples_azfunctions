using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

using Microsoft.PowerPlatform.Cds.Client;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk;

namespace Ndxc.Blog
{
    public static class Http_GetEnvironmentVariableValue
    {
        [FunctionName("Http_GetEnvironmentVariableValue")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            string name = req.Query["name"];
            string value;

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            name = name ?? data?.name;

            // Ensure we have been provided with an Environment Variable Name
            if (name == null || name.Length <= 0)
            {
                return new BadRequestObjectResult("You must provide a value for 'name' either in the POST body or as a query parameter");
            }

            CdsServiceClient svc;  

            try
            {

                svc = new CdsServiceClient("CHANGEME");

                // Only 1 Definition is expected 
                EntityCollection ecDefinitions = GetDefinitions(svc, name);

                if (ecDefinitions.Entities.Count == 1)
                {

                    // Set to the Default Value of the Variable
                    value = (string)ecDefinitions.Entities[0]["defaultvalue"];

                    // 0, or 1 Value is expected
                    // 0 if no Current Value has been set for the Environment Variable
                    EntityCollection ecValues = GetValuesForDefinition(svc, ecDefinitions.Entities[0].Id); ;

                    if (ecValues.Entities.Count == 1)
                    {
                        value = (string)ecValues.Entities[0]["value"];
                    }

                    return new OkObjectResult(value);
                }
                else
                {
                    return new BadRequestObjectResult(string.Format("Environment Variable with Name '{0}' does not exist", name));
                }

                
            }
            catch (Exception ex)
            {
                return new BadRequestObjectResult(string.Format("An Error occured: {0}", ex.Message));
            }         
        }

        private static EntityCollection GetDefinitions(CdsServiceClient service, string variableName)
        {

            var qeDefinitions = new QueryExpression();
            qeDefinitions.EntityName = "environmentvariabledefinition";
            qeDefinitions.ColumnSet = new ColumnSet();
            qeDefinitions.ColumnSet.Columns.Add("environmentvariabledefinitionid");
            qeDefinitions.ColumnSet.Columns.Add("defaultvalue");
            qeDefinitions.TopCount = 1;

            qeDefinitions.Criteria.AddFilter(LogicalOperator.And);
            qeDefinitions.Criteria.AddCondition("displayname", ConditionOperator.Equal, variableName);

            return service.RetrieveMultiple(qeDefinitions);
        }

        private static EntityCollection GetValuesForDefinition(CdsServiceClient service, Guid definitionId)
        {

            var qeValues = new QueryExpression();
            qeValues.EntityName = "environmentvariablevalue";
            qeValues.ColumnSet = new ColumnSet();
            qeValues.ColumnSet.Columns.Add("value");
            qeValues.TopCount = 1;

            qeValues.Criteria.AddFilter(LogicalOperator.And);
            qeValues.Criteria.AddCondition("environmentvariabledefinitionid", ConditionOperator.Equal, definitionId);

            return service.RetrieveMultiple(qeValues);
        }
    }
}
