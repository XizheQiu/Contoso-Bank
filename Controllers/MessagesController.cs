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
using Microsoft.WindowsAzure.MobileServices;
using Contoso_Bank.DataModels;

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
                await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);


                //clearing user data-------------------------------------------------------------------------------------------------------
                if (activity.Text.ToLower().Contains("clear"))
                {
                    reply = activity.CreateReply("User data cleared");
                    await stateClient.BotState.DeleteStateForUserAsync(activity.ChannelId, activity.From.Id);
                    await connector.Conversations.ReplyToActivityAsync(reply);
                    return Request.CreateResponse(HttpStatusCode.OK);
                }

                //accessing easytable-------------------------------------------------------------------------------------------------------

                List<xizhesContosoBank> rootObjectList = await AzureManager.AzureManagerInstance.GetxizhesContosoBanks();

                //authorization

                if (userData.GetProperty<bool>("loggingInUserName"))
                {
                    userData = await stateClient.BotState.GetUserDataAsync(activity.ChannelId, activity.From.Id);
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
                    userData = await stateClient.BotState.GetUserDataAsync(activity.ChannelId, activity.From.Id);
                    userData.SetProperty<bool>("loggingInPassword", false);
                    userData.SetProperty<string>("logInPassword", activity.Text);

                    string loggingInUsername = userData.GetProperty<string>("logInUsername");
                    string loggingInPassword = userData.GetProperty<string>("logInPassword");
                    await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);

                    reply = activity.CreateReply("Username or password incorrect, try again!");
                    for (int i = 0; i < rootObjectList.Count();i++)
                    {
                        
                        if (loggingInUsername == rootObjectList[i].userName && loggingInPassword == rootObjectList[i].password)
                        {
                            userData = await stateClient.BotState.GetUserDataAsync(activity.ChannelId, activity.From.Id);
                            userData.SetProperty<bool>("loggedin", true);
                            userData.SetProperty<DateTime>("loggedOnExpiryTime", DateTime.Now.AddMinutes(25));
                            userData.SetProperty<string>("loggedInUserName", loggingInUsername);
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
                    userData = await stateClient.BotState.GetUserDataAsync(activity.ChannelId, activity.From.Id);
                    userData.SetProperty<bool>("registerReady", false);
                    if (activity.Text == "Proceed")
                    {

                        xizhesContosoBank xizhesContosoBank = new xizhesContosoBank()
                        {
                            createdAt = DateTime.Now,
                            userName = userData.GetProperty<string>("userName"),
                            password = userData.GetProperty<string>("password"),
                            savings = 0,
                            address = userData.GetProperty<string>("address"),
                            phone = userData.GetProperty<string>("phone"),
                        };

                        await AzureManager.AzureManagerInstance.AddxizhesContosoBank(xizhesContosoBank);

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
                        userData = await stateClient.BotState.GetUserDataAsync(activity.ChannelId, activity.From.Id);
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
                    userData = await stateClient.BotState.GetUserDataAsync(activity.ChannelId, activity.From.Id);
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
                    userData = await stateClient.BotState.GetUserDataAsync(activity.ChannelId, activity.From.Id);
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
                    userData = await stateClient.BotState.GetUserDataAsync(activity.ChannelId, activity.From.Id);
                    userData.SetProperty<bool>("registerPhone", false);
                    userData.SetProperty<string>("phone", activity.Text);
                    userData.SetProperty<bool>("registerReady", true);
                    await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);

                    userData = await stateClient.BotState.GetUserDataAsync(activity.ChannelId, activity.From.Id);
                    string userName = userData.GetProperty<string>("userName");
                    string password = userData.GetProperty<string>("password");
                    string address = userData.GetProperty<string>("address");
                    string phone = userData.GetProperty<string>("phone");

                    Activity replyToConversation = activity.CreateReply();
                    replyToConversation.Recipient = activity.From;
                    replyToConversation.Type = "message";
                    replyToConversation.Attachments = new List<Attachment>();

                    List<CardAction> cardButtons = new List<CardAction>();
                    CardAction proceedButton = new CardAction()
                    {
                        Value = "Proceed",
                        Type = "postBack",
                        Title = "Proceed",
                    };
                    cardButtons.Add(proceedButton);

                    CardAction retryButton = new CardAction()
                    {
                        Value = "Retry",
                        Type = "postBack",
                        Title = "Retry"
                    };
                    cardButtons.Add(retryButton);

                    CardAction cancelButton = new CardAction()
                    {
                        Value = "Cancel",
                        Type = "postBack",
                        Title = "Cancel"
                    };
                    cardButtons.Add(cancelButton);

                    List<CardImage> cardImages = new List<CardImage>();
                    cardImages.Add(new CardImage(url: "http://drdianahoppe.com/wp-content/uploads/2013/04/Piggy-Bank.jpg"));

                    HeroCard plCard = new HeroCard()
                    {
                        Title = "Confirmation",
                        Text = $"Username: {userName}  \nPassword: {password}  \nAddress: {address}  \nPhone: {phone}",
                        Images = cardImages,
                        Buttons = cardButtons
                    };

                    Attachment plAttachment = plCard.ToAttachment();
                    replyToConversation.Attachments.Add(plAttachment);
                    
                    await connector.Conversations.SendToConversationAsync(replyToConversation);

                    return Request.CreateResponse(HttpStatusCode.OK);
                }


                //handling deposit-------------------------------------------------------------------------------------------------------
                if(userData.GetProperty<bool>("depositing"))
                {
                    double amount;
                    bool isDouble = double.TryParse(activity.Text, out amount);
                    if (!isDouble)
                    {
                        reply = activity.CreateReply("Invalid amount, please enter a valid number");
                        await connector.Conversations.ReplyToActivityAsync(reply);
                        return Request.CreateResponse(HttpStatusCode.OK);
                    }

                    userData = await stateClient.BotState.GetUserDataAsync(activity.ChannelId, activity.From.Id);
                    userData.SetProperty<bool>("depositing", false);
                    userData.SetProperty<double>("depositAmount", amount);
                    userData.SetProperty<bool>("depositReady", true);
                    await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);

                    Activity replyToConversation = activity.CreateReply();
                    replyToConversation.Recipient = activity.From;
                    replyToConversation.Type = "message";
                    replyToConversation.Attachments = new List<Attachment>();

                    List<CardAction> cardButtons = new List<CardAction>();
                    CardAction proceedButton = new CardAction()
                    {
                        Value = "Proceed",
                        Type = "postBack",
                        Title = "Proceed",
                    };
                    cardButtons.Add(proceedButton);

                    CardAction cancelButton = new CardAction()
                    {
                        Value = "Cancel",
                        Type = "postBack",
                        Title = "Cancel"
                    };
                    cardButtons.Add(cancelButton);

                    List<CardImage> cardImages = new List<CardImage>();
                    cardImages.Add(new CardImage(url: "http://drdianahoppe.com/wp-content/uploads/2013/04/Piggy-Bank.jpg"));

                    HeroCard plCard = new HeroCard()
                    {
                        Title = "Confirmation",
                        Text = $"You want to deposit ${amount.ToString()}",
                        Images = cardImages,
                        Buttons = cardButtons
                    };

                    Attachment plAttachment = plCard.ToAttachment();
                    replyToConversation.Attachments.Add(plAttachment);

                    await connector.Conversations.SendToConversationAsync(replyToConversation);

                    return Request.CreateResponse(HttpStatusCode.OK);
                }

                if (userData.GetProperty<bool>("depositReady"))
                {
                    if (activity.Text == "Proceed")
                    {

                        xizhesContosoBank rootObject = null;

                        for (int i = 0; i < rootObjectList.Count(); i++)
                        {
                            if (userData.GetProperty<string>("loggedInUserName") == rootObjectList[i].userName)
                            {
                                rootObject = rootObjectList[i];
                            }
                        }

                        if (rootObject != null)
                        {
                            rootObject.savings += userData.GetProperty<double>("depositAmount");
                            await AzureManager.AzureManagerInstance.UpdatexizhesContosoBank(rootObject);
                            userData = await stateClient.BotState.GetUserDataAsync(activity.ChannelId, activity.From.Id);
                            userData.SetProperty<bool>("depositing", false);
                            reply = activity.CreateReply($"Deposit suceeded, you now have {rootObject.savings}");
                            await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);
                            await connector.Conversations.ReplyToActivityAsync(reply);
                            return Request.CreateResponse(HttpStatusCode.OK);
                        }
                        else
                        {
                            reply = activity.CreateReply("Error, please contact the bank directly.");
                        }
                    }
                    else if (activity.Text == "Cancel")
                    {
                        reply = activity.CreateReply("Your deposit has been cancelled");
                        await stateClient.BotState.DeleteStateForUserAsync(activity.ChannelId, activity.From.Id);
                        await connector.Conversations.ReplyToActivityAsync(reply);
                        return Request.CreateResponse(HttpStatusCode.OK);
                    }
                    
                }


                //handling withdraw-------------------------------------------------------------------------------------------------------
                if (userData.GetProperty<bool>("withdrawing"))
                {
                    double amount;
                    bool isDouble = double.TryParse(activity.Text, out amount);
                    if (!isDouble)
                    {
                        reply = activity.CreateReply("Invalid amount, please enter a valid number");
                        await connector.Conversations.ReplyToActivityAsync(reply);
                        return Request.CreateResponse(HttpStatusCode.OK);
                    }

                    userData = await stateClient.BotState.GetUserDataAsync(activity.ChannelId, activity.From.Id);
                    userData.SetProperty<bool>("withdrawing", false);
                    userData.SetProperty<double>("withdrawAmount", amount);
                    userData.SetProperty<bool>("withdrawReady", true);
                    await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);

                    Activity replyToConversation = activity.CreateReply();
                    replyToConversation.Recipient = activity.From;
                    replyToConversation.Type = "message";
                    replyToConversation.Attachments = new List<Attachment>();

                    List<CardAction> cardButtons = new List<CardAction>();
                    CardAction proceedButton = new CardAction()
                    {
                        Value = "Proceed",
                        Type = "postBack",
                        Title = "Proceed",
                    };
                    cardButtons.Add(proceedButton);

                    CardAction cancelButton = new CardAction()
                    {
                        Value = "Cancel",
                        Type = "postBack",
                        Title = "Cancel"
                    };
                    cardButtons.Add(cancelButton);

                    List<CardImage> cardImages = new List<CardImage>();
                    cardImages.Add(new CardImage(url: "http://drdianahoppe.com/wp-content/uploads/2013/04/Piggy-Bank.jpg"));

                    HeroCard plCard = new HeroCard()
                    {
                        Title = "Confirmation",
                        Text = $"You want to withdraw ${amount.ToString()}",
                        Images = cardImages,
                        Buttons = cardButtons
                    };

                    Attachment plAttachment = plCard.ToAttachment();
                    replyToConversation.Attachments.Add(plAttachment);

                    await connector.Conversations.SendToConversationAsync(replyToConversation);

                    return Request.CreateResponse(HttpStatusCode.OK);
                }

                if (userData.GetProperty<bool>("withdrawReady"))
                {
                    if (activity.Text == "Proceed")
                    {
                        xizhesContosoBank rootObject = null;

                        for (int i = 0; i < rootObjectList.Count(); i++)
                        {
                            if (userData.GetProperty<string>("loggedInUserName") == rootObjectList[i].userName)
                            {
                                rootObject = rootObjectList[i];
                            }
                        }

                        if (rootObject != null)
                        {
                            rootObject.savings -= userData.GetProperty<double>("withdrawAmount");
                            await AzureManager.AzureManagerInstance.UpdatexizhesContosoBank(rootObject);
                            userData = await stateClient.BotState.GetUserDataAsync(activity.ChannelId, activity.From.Id);
                            userData.SetProperty<bool>("withdrawReady", false);
                            reply = activity.CreateReply($"Withdrawal suceeded, you now have {rootObject.savings} left in your account");
                            await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);
                            await connector.Conversations.ReplyToActivityAsync(reply);
                        }
                        else
                        {
                            reply = activity.CreateReply("Error, please contact the bank directly.");
                        }
                        return Request.CreateResponse(HttpStatusCode.OK);
                    }
                    else if (activity.Text == "Cancel")
                    {
                        reply = activity.CreateReply("Your withdrawal has been cancelled");
                        await stateClient.BotState.DeleteStateForUserAsync(activity.ChannelId, activity.From.Id);
                        await connector.Conversations.ReplyToActivityAsync(reply);
                        return Request.CreateResponse(HttpStatusCode.OK);
                    }
                }

                

                //handling suspend-------------------------------------------------------------------------------------------------------

                if (userData.GetProperty<bool>("suspending"))
                {
                    if(activity.Text == "Suspend")
                    {
                        xizhesContosoBank rootObject = null;

                        for (int i = 0; i < rootObjectList.Count(); i++)
                        {
                            if (userData.GetProperty<string>("loggedInUserName") == rootObjectList[i].userName)
                            {
                                rootObject = rootObjectList[i];
                            }
                        }

                        await AzureManager.AzureManagerInstance.DeletexizhesContosoBank(rootObject);
                        reply = activity.CreateReply("Your account has been suspended.");
                        await stateClient.BotState.DeleteStateForUserAsync(activity.ChannelId, activity.From.Id);
                    }
                    else if(activity.Text == "Cancel")
                    {
                        reply = activity.CreateReply("Your suspend is cancelled.");
                        userData = await stateClient.BotState.GetUserDataAsync(activity.ChannelId, activity.From.Id);
                        userData.SetProperty<bool>("suspending", false);
                        await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);
                    }
                    await connector.Conversations.ReplyToActivityAsync(reply);
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
                    userData = await stateClient.BotState.GetUserDataAsync(activity.ChannelId, activity.From.Id);
                    userData.SetProperty<bool>("registerUserName", true);
                    await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);
                    reply = activity.CreateReply("Please enter a new username");
                }

                //view-------------------------------------------------------------------------------------------------------
                if (intent == "view")
                {
                    if (!userData.GetProperty<bool>("loggedin"))
                    {
                        userData = await stateClient.BotState.GetUserDataAsync(activity.ChannelId, activity.From.Id);
                        userData.SetProperty<bool>("wantView", true);
                        await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);
                        reply = activity.CreateReply("You need to log on, please enter your username");
                        userData = await stateClient.BotState.GetUserDataAsync(activity.ChannelId, activity.From.Id);
                        userData.SetProperty<bool>("loggingInUserName", true);
                        await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);
                        await connector.Conversations.ReplyToActivityAsync(reply);
                        return Request.CreateResponse(HttpStatusCode.OK);
                    }

                    userData = await stateClient.BotState.GetUserDataAsync(activity.ChannelId, activity.From.Id);
                    userData.SetProperty<bool>("wantView", false);
                    await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);
                    double savings = 0;
                    for (int i = 0; i < rootObjectList.Count(); i++)
                    {
                        if (userData.GetProperty<string>("loggedInUserName") == rootObjectList[i].userName)
                        {
                            savings = rootObjectList[i].savings;
                        }
                    }
                    reply = activity.CreateReply($"You have ${savings} in your account");
                }

                //withdraw-------------------------------------------------------------------------------------------------------
                if (intent == "withdraw")
                {
                    if (!userData.GetProperty<bool>("loggedin"))
                    {
                        userData = await stateClient.BotState.GetUserDataAsync(activity.ChannelId, activity.From.Id);
                        userData.SetProperty<bool>("wantWithdraw", true);
                        await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);
                        reply = activity.CreateReply("You need to log on, please enter your username");
                        userData = await stateClient.BotState.GetUserDataAsync(activity.ChannelId, activity.From.Id);
                        userData.SetProperty<bool>("loggingInUserName", true);
                        await connector.Conversations.ReplyToActivityAsync(reply);
                        await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);
                        return Request.CreateResponse(HttpStatusCode.OK);
                    }

                    xizhesContosoBank rootObject = null;

                    for (int i = 0; i < rootObjectList.Count(); i++)
                    {
                        if (userData.GetProperty<string>("loggedInUserName") == rootObjectList[i].userName)
                        {
                            rootObject = rootObjectList[i];
                        }
                    }

                    userData = await stateClient.BotState.GetUserDataAsync(activity.ChannelId, activity.From.Id);
                    userData.SetProperty<bool>("wantWithdraw", false);
                    userData.SetProperty<bool>("withdrawing", true);
                    reply = activity.CreateReply($"You have ${rootObject.savings} in your account, How much would you like to withdraw? Please specify an amount");
                    await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);
                }

                //deposit-------------------------------------------------------------------------------------------------------
                if (intent == "deposit")
                {
                    if (!userData.GetProperty<bool>("loggedin"))
                    {
                        userData = await stateClient.BotState.GetUserDataAsync(activity.ChannelId, activity.From.Id);
                        userData.SetProperty<bool>("wantDeposit", true);
                        await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);
                        reply = activity.CreateReply("You need to log on, please enter your username");
                        userData = await stateClient.BotState.GetUserDataAsync(activity.ChannelId, activity.From.Id);
                        userData.SetProperty<bool>("loggingInUserName", true);
                        await connector.Conversations.ReplyToActivityAsync(reply);
                        await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);
                        return Request.CreateResponse(HttpStatusCode.OK);
                    }

                    xizhesContosoBank rootObject = null;

                    for (int i = 0; i < rootObjectList.Count(); i++)
                    {
                        if (userData.GetProperty<string>("loggedInUserName") == rootObjectList[i].userName)
                        {
                            rootObject = rootObjectList[i];
                        }
                    }

                    userData = await stateClient.BotState.GetUserDataAsync(activity.ChannelId, activity.From.Id);
                    userData.SetProperty<bool>("wantDeposit", false);
                    userData.SetProperty<bool>("depositing", true);
                    reply = activity.CreateReply($"You have ${rootObject.savings} in your account, How much would you like to deposit? Please specify an amount");
                    await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);
                }

                //suspend-------------------------------------------------------------------------------------------------------
                if (intent == "suspend")
                {
                    if (!userData.GetProperty<bool>("loggedin"))
                    {
                        userData = await stateClient.BotState.GetUserDataAsync(activity.ChannelId, activity.From.Id);
                        userData.SetProperty<bool>("wantSuspend", true);
                        await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);
                        reply = activity.CreateReply("You need to log on, please enter your username");
                        userData = await stateClient.BotState.GetUserDataAsync(activity.ChannelId, activity.From.Id);
                        userData.SetProperty<bool>("loggingInUserName", true);
                        await connector.Conversations.ReplyToActivityAsync(reply);
                        await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);
                        return Request.CreateResponse(HttpStatusCode.OK);
                    }

                    userData = await stateClient.BotState.GetUserDataAsync(activity.ChannelId, activity.From.Id);
                    userData.SetProperty<bool>("wantSuspend", false);
                    userData.SetProperty<bool>("suspending", true);
                    await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);
                    Activity replyToConversation = activity.CreateReply();
                    replyToConversation.Recipient = activity.From;
                    replyToConversation.Type = "message";
                    replyToConversation.Attachments = new List<Attachment>();

                    List<CardAction> cardButtons = new List<CardAction>();
                    CardAction proceedButton = new CardAction()
                    {
                        Value = "Suspend",
                        Type = "imBack",
                        Title = "Suspend"
                    };
                    cardButtons.Add(proceedButton);

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

                //currency-------------------------------------------------------------------------------------------------------
                if (intent == "currency")
                {

                    HttpClient currencyClient = new HttpClient();
                    string currencyResponse = await currencyClient.GetStringAsync(new Uri("http://api.fixer.io/latest?base=nzd"));
                    currencyObject.RootObject currency = JsonConvert.DeserializeObject<currencyObject.RootObject>(currencyResponse);

                    double AUD = currency.rates.AUD;                    
                    double CNY = currency.rates.CNY;                   
                    double HKD = currency.rates.HKD;                    
                    double JPY = currency.rates.JPY;
                    double KRW = currency.rates.KRW;                    
                    double RUB = currency.rates.RUB;                   
                    double USD = currency.rates.USD;                    
                    double EUR = currency.rates.EUR;

                    //reply = activity.CreateReply($"AUD: {AUD} CNY: {CNY}");

                    Activity replyToConversation = activity.CreateReply();
                    replyToConversation.Recipient = activity.From;
                    replyToConversation.Type = "message";
                    replyToConversation.Attachments = new List<Attachment>();

                    List<ReceiptItem> receiptList = new List<ReceiptItem>();

                    ReceiptItem AUDItem = new ReceiptItem()
                    {
                        Title = "AUD",
                        Price = AUD.ToString()
                    };                    
                    receiptList.Add(AUDItem);

                    ReceiptItem CNYItem = new ReceiptItem()
                    {
                        Title = "CNY",
                        Price = CNY.ToString()
                    };
                    receiptList.Add(CNYItem);

                    ReceiptItem EURItem = new ReceiptItem()
                    {
                        Title = "EUR",
                        Price = EUR.ToString()
                    };
                    receiptList.Add(EURItem);

                    ReceiptItem HKDItem = new ReceiptItem()
                    {
                        Title = "HKD",
                        Price = HKD.ToString()
                    };
                    receiptList.Add(HKDItem);

                    ReceiptItem JPYItem = new ReceiptItem()
                    {
                        Title = "JPY",
                        Price = JPY.ToString()
                    };
                    receiptList.Add(JPYItem);

                    ReceiptItem KRWItem = new ReceiptItem()
                    {
                        Title = "KRW",
                        Price = AUD.ToString()
                    };
                    receiptList.Add(KRWItem);

                    ReceiptItem RUBItem = new ReceiptItem()
                    {
                        Title = "RUB",
                        Price = RUB.ToString()
                    };
                    receiptList.Add(RUBItem);

                    ReceiptItem USDItem = new ReceiptItem()
                    {
                        Title = "USD",
                        Price = USD.ToString()
                    };
                    receiptList.Add(USDItem);
                    
                    ReceiptCard plCard = new ReceiptCard()
                    {
                        Title = "The current exchange rate based on New Zealand dollars",
                        Items = receiptList,
                    };

                    Attachment plAttachment = plCard.ToAttachment();
                    replyToConversation.Attachments.Add(plAttachment);
                    await connector.Conversations.ReplyToActivityAsync(replyToConversation);
                    return Request.CreateResponse(HttpStatusCode.OK);
                }

                // return our reply to the user-------------------------------------------------------------------------------------------------------             
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