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
                Activity reply = activity.CreateReply("Hi");
                ConnectorClient connector = new ConnectorClient(new Uri(activity.ServiceUrl));
                string manualIntent = "none";

                //grabbing state-------------------------------------------------------------------------------------------------------
                StateClient stateClient = activity.GetStateClient();
                BotData userData = await stateClient.BotState.GetUserDataAsync(activity.ChannelId, activity.From.Id);
                if(userData.GetProperty<bool>("loggedIn"))
                {
                    if(DateTime.Compare(userData.GetProperty<DateTime>("loggedOnExpiryTime"), DateTime.Now) > 0)
                    {
                        userData.SetProperty<bool>("loggedin", false);
                    }
                }


                //clearing user data-------------------------------------------------------------------------------------------------------
                if (activity.Text.ToLower().Contains("clear"))
                {
                    reply = activity.CreateReply("User data cleared");
                    await stateClient.BotState.DeleteStateForUserAsync(activity.ChannelId, activity.From.Id);
                    await connector.Conversations.ReplyToActivityAsync(reply);
                    return Request.CreateResponse(HttpStatusCode.OK);
                }

                //accessing easytable-------------------------------------------------------------------------------------------------------
                HttpClient client = new HttpClient();
                client.DefaultRequestHeaders.Add("ZUMO-API-VERSION", "2.0.0");
                string x = await client.GetStringAsync(new Uri("http://xizhescontosobank.azurewebsites.net/tables/xizhescontosobank"));
                List<bankObject.RootObject> rootObjectList;
                rootObjectList = JsonConvert.DeserializeObject<List<bankObject.RootObject>>(x);

                //handling log in

                if (userData.GetProperty<bool>("loggingInUserName"))
                {
                    userData.SetProperty<bool>("loggingInUserName", false);
                    userData.SetProperty<bool>("loggingInPassword", true);
                    userData.SetProperty<string>("logInUsername", activity.Text);
                    reply = activity.CreateReply("Please enter your password");
                    await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);
                    await connector.Conversations.ReplyToActivityAsync(reply);
                    return Request.CreateResponse(HttpStatusCode.OK);
                }

                if (userData.GetProperty<bool>("loggingInPassword"))
                {
                    userData.SetProperty<bool>("loggingInPassword", false);
                    userData.SetProperty<string>("logInPassword", activity.Text);

                    string loggingInUsername = userData.GetProperty<string>("logInUsername");
                    string loggingInPassword = userData.GetProperty<string>("logInPassword");

                    reply = activity.CreateReply("Username or password incorrect, try again!");
                    for (int i = 0; i < rootObjectList.Count();i++)
                    {
                        
                        if (loggingInUsername == rootObjectList[i].userName && loggingInPassword == rootObjectList[i].password)
                        {
                            userData.SetProperty<bool>("loggedin", true);
                            userData.SetProperty<DateTime>("loggedOnExpiryTime", DateTime.Now.AddMinutes(25));
                            reply = activity.CreateReply("You are logged on, to log out, enter 'clear'");
                        }
                    }
                                        
                    await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);
                    await connector.Conversations.ReplyToActivityAsync(reply);

                    if (userData.GetProperty<bool>("wantWithdraw"))
                    {
                        manualIntent = "withdraw";
                    }

                    if (userData.GetProperty<bool>("wantDeposit"))
                    {
                        manualIntent = "deposit";
                    }

                    if (userData.GetProperty<bool>("wantView"))
                    {
                        manualIntent = "view";
                    }

                    if (userData.GetProperty<bool>("wantSuspend"))
                    {
                        manualIntent = "suspend";
                    }
                }

                //handling registration-------------------------------------------------------------------------------------------------------

                if (userData.GetProperty<bool>("registerReady"))
                {
                    userData.SetProperty<bool>("registerReady", false);
                    if (activity.Text == "Proceed")
                    {
                        reply = activity.CreateReply("Your new account has been registered");
                        await stateClient.BotState.DeleteStateForUserAsync(activity.ChannelId, activity.From.Id);
                        await connector.Conversations.ReplyToActivityAsync(reply);
                        return Request.CreateResponse(HttpStatusCode.OK);
                    }
                    if (activity.Text == "Retry")
                    {
                        reply = activity.CreateReply("Please try again");
                        await stateClient.BotState.DeleteStateForUserAsync(activity.ChannelId, activity.From.Id);
                        await connector.Conversations.ReplyToActivityAsync(reply);
                        manualIntent = "register";
                    }
                    if (activity.Text == "Cancel")
                    {
                        reply = activity.CreateReply("Your registration has been cancelled");
                        await stateClient.BotState.DeleteStateForUserAsync(activity.ChannelId, activity.From.Id);
                        await connector.Conversations.ReplyToActivityAsync(reply);
                        return Request.CreateResponse(HttpStatusCode.OK);
                    }
                }

                if (userData.GetProperty<bool>("registerUserName"))
                {
                    bool repeatedUsername = false;
                    for (int i = 0; i < rootObjectList.Count; i++)
                    {
                        if (rootObjectList[i].userName == activity.Text)
                        {
                            repeatedUsername = true;
                        }
                    }

                    if (repeatedUsername == false)
                    {
                        userData.SetProperty<bool>("registerUserName", false);
                        userData.SetProperty<bool>("registerPassword", true);
                        userData.SetProperty<string>("userName", activity.Text);
                        reply = activity.CreateReply("Please enter a new password");
                    }
                    else
                    {
                        reply = activity.CreateReply("Sorry this username has been taken, please try another one.");
                    }
                    await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);
                    await connector.Conversations.ReplyToActivityAsync(reply);
                    return Request.CreateResponse(HttpStatusCode.OK);
                }

                if (userData.GetProperty<bool>("registerPassword"))
                {
                    userData.SetProperty<bool>("registerPassword", false);
                    userData.SetProperty<bool>("registerAddress", true);
                    userData.SetProperty<string>("password", activity.Text);
                    reply = activity.CreateReply("Please enter a new address");
                    await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);
                    await connector.Conversations.ReplyToActivityAsync(reply);
                    return Request.CreateResponse(HttpStatusCode.OK);
                }

                if (userData.GetProperty<bool>("registerAddress"))
                {
                    userData.SetProperty<bool>("registerAddress", false);
                    userData.SetProperty<bool>("registerPhone", true);
                    userData.SetProperty<string>("address", activity.Text);
                    reply = activity.CreateReply("Please enter a new phone number");
                    await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);
                    await connector.Conversations.ReplyToActivityAsync(reply);
                    return Request.CreateResponse(HttpStatusCode.OK);
                }

                if (userData.GetProperty<bool>("registerPhone"))
                {
                    userData.SetProperty<bool>("registerPhone", false);
                    userData.SetProperty<string>("phone", activity.Text);
                    await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);

                    userData.SetProperty<bool>("registerReady", true);
                    string userName = userData.GetProperty<string>("userName");
                    string password = userData.GetProperty<string>("password");
                    string address = userData.GetProperty<string>("address");
                    string phone = userData.GetProperty<string>("phone");

                    Activity replyToConversation = activity.CreateReply($"Please confirm the following:");
                    await connector.Conversations.SendToConversationAsync(replyToConversation);
                    replyToConversation = activity.CreateReply($"Username: {userName}");
                    await connector.Conversations.SendToConversationAsync(replyToConversation);
                    replyToConversation = activity.CreateReply($"Password: {password}");
                    await connector.Conversations.SendToConversationAsync(replyToConversation);
                    replyToConversation = activity.CreateReply($"Address: {address}");
                    await connector.Conversations.SendToConversationAsync(replyToConversation);
                    replyToConversation = activity.CreateReply($"Phone: {phone}");
                    await connector.Conversations.SendToConversationAsync(replyToConversation);

                    replyToConversation = activity.CreateReply();
                    replyToConversation.Recipient = activity.From;
                    replyToConversation.Type = "message";
                    replyToConversation.Attachments = new List<Attachment>();

                    List<CardAction> cardButtons = new List<CardAction>();
                    CardAction proceedButton = new CardAction()
                    {
                        Value = "Proceed",
                        Type = "imBack",
                        Title = "Proceed"
                    };
                    cardButtons.Add(proceedButton);

                    CardAction retryButton = new CardAction()
                    {
                        Value = "Retry",
                        Type = "imBack",
                        Title = "Retry"
                    };
                    cardButtons.Add(retryButton);

                    CardAction cancelButton = new CardAction()
                    {
                        Value = "Cancel",
                        Type = "imBack",
                        Title = "Cancel"
                    };
                    cardButtons.Add(cancelButton);

                    SigninCard plCard = new SigninCard()
                    {
                        Buttons = cardButtons
                    };

                    Attachment plAttachment = plCard.ToAttachment();
                    replyToConversation.Attachments.Add(plAttachment);
                    await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);
                    await connector.Conversations.SendToConversationAsync(replyToConversation);

                    return Request.CreateResponse(HttpStatusCode.OK);
                }

                //interpret intent-------------------------------------------------------------------------------------------------------
                HttpClient luisClient = new HttpClient();
                string input = System.Web.HttpUtility.UrlEncode(activity.Text);
                string luisResponse = await luisClient.GetStringAsync(new Uri("https://api.projectoxford.ai/luis/v2.0/apps/2dbd88e3-96b5-4b48-91bd-2362f25f803f?subscription-key=fbeed415937941c4a78980f81acff101&q=" + input + "&verbose=true"));
                luisObject.RootObject luisRootObject = JsonConvert.DeserializeObject<luisObject.RootObject>(luisResponse);
                string intent = luisRootObject.topScoringIntent.intent;
                double score = luisRootObject.topScoringIntent.score;

                if (manualIntent == "register")
                {
                    intent = "register";
                }

                if (manualIntent == "withdraw")
                {
                    intent = "withdraw";
                }

                if (manualIntent == "deposit")
                {
                    intent = "deposit";
                }

                if (manualIntent == "view")
                {
                    intent = "view";
                }

                if (manualIntent == "suspend")
                {
                    intent = "suspend";
                }


                //create, register-------------------------------------------------------------------------------------------------------
                if (intent == "register")
                {
                    userData.SetProperty<bool>("registerUserName", true);
                    await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);
                    reply = activity.CreateReply("Please enter a new username");
                }

                //retreieve, view-------------------------------------------------------------------------------------------------------
                if (intent == "view")
                {
                    if (!userData.GetProperty<bool>("loggedin"))
                    {
                        userData.SetProperty<bool>("wantView", true);
                        await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);
                        reply = activity.CreateReply("You need to log on, please enter your username");
                        userData.SetProperty<bool>("loggingInUserName", true);
                        await connector.Conversations.ReplyToActivityAsync(reply);
                        await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);
                        return Request.CreateResponse(HttpStatusCode.OK);
                    }

                    userData.SetProperty<bool>("wantView", false);
                    await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);
                    reply = activity.CreateReply("In view mode");
                }

                //update, withdraw-------------------------------------------------------------------------------------------------------
                if (intent == "withdraw")
                {
                    if (!userData.GetProperty<bool>("loggedin"))
                    {
                        userData.SetProperty<bool>("wantWithdraw", true);
                        await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);
                        reply = activity.CreateReply("You need to log on, please enter your username");
                        userData.SetProperty<bool>("loggingInUserName", true);
                        await connector.Conversations.ReplyToActivityAsync(reply);
                        await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);
                        return Request.CreateResponse(HttpStatusCode.OK);
                    }

                    userData.SetProperty<bool>("wantWithdraw", false);
                    await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);
                    reply = activity.CreateReply("In withdraw mode");
                }

                //update, deposit-------------------------------------------------------------------------------------------------------
                if (intent == "deposit")
                {
                    if (!userData.GetProperty<bool>("loggedin"))
                    {
                        userData.SetProperty<bool>("wantDeposit", true);
                        await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);
                        reply = activity.CreateReply("You need to log on, please enter your username");
                        userData.SetProperty<bool>("loggingInUserName", true);
                        await connector.Conversations.ReplyToActivityAsync(reply);
                        await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);
                        return Request.CreateResponse(HttpStatusCode.OK);
                    }

                    userData.SetProperty<bool>("wantDeposit", false);
                    await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);
                    reply = activity.CreateReply("In deposit mode");
                }

                //delete, suspend-------------------------------------------------------------------------------------------------------
                if (intent == "suspend")
                {
                    if (!userData.GetProperty<bool>("loggedin"))
                    {
                        userData.SetProperty<bool>("wantSuspend", true);
                        await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);
                        reply = activity.CreateReply("You need to log on, please enter your username");
                        userData.SetProperty<bool>("loggingInUserName", true);
                        await connector.Conversations.ReplyToActivityAsync(reply);
                        await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);
                        return Request.CreateResponse(HttpStatusCode.OK);
                    }

                    userData.SetProperty<bool>("wantSuspend", false);
                    await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);
                    reply = activity.CreateReply("In suspend mode");
                }

                // return our reply to the user   -------------------------------------------------------------------------------------------------------             
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