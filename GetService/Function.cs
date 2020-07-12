using System.Collections.Generic;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using System.Diagnostics;
using System;
using Amazon.Lex;
using Amazon;
using Amazon.Lex.Model;
using System.Threading.Tasks;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]

namespace GetService
{
    [DebuggerDisplay("Reference : {myTest}")]
    public class Function
    {
        private static readonly RegionEndpoint lexRegion = RegionEndpoint.EUWest2;

        public String myTest = "Test Ref";

        public APIGatewayProxyResponse FunctionHandler(APIGatewayProxyRequest Request, ILambdaContext context)
        {

            // String response = GetClientFromLexAsync().Result;
            //Task<String> response = GetClientFromLexAsync();
            //GetClientFromLexAsync().Wait();
            GetClientFromLexAsync().GetAwaiter().GetResult();

            APIGatewayProxyResponse apiResponse = new APIGatewayProxyResponse
            {
                Headers = new Dictionary<string, string> { { "Content-Type", "application/json" }, { "Result", "Success" } }
            };
            return apiResponse;

        }

        static async Task<string> GetClientFromLexAsync()
        {
            try
            {
                AmazonLexClient lexClient = new AmazonLexClient(lexRegion);
                PostTextRequest textRequest = new PostTextRequest();
                textRequest.UserId = "MailBot1";
                textRequest.BotAlias = "CONNIENBC";
                textRequest.BotName = "Connie_NBC";
                textRequest.InputText = "How can i claim benefits";
                PostTextResponse textRespone = await lexClient.PostTextAsync(textRequest);
                return textRespone.IntentName;
            }
            catch(Exception error)
            {
                return "Error";
            }
        }
    }
}
