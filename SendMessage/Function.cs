using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

using Amazon.Lambda.Core;
using Amazon.Lambda.APIGatewayEvents;

using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.ApiGatewayManagementApi;
using Amazon.ApiGatewayManagementApi.Model;
using Amazon.Runtime;
using CommonChat;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]

namespace SendMessage
{
    public class Function
    {
        IAmazonDynamoDB _ddbClient = new AmazonDynamoDBClient();

        public async Task<APIGatewayProxyResponse> FunctionHandler(JObject request, ILambdaContext context)
        {
            try
            {
                // Using JObject instead of APIGatewayProxyRequest till APIGatewayProxyRequest gets updated with DomainName and ConnectionId 
                var domainName = request["requestContext"]["domainName"].ToString();
                var stage = request["requestContext"]["stage"].ToString();
                var endpoint = $"https://{domainName}/{stage}";
                context.Logger.LogLine($"API Gateway management endpoint: {endpoint}");

                var body = request["body"]?.ToString();
                var message = JsonConvert.DeserializeObject<JObject>(body);
                var data = message["data"]?.ToString();

                var stream = new MemoryStream(UTF8Encoding.UTF8.GetBytes(data));
                
                var scanRequest = new ScanRequest
                {
                    TableName = Constants.TABLE_NAME,
                    ProjectionExpression = Constants.ConnectionIdField
                };
    
                var scanResponse = await _ddbClient.ScanAsync(scanRequest);

                var apiClient = new AmazonApiGatewayManagementApiClient(new AmazonApiGatewayManagementApiConfig
                {
                    ServiceURL = endpoint
                });
                
                var count = 0;
                foreach (var item in scanResponse.Items)
                {
                    var connectionId = item[Constants.ConnectionIdField].S;


                    var postConnectionRequest = new PostToConnectionRequest
                    {
                        ConnectionId = connectionId,
                        Data = stream
                    };

                    try
                    {
                        context.Logger.LogLine($"Post to connection {count}: {connectionId}");
                        stream.Position = 0;
                        await apiClient.PostToConnectionAsync(postConnectionRequest);
                        count++;
                    }
                    catch (AmazonServiceException e)
                    {
                        // API Gateway returns a status of 410 GONE when the connection is no
                        // longer available. If this happens, we simply delete the identifier
                        // from our DynamoDB table.
                        if (e.StatusCode == HttpStatusCode.Gone)
                        {
                            var ddbDeleteRequest = new DeleteItemRequest
                            {
                                TableName = Constants.TABLE_NAME,
                                Key = new Dictionary<string, AttributeValue>
                                {
                                    {Constants.ConnectionIdField, new AttributeValue {S = connectionId}}
                                }
                            };

                            context.Logger.LogLine($"Deleting gone connection: {connectionId}");
                            await _ddbClient.DeleteItemAsync(ddbDeleteRequest);
                        }
                        else
                        {
                            context.Logger.LogLine($"Error posting message to {connectionId}: {e.Message}");
                            context.Logger.LogLine(e.StackTrace);                            
                        }
                    }
                }
                
                return new APIGatewayProxyResponse
                {
                    StatusCode = 200,
                    Body = "Data send to " + count + " connection" + (count == 1 ? "" : "s")
                };
            }
            catch (Exception e)
            {
                context.Logger.LogLine("Error disconnecting: " + e.Message);
                context.Logger.LogLine(e.StackTrace);
                return new APIGatewayProxyResponse
                {
                    StatusCode = 500,
                    Body = $"Failed to send message: {e.Message}" 
                };
            }
            
        }
    }
}
