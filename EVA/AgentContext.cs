using OpenAI_API.Chat;
using OpenAI_API;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EVA.Commands;
using Newtonsoft.Json.Linq;
using static EVA.MainWindowView;
using System.Text.Json;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Collections.Concurrent;
using System.Security.Authentication;

namespace EVA
{
    public enum Role { User, AI, Thinking, System , Error}
    public class AgentContext : INotifyPropertyChanged
    {
        // Add properties for Tokens and Cost
        public event PropertyChangedEventHandler PropertyChanged;

        public readonly OpenAIAPI OpenAIApi;
        public readonly Conversation chat;
        private List<OpenAI_API.Models.Model> models;
        public ObservableCollection<Message> Messages { get; } = new ObservableCollection<Message>();
        private Dictionary<string, Type> CommandTypes { get; } = new Dictionary<string, Type>();
        public bool HasActiveConversation { get; private set; } = false;
        public bool IsProcessing { get; private set; } = false;
        public Config Config { get; private set; }
        public List<string> StepsTaken { get; private set; } = new List<string>();
        public List<Command> PlannedCommands { get; private set; } = new List<Command>();

        public string UserRequest { set; get; } = string.Empty;

        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        public string Cost
        {
            get
            {
                return Math.Round(((Tokens / 1000.0) * 0.2), 1).ToString() + " Cent";
            }
        }
        public int Tokens { get; set; } = 0;

        public Action<Role, string> MessageReceivedCallback { get; set; }

        public AgentContext(Config config)
        {
            this.Config = config;

            OpenAIApi = new OpenAIAPI(Config.OpenAIAccessToken);
            chat = OpenAIApi.Chat.CreateConversation();

            CommandTypes.Add((new AskUserCommand()).CommandName, typeof(AskUserCommand));
            CommandTypes.Add((new AskWikipediaCommand()).CommandName, typeof(AskWikipediaCommand));
            CommandTypes.Add((new FinalResponseCommand()).CommandName, typeof(FinalResponseCommand));
            CommandTypes.Add((new ProcessWithGPTCommand()).CommandName, typeof(ProcessWithGPTCommand));
            CommandTypes.Add((new VoiceOutputCommand()).CommandName, typeof(VoiceOutputCommand));
            CommandTypes.Add((new ListLocalFilesCommand()).CommandName, typeof(ListLocalFilesCommand));
            CommandTypes.Add((new ReadLocalFileCommand()).CommandName, typeof(ReadLocalFileCommand));
            CommandTypes.Add((new WriteLocalFileCommand()).CommandName, typeof(WriteLocalFileCommand));
            CommandTypes.Add((new CalculateExpressionCommand()).CommandName, typeof(CalculateExpressionCommand));

        }
        public async Task<string> AnalyzeInputWithGPT4Async(string prompt)
        {
            var result = await OpenAIApi.Chat.CreateChatCompletionAsync(prompt);
            Tokens += result.Usage.TotalTokens;
            return result.Choices[0].Message.Content;
        }
        public async Task HandleUserRequestAsync(string userRequest)
        {
            //We are already processing a request so this is additional info from the user
            if (!string.IsNullOrWhiteSpace(UserRequest))
            {
                OnNewUserMessage(userRequest);
                return;
            }
            UserRequest = userRequest;
            await ProcessTask();
        }
        public async Task ProcessTask()
        {
            // First, get the suggested command from the LLM
            string jsonResponse = string.Empty;
            try
            {
                jsonResponse = await SubconsciousnessActionAsync();
            } catch (AuthenticationException ex)
            {
                SendMessageToUI(Role.Error, $"Error:\n\"{ex.Message}\"\n\nDid you forget to add your API Key to \"{Config.ConfigFile}\"?");
                return;
            }

            // Deserialize the command
            var command = DeserializeCommand(jsonResponse);
            command.Context = this;


            SendMessageToUI(Role.System, "Subconscious Command: " + command.GetPrompt());
            // Check if the command is the final response
            if (command is FinalResponseCommand finalResponse)
            {

                //Messages.Add(new AssistantMessage { Text = finalResponse.Text });
                SendMessageToUI(Role.AI, finalResponse.Text);

                //Reset
                UserRequest = string.Empty;
                StepsTaken.Clear();

                return;
            }

            // Execute the command and get the result
            //ProcessingMessage(true);
            await command.Execute();
            //ProcessingMessage(false);

            if (command is AskUserCommand question)
            {
                // Create a TaskCompletionSource to wait for the next user message
                TaskCompletionSource<string> tcs = new TaskCompletionSource<string>();

                // Subscribe to an event that will be triggered when a new user message is added
                NewUserMessage += (sender, userMessage) =>
                {
                    tcs.TrySetResult(userMessage);
                };

                // Wait for the user's message
                question.Result = await tcs.Task;
            }

            string result = command.GetResult("");
            StepsTaken.Add(result);

            // Continue the process recursively until a final response is obtained
            await ProcessTask();
        }



