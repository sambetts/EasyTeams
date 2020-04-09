using EasyTeams.Common;
using EasyTeams.Common.BusinessLogic;
using Luis;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.BotBuilderSamples;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace EasyTeams.Bot.Dialogs
{
    public class MainDialog : ComponentDialog
    {
        private ConfCallManagerRecognizer _confCallManagerRecognizer;
        public MainDialog(NewConferenceCallDiag newConferenceCallDiag, ConfCallManagerRecognizer luisRecognizer) 
        {
            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), new WaterfallStep[] 
            {
                AskWhatTheyWant,
                DoWhatTheyWant,
                End
            }));
            AddDialog(new TextPrompt(nameof(TextPrompt)));
            AddDialog(newConferenceCallDiag);
            InitialDialogId = nameof(WaterfallDialog);

            _confCallManagerRecognizer = luisRecognizer;
        }

        private async Task<DialogTurnResult> AskWhatTheyWant(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            // Did the user type something before this new dialogue? Could've been an instruction after last dialogue finished...
            var lastActivity = stepContext.Context.Activity;
            if (lastActivity?.Text != null)
            {
                string lastMsg = (string)lastActivity.Text;
                return await stepContext.NextAsync(lastMsg, cancellationToken);
            }
            else
            {
                // If nothing typed before, ask
                string msg = "What do you want to do?";
                return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions() { Prompt = MessageFactory.Text(msg) }, cancellationToken);
            }
        }

        private async Task<DialogTurnResult> DoWhatTheyWant(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var luisResponse = await _confCallManagerRecognizer.RecognizeAsync<LUISConferenceCallRequest>(stepContext.Context, cancellationToken);
            switch (luisResponse.TopIntent().intent)
            {
                case LUISConferenceCallRequest.Intent.AddPerson:

                    await stepContext.Context.SendActivityAsync(MessageFactory.Text("You want to create a conference call then? Say 'Create conference'"));
                    break;
                case LUISConferenceCallRequest.Intent.CreateConferenceCall:

                    DateTime? when = null;
                    if (!string.IsNullOrEmpty(luisResponse.WhenTimex))
                    {
                        var timex = new Microsoft.Recognizers.Text.DataTypes.TimexExpression.TimexProperty(luisResponse.WhenTimex);
                        when = timex.GetDateTime();
                    }
                    var newConfDetails = new GraphNewConferenceCallRequest() { Start = when };

                    return await stepContext.BeginDialogAsync(nameof(NewConferenceCallDiag), newConfDetails, cancellationToken);
                case LUISConferenceCallRequest.Intent.None:
                    break;
                default:
                    // Catch all for unhandled intents
                    var didntUnderstandMessageText = $"Sorry, I didn't get that. Please try asking in a different way (intent was {luisResponse.TopIntent().intent})";
                    var didntUnderstandMessage = MessageFactory.Text(didntUnderstandMessageText, didntUnderstandMessageText, Microsoft.Bot.Schema.InputHints.IgnoringInput);
                    await stepContext.Context.SendActivityAsync(didntUnderstandMessage, cancellationToken);

                    break;
            }

            return await stepContext.NextAsync(null, cancellationToken);
        }

        private async Task<DialogTurnResult> End(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            return await stepContext.EndDialogAsync(null, cancellationToken);
        }
    }
}
