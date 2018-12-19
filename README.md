# .NET Core Port of simple-websockets-chat-app

This is a .NET Core port of the [simple-websockets-chat-app](https://github.com/aws-samples/simple-websockets-chat-app) sample for AWS Lambda. For more information [Announcing WebSocket APIs in Amazon API Gateway](https://aws.amazon.com/blogs/compute/announcing-websocket-apis-in-amazon-api-gateway/) blog post.

## Deploy

To deploy this sample use the the [AWS Lambda .NET Core Global Tool](https://aws.amazon.com/blogs/developer/net-core-global-tools-for-aws/).

To install the global tool execute the command. Be sure at least version 3.1.0 of the tool is installed.

```
dotnet tool install -g Amazon.Lambda.Tools
```

Then to deploy execute the following command in the root directory of this repository.
```
dotnet lambda deploy-serverless <stack-name> --region <region> --s3-bucket <storage-bucket>
```
