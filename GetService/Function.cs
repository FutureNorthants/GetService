using System.Collections.Generic;
using Amazon.Lambda.Core;
using System.Diagnostics;
using System;
using Amazon.Lex;
using Amazon;
using Amazon.Lex.Model;
using System.Threading.Tasks;
using Amazon.StepFunctions;
using Amazon.StepFunctions.Model;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Net.Http;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]

namespace GetService
{
    [DebuggerDisplay("Reference : {myTest}")]
    public class Function
    {
        private static readonly RegionEndpoint lexRegion = RegionEndpoint.EUWest2;
        private static String taskToken;

        public String myTest = "Test Ref";

        public async Task FunctionHandler(object input, ILambdaContext context)
        {
            String instance = Environment.GetEnvironmentVariable("instance");
            String cxmEndPoint;
            String cxmAPIKey;
            //TODO Use secrets manager
            //TODO change default to error trap
            //TODO Use lambda variable instead of instance
            switch (instance.ToLower())
            {
                case "live":
                    cxmEndPoint = Environment.GetEnvironmentVariable("cxmEndPointLive");
                    cxmAPIKey = Environment.GetEnvironmentVariable("cxmAPIKeyLive");
                    break;
                default:
                    cxmEndPoint = Environment.GetEnvironmentVariable("cxmEndPointTest");
                    cxmAPIKey = Environment.GetEnvironmentVariable("cxmAPIKeyTest");
                    break;
            }
            JObject o = JObject.Parse(input.ToString());
            String caseReference = (string)o.SelectToken("CaseReference");
            taskToken = (string)o.SelectToken("TaskToken");
            Console.WriteLine("caseReference : " + caseReference);

            

            CaseDetails caseDetails = await GetCustomerContactAsync(cxmEndPoint, cxmAPIKey, caseReference, taskToken);
            try
            {
                if (!String.IsNullOrEmpty(caseDetails.customerContact))
                {
                    String response = await GetClientFromLexAsync(caseDetails.customerContact);

                    if (!String.IsNullOrEmpty(response))
                    {
                            await SendSuccessAsync();
                        Console.WriteLine("Service : " + response);
                    }
                }
            }
            catch (Exception)
            {
            }
            Console.WriteLine("Completed");
        }

        private async Task<CaseDetails> GetCustomerContactAsync(String cxmEndPoint, String cxmAPIKey, String caseReference, String taskToken)
        {
            CaseDetails caseDetails = new CaseDetails();
            HttpClient cxmClient = new HttpClient();
            cxmClient.BaseAddress = new Uri(cxmEndPoint);
            String requestParameters = "key=" + cxmAPIKey;
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, "/api/service-api/norbert/case/" + caseReference + "?" + requestParameters);
            try
            {
                HttpResponseMessage response = cxmClient.SendAsync(request).Result;
                if (response.IsSuccessStatusCode)
                {
                    HttpContent responseContent = response.Content;
                    String responseString = responseContent.ReadAsStringAsync().Result;
                    JObject caseSearch = JObject.Parse(responseString);
                    caseDetails.customerContact = (String)caseSearch.SelectToken("values.enquiry_details");
                }
                else
                {
                    await SendFailureAsync("Getting case details for " + caseReference + " : " + response.StatusCode.ToString());
                    Console.WriteLine("ERROR : GetStaffResponseAsync : " + request.ToString());
                    Console.WriteLine("ERROR : GetStaffResponseAsync : " + response.StatusCode.ToString());
                }
            }
            catch (Exception error)
            {
                await SendFailureAsync("Getting case details for " + caseReference + " : " + error.Message);
                Console.WriteLine("ERROR : GetStaffResponseAsync : " + error.StackTrace);
            }
            return caseDetails;
        }

        static async Task<string> GetClientFromLexAsync(String customerContact)
        {
            try
            {
                AmazonLexClient lexClient = new AmazonLexClient(lexRegion);
                PostTextRequest textRequest = new PostTextRequest();
                textRequest.UserId = "MailBot";
                textRequest.BotAlias = "DEV";
                textRequest.BotName = "NBC_Mailbot_Intents";
                textRequest.InputText = customerContact;
                PostTextResponse textRespone = await lexClient.PostTextAsync(textRequest);
                return textRespone.IntentName;
            }
            catch(Exception error)
            {
                await SendFailureAsync(error.Message);
                return "Error" + error.Message;
            }
        }

        private async Task SendSuccessAsync()
        {
            AmazonStepFunctionsClient client = new AmazonStepFunctionsClient();
            SendTaskSuccessRequest successRequest = new SendTaskSuccessRequest();
            successRequest.TaskToken = taskToken;
            Dictionary<String, String> result = new Dictionary<String, String>
            {
                { "Result"  , "Success"  },
                { "Message" , "Completed"}
            };

            string requestOutput = JsonConvert.SerializeObject(result, Formatting.Indented);
            successRequest.Output = requestOutput;
            try
            {
                await client.SendTaskSuccessAsync(successRequest);
            }
            catch (Exception error)
            {
                Console.WriteLine("ERROR : SendSuccessAsync : " + error.Message);
                Console.WriteLine("ERROR : SendSuccessAsync : " + error.StackTrace);
            }
            await Task.CompletedTask;
        }

        private static async Task SendFailureAsync(String message)
        {
            AmazonStepFunctionsClient client = new AmazonStepFunctionsClient();
            SendTaskSuccessRequest successRequest = new SendTaskSuccessRequest();
            successRequest.TaskToken = taskToken;
            Dictionary<String, String> result = new Dictionary<String, String>
            {
                { "Result"  , "Error" },
                { "Message" , message}
            };

            string requestOutput = JsonConvert.SerializeObject(result, Formatting.Indented);
            successRequest.Output = requestOutput;
            try
            {
                await client.SendTaskSuccessAsync(successRequest);
            }
            catch (Exception error)
            {
                Console.WriteLine("ERROR : SendFailureAsync : " + error.Message);
                Console.WriteLine("ERROR : SendFailureAsync : " + error.StackTrace);
            }
            await Task.CompletedTask;
        }
    }
    class CaseDetails
    {
        public String customerContact { get; set; } = "";
    }
}
