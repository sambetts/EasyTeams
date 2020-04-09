using EasyTeams.Bot.Models;
using EasyTeams.Common;
using EasyTeams.Common.BusinessLogic;
using EasyTeamsBot.Common;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace EasyTeams.Bot.Dialogs
{
    public class AddPeopleDialog : CancelAndHelpDialog
    {
        public AddPeopleDialog(string id = null) : base(id ?? nameof(AddPeopleDialog))
        {
            AddDialog(new TextPrompt(nameof(TextPrompt)));  // People name search
            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), new WaterfallStep[] 
            {
                AskForName,
                ResolveName,
                ConfirmName,
                ConfirmDone
            }));
            AddDialog(new ConfirmPrompt(nameof(ConfirmPrompt)));
            AddDialog(new ChoicePrompt(nameof(ChoicePrompt)));              // People picker

            InitialDialogId = nameof(WaterfallDialog);
        }


        private async Task<DialogTurnResult> AskForName(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions() 
            { 
                Prompt = MessageFactory.Text("Who should we add? Enter something to search for people."), RetryPrompt = MessageFactory.Text("Seriously, who?")
            });
            
        }
        private async Task<DialogTurnResult> ResolveName(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var searchParams = (PeopleSearchList)stepContext.Options;

            var searchQuery = (string)stepContext.Result;

            // Check email against Graph?
            var teamsManager = new PrecachedAuthTokenTeamsManager(searchParams.OAuthToken.Token);

            // Build & execute people search request
            var req = teamsManager.Client.Me.People.Request();
            req.QueryOptions.Add(new Microsoft.Graph.QueryOption("$search", searchQuery));
            var peopleResultsTopTen = await req.GetAsync();

            if (peopleResultsTopTen.Count > 0)
            {
                // Build list of choices from results
                var choices = new List<Choice>();
                foreach (var searchResult in peopleResultsTopTen)
                {
                    choices.Add(new Choice()
                    {
                        Value = $"{searchResult.DisplayName} ({searchResult.UserPrincipalName})",
                        Synonyms = new List<string>() { searchResult.UserPrincipalName, searchResult.DisplayName }
                    });
                }

                // Ask user to select a person
                string msg = $"Which one?";
                var promptMessage = MessageFactory.Text(msg, msg, Microsoft.Bot.Schema.InputHints.ExpectingInput);
                return await stepContext.PromptAsync(nameof(ChoicePrompt), new PromptOptions { Prompt = promptMessage, Choices = choices }, cancellationToken);
            }
            else
            {
                await stepContext.Context.SendActivityAsync(MessageFactory.Text("Couldn't find anyone with that name. Try another name."));

                // Go around again
                return await stepContext.ReplaceDialogAsync(nameof(AddPeopleDialog), searchParams, cancellationToken);

            }

        }
        private async Task<DialogTurnResult> ConfirmName(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var dialogParams = (PeopleSearchList)stepContext.Options;

            var response = (FoundChoice)stepContext.Result;
            var selectedContact = response.Value;
            string selectedEmail = DataUtils.ExtractEmailFromContact(selectedContact);
            dialogParams.Recipients.Add(new ContactEmailAddress(selectedEmail));

            string msg = $"Anyone else?";
            var promptMessage = MessageFactory.Text(msg, msg, Microsoft.Bot.Schema.InputHints.ExpectingInput);

            return await stepContext.PromptAsync(nameof(ConfirmPrompt), new PromptOptions { Prompt = promptMessage }, cancellationToken);
        }


        private async Task<DialogTurnResult> ConfirmDone(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            bool addMorePeople = (bool)stepContext.Result;

            var dialogParams = (PeopleSearchList)stepContext.Options;
            if (addMorePeople)
            {
                // Go around again
                return await stepContext.ReplaceDialogAsync(nameof(AddPeopleDialog), dialogParams, cancellationToken);
            }
            else
            {
                return await stepContext.EndDialogAsync(dialogParams, cancellationToken);
            }
        }


    }

}
