using System.Collections.Generic;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using System.Net;
using System.Diagnostics;
using System;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]

namespace GetService
{
    [DebuggerDisplay("Reference : {myTest}")]
    public class Function
    {

    public String myTest = "Test Ref";

    public APIGatewayProxyResponse FunctionHandler(APIGatewayProxyRequest Request, ILambdaContext context)
        {
            APIGatewayProxyResponse apiResponse = new APIGatewayProxyResponse
            {
                Headers = new Dictionary<string, string> { { "Content-Type", "application/json" }, { "Result", "Success" } }
            };
            return apiResponse;

        }
    }
}