        public async Task<string> SubconsciousnessActionAsync()
        {

            string prompt = "";
            prompt = "As part of an AI assistant it is your task to selects the most appropriate programmatic command from a list of possible commands based on a useriput and the AIs steps to solve the task so far. Depending on the command fill in all the necessary parameters of the command based on the userinput.";
            prompt += "Available commands:" + "\n";
            foreach (var a in CommandTypes)
            {
                var cmd = (Command)Activator.CreateInstance(a.Value);
                prompt += cmd.Description + ": " + cmd.GetPrompt() + "\n";
            }
            prompt += "Respond only in the JSON format that can be parsed with JSON.parse of the selected action. Only use actions from the provided list! Fill in the template \"{\"action\":\"NameofActionfromList\",... other parameter specified in the list based on the useriput}\"";

            prompt += $"User request:\n\"{UserRequest}\"\n";
            prompt += "\nSteps and tasks already done:\n";
            if(StepsTaken.Count == 0)
                prompt += "None\n";
            foreach (var s in StepsTaken)
                prompt += s + "\n";
            prompt += "If the user request can be answered with this information, give a final response with the \"respond\" command. Otherwise try another command. Avoid loops like endlessly checking the same wikipedia article. Make sure the output is in JSON format like specified in the command list.";
            //AddMessage(Role.System,"Subconsciousness:\n" + prompt);

            // Refine the prompt by checking for loops, false assumptions, and improving the focus on the user's goal
            //string refinedPrompt = await RefinePromptAsync(userInput, StepsByAI, prompt);

            //ProcessingMessage(true);
            var result = await OpenAIApi.Chat.CreateChatCompletionAsync(prompt);
            //ProcessingMessage(false);

            Tokens += result.Usage.TotalTokens;
            return result.Choices[0].Message.Content;

        }

        public async Task<string> RefinePromptAsync(string userInput, List<string> StepsByAI, string initialPrompt)
        {
            // Check if the prompt contains any loops, repetitions or false assumptions
            bool loopDetected = false;
            bool falseAssumptionDetected = false;
            string refinedPrompt = initialPrompt;

            // Check for loops or repetitions
            HashSet<string> uniqueSteps = new HashSet<string>();
            foreach (var step in StepsByAI)
            {
                if (uniqueSteps.Contains(step))
                {
                    loopDetected = true;
                    break;
                }
                uniqueSteps.Add(step);
            }

            // If a loop update the prompt to avoid them
            if (loopDetected || falseAssumptionDetected)
            {
                refinedPrompt += "\nImportant: ";
                if (loopDetected)
                {
                    refinedPrompt += "The AI has detected a loop in the previous steps. ";
                }
                refinedPrompt += "Please take this into account and avoid repeating the same actions. Focus on the user's goal and provide a helpful and accurate response.";
            }
            if (Config.RefinePromptsWithGPT)
                refinedPrompt = await RefinePromptWithGPTAsync(refinedPrompt);

            return refinedPrompt;
        }

        public async Task<string> RefinePromptWithGPTAsync(string prompt)
        {
            string gptPrompt = $"The following is a prompt for an AI assistant: \"{prompt}\". Please improve and refine the prompt to make it more clear and effective.";

            var result = await OpenAIApi.Chat.CreateChatCompletionAsync(gptPrompt);
            Tokens += result.Usage.TotalTokens;

            string refinedPrompt = result.Choices[0].Message.Content;
            return refinedPrompt.Trim();
        }
        public Command DeserializeCommand(string jsonResponse)
        {
            var json = JObject.Parse(jsonResponse);
            var commandName = json["action"]?.ToString();

            if (commandName != null && CommandTypes.TryGetValue(commandName, out Type commandType))
            {
                var command = (Command)JsonSerializer.Deserialize(jsonResponse, commandType);

                // Populate CustomProperties
                foreach (var property in json.Properties())
                {
                    if (property.Name != "action")
                    {
                        command.CustomProperties[property.Name] = property.Value.ToObject<object>();
                    }
                }

                return command;
            }

            return null;
        }
        public delegate void NewUserMessageEventHandler(object sender, string userMessage);
        public event NewUserMessageEventHandler NewUserMessage;

        protected virtual void OnNewUserMessage(string userMessage)
        {
            NewUserMessage?.Invoke(this, userMessage);
            StepsTaken.Add("The user wrote in the chat: " + userMessage);
        }
      

        /* private async Task<string> ProcessAdditionalInputAsync(string additionalInput)
         {
             // Implement the logic to process the additional input and update the response if necessary
             // For example, if the additional input suggests checking the German Wikipedia, modify the search command accordingly
         }*/


        // ... other properties and methods ...
        public void SendMessageToUI(Role role, string message)
        {
            MessageReceivedCallback?.Invoke(role, message);
        }

        private async Task UpdatePlannedCommandsAsync(string userRequest, List<string> intermediateResults)
        {
            // ... method body to update PlannedCommands based on user input and intermediate results ...
        }
    }
}
