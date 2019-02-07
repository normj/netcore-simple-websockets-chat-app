using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Amazon.Lambda.Core;
using Amazon.Lambda.APIGatewayEvents;

using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;

using CommonChat;
using Newtonsoft.Json.Linq;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]


namespace OnConnect
{
    public class Function
    {
        IAmazonDynamoDB _ddbClient = new AmazonDynamoDBClient();

        public async Task<APIGatewayProxyResponse> FunctionHandler(APIGatewayProxyRequest request, ILambdaContext context)
        {
            try
            {
                // Using JObject instead of APIGatewayProxyRequest till APIGatewayProxyRequest gets updated with DomainName and ConnectionId 
                var connectionId = request.RequestContext.ConnectionId;
                context.Logger.LogLine($"ConnectionId: {connectionId}");

                var ddbRequest = new PutItemRequest
                {
                    TableName = Constants.TABLE_NAME,
                    Item = new Dictionary<string, AttributeValue>
                    {
                        {Constants.ConnectionIdField, new AttributeValue{ S = connectionId}}
                    }
                };

                await _ddbClient.PutItemAsync(ddbRequest);

                return new APIGatewayProxyResponse
                {
                    StatusCode = 200,
                    Body = "Connected."
                };
            }
            catch (Exception e)
            {
                context.Logger.LogLine("Error connecting: " + e.Message);
                context.Logger.LogLine(e.StackTrace);
                return new APIGatewayProxyResponse
                {
                    StatusCode = 500,
                    Body = $"Failed to connect: {e.Message}" 
                };
            }
        }
    }
}
