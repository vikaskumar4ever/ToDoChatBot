using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ToDoCoreBot.Utilities;

namespace ToDoCoreBot.Dialogs.Operations
{
    public class CreateTaskDialog : ComponentDialog
    {
        private readonly CosmosDBClient _cosmosDBClient;
        public CreateTaskDialog(CosmosDBClient cosmosDBClient) : base(nameof(CreateTaskDialog))
        {
            _cosmosDBClient = cosmosDBClient;
            var waterfallSteps = new WaterfallStep[]
            {
                TasksStepAsync,
                ActStepAsync,
                MoreTasksStepAsync,
                SummaryStepAsync
            };
            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), waterfallSteps));
            AddDialog(new TextPrompt(nameof(TextPrompt)));
            AddDialog(new ConfirmPrompt(nameof(ConfirmPrompt)));
            AddDialog(new CreateMoreTaskDialog());

            InitialDialogId = nameof(WaterfallDialog); 
        }

        private async Task<DialogTurnResult> TasksStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions
            {
                Prompt = MessageFactory.Text("Please give the task to add.")
            }, cancellationToken);
        }

        private async Task<DialogTurnResult> ActStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var userDetails = (User)stepContext.Options;
            stepContext.Values["Task"] = (string)stepContext.Result;
            userDetails.TaskList.Add((string)stepContext.Values["Task"]);

            return await stepContext.PromptAsync(nameof(ConfirmPrompt), new PromptOptions
            {
                Prompt = MessageFactory.Text("Would you like to Add more tasks?")
            }, cancellationToken);
        }

        private async Task<DialogTurnResult> MoreTasksStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var userDetails = (User)stepContext.Options;
            if ((bool)stepContext.Result)
            {
                return await stepContext.BeginDialogAsync(nameof(CreateMoreTaskDialog), userDetails, cancellationToken);
            }
            else
            {
                return await stepContext.NextAsync(userDetails, cancellationToken);
            }
        }

        private async Task<DialogTurnResult> SummaryStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var userDetails = (User)stepContext.Options;
            await stepContext.Context.SendActivityAsync(MessageFactory.Text("Here are the task you provided - "), cancellationToken);
            for (int i = 0; i < userDetails.TaskList.Count; i++)
            {
                await stepContext.Context.SendActivityAsync(MessageFactory.Text(userDetails.TaskList[i]), cancellationToken);
            }
            await stepContext.Context.SendActivityAsync(MessageFactory.Text("Please wait, while we add the task to the database - "), cancellationToken);

            for (int i = 0; i < userDetails.TaskList.Count; i++)
            {
                if (await _cosmosDBClient.AddItemsToContainerAsync(User.UserID, userDetails.TaskList[i]) == -1)
                {
                    await stepContext.Context.SendActivityAsync(MessageFactory.Text("The task '" + userDetails.TaskList[i] + "' already present"), cancellationToken);
                }
            }
            await stepContext.Context.SendActivityAsync(MessageFactory.Text("The add task operation is completed. Thank you. "), cancellationToken);
            //AddItemsToContainerAsync
            return await stepContext.EndDialogAsync(userDetails, cancellationToken);
        }
    }
}
