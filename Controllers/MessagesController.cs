using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Description;
using Microsoft.Bot.Connector;
using Newtonsoft.Json;
using Contoso_Bank.Models;
using System.Collections.Generic;

namespace Contoso_Bank
{
    [BotAuthentication]
    public class MessagesController : ApiController
    {
        /// <summary>
        /// POST: api/Messages
        /// Receive a message from a user and reply to it
        /// </summary>
        public async Task<HttpResponseMessage> Post([FromBody]Activity activity)
        {
            if (activity.Type == ActivityTypes.Message)
            {
                //interpret intent

                HttpClient luisClient = new HttpClient();
                string input = System.Web.HttpUtility.UrlEncode(activity.Text);
                string luisResponse = await luisClient.GetStringAsync(new Uri("https://api.projectoxford.ai/luis/v2.0/apps/2dbd88e3-96b5-4b48-91bd-2362f25f803f?subscription-key=fbeed415937941c4a78980f81acff101&q=" + input + "&verbose=true"));
                luisObject.RootObject luisRootObject = JsonConvert.DeserializeObject<luisObject.RootObject>(luisResponse);
                string intent = luisRootObject.topScoringIntent.intent;
                double score = luisRootObject.topScoringIntent.score;

                //accessing easytable
                HttpClient client = new HttpClient();
                client.DefaultRequestHeaders.Add("ZUMO-API-VERSION", "2.0.0");
                string x = await client.GetStringAsync(new Uri("http://xizhescontosobank.azurewebsites.net/tables/xizhescontosobank"));
                List<bankObject.RootObject> rootObjectList;
                rootObjectList = JsonConvert.DeserializeObject<List<bankObject.RootObject>>(x);
                          
                string id = rootObjectList[0].id;          
                string userName = rootObjectList[0].userName;
                string password = rootObjectList[0].password;
                double savings = rootObjectList[0].savings;
                string address = rootObjectList[0].address;
                string phone = rootObjectList[0].phone;

                //create

                //retreieve

                //update

                //delete


                ConnectorClient connector = new ConnectorClient(new Uri(activity.ServiceUrl));
                // calculate something for us to return
                int length = (activity.Text ?? string.Empty).Length;

                // return our reply to the user
                //Activity reply = activity.CreateReply($"Your intent is {intent} and score is {score}");
                Activity reply = activity.CreateReply($"Your intent is {intent}");
                await connector.Conversations.ReplyToActivityAsync(reply);
            }
            else
            {
                HandleSystemMessage(activity);
            }
            var response = Request.CreateResponse(HttpStatusCode.OK);
            return response;
        }

        private Activity HandleSystemMessage(Activity message)
        {
            if (message.Type == ActivityTypes.DeleteUserData)
            {
                // Implement user deletion here
                // If we handle user deletion, return a real message
            }
            else if (message.Type == ActivityTypes.ConversationUpdate)
            {
                // Handle conversation state changes, like members being added and removed
                // Use Activity.MembersAdded and Activity.MembersRemoved and Activity.Action for info
                // Not available in all channels
            }
            else if (message.Type == ActivityTypes.ContactRelationUpdate)
            {
                // Handle add/remove from contact lists
                // Activity.From + Activity.Action represent what happened
            }
            else if (message.Type == ActivityTypes.Typing)
            {
                // Handle knowing tha the user is typing
            }
            else if (message.Type == ActivityTypes.Ping)
            {
            }

            return null;
        }
    }
}