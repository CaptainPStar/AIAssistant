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
using System.Diagnostics;
using System.IO;

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

using System.Globalization;

using System.Data;

using System.Net.Http.Headers;
using System.Net.Http;
using ControlzEx.Standard;
using System.Speech.Synthesis;
using OpenAI_API.Completions;
using OpenAI_API.Models;
using System.Security.Policy;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;

namespace EVA
{
    public enum Role { User, AI, Thinking, System, Error }
    public partial class AgentContext : INotifyPropertyChanged
    {

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public readonly OpenAIAPI OpenAIApi;
        public readonly Conversation chat;
        private List<OpenAI_API.Models.Model> models;

        public CommandFactory CommandFactory { private set; get; }
        public ObservableCollection<Message> Messages { get; } = new ObservableCollection<Message>();
        private Dictionary<string, Type> CommandTypes { get; } = new Dictionary<string, Type>();
        public bool HasActiveConversation { get; private set; } = false;
        public bool IsProcessing { get; private set; } = false;
        public Config Config { get; private set; }
        public List<string> StepsTaken { get; private set; } = new List<string>();
        public List<string> StepsPlanned { get; private set; } = new List<string>();
        private TaskBreakdownPlanner Planner { get; set; }
        public List<ICommand> PlannedCommands { get; private set; } = new List<ICommand>();
        public List<string> AbilitiesPrompt { get; set; } = new List<string>();
        public string UserRequest { set; get; } = string.Empty;

        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        public string Cost
        {
            get
            {
                return Math.Round(((Tokens / 1000.0) * 0.2), 1).ToString() + " Cent";
            }
        }
        private int _tokens = 0;
        //public int Tokens { get; set; } = 0;

        public Action<Role, string> MessageReceivedCallback { get; set; }
        public int Tokens {
            get {
                return _tokens;
            }
            set {
                _tokens = value; OnPropertyChanged("Tokens");
            }
        }

        public AgentContext(Config config)
        {
            this.Config = config;

            if (Config.UseAzure)
            {
                try
                {
                    OpenAIApi = OpenAIAPI.ForAzure($"{Config.AzureOpenAIAPIResource}", Config.AzureOpenAIAPIDeploymentID, Config.AzureOpenAIAPIKey);
                    OpenAIApi.ApiVersion = "2023-03-15-preview"; // needed to access chat endpoint on Azure
                                                                 // var model = OpenAIApi.Models.RetrieveModelDetailsAsync(Config.AzureOpenAIAPIDeploymentID).GetAwaiter().GetResult();
                } catch (Exception ex) {
                    SendMessageToUI(Role.Error, $"Error connecting to Azure Endpoint:\n{ex.Message}");
                }
            }
            else
            {
                OpenAIApi = new OpenAIAPI(Config.OpenAIAccessToken);
            }

            //OpenAIAPI.ForAzure("YourResourceName", "deploymentId", "api-key");
            //api.ApiVersion = "2023-03-15-preview"; // needed to access chat endpoint on Azure

            //chat = OpenAIApi.Chat.CreateConversation();
            CommandFactory = new CommandFactory();

            CommandFactory.RegisterCommand(typeof(AskUserCommand));
            CommandFactory.RegisterCommand(typeof(AskWikipediaCommand));
            CommandFactory.RegisterCommand(typeof(FinalResponseCommand));
            CommandFactory.RegisterCommand(typeof(ProcessWithGPTCommand));
            CommandFactory.RegisterCommand(typeof(BrowseURLCommand));

            if (Config.UseFileIO)
            {
                CommandFactory.RegisterCommand(typeof(ListFilesCommand));
                CommandFactory.RegisterCommand(typeof(SummarizeFileCommand));

                //CommandFactory.RegisterCommand(typeof(ReadLocalFileCommand));
                //CommandFactory.RegisterCommand(typeof(WriteLocalFileCommand));
            }

            if (Config.AllowCMDAccess)
            {
                CommandFactory.RegisterCommand(typeof(RunWindowsCommandPromptCommand));
            }

            CommandFactory.RegisterCommand(typeof(CalculateExpressionCommand));

          

            if (Config.PlanAhead)
                Planner = new TaskBreakdownPlanner(this, AbilitiesPrompt);

        }
        public async Task<string> AnalyzeInputWithGPT4Async(string prompt)
        {
            var result = await OpenAIApi.Completions.CreateCompletionAsync(new CompletionRequest(prompt, model: Model.GPT4, temperature: 0.1));

            Tokens += result.Usage.TotalTokens;
            return result.Completions[0].Text;
        }
        public async Task HandleUserRequestAsync(string userRequest)
        {
            if (Config.UseMemory)
            {
                //SendMessageToUI(Role.System, "Using Memory-Feature. Spinning up Weaviate Vector Database...");
                //var flurlClient = new FlurlClient();
               
                //Console.WriteLine(meta.Error != null ? meta.Error.Message : meta.Result.Version);
                
                //SendMessageToUI(Role.System, $"Weaviate Version {weaviateVersion} found.");


            }
            //We are already processing a request so this is additional info from the user
            if (!string.IsNullOrEmpty(UserRequest))
            {
                OnNewUserMessage(userRequest);
                return;
            } else
            {
                //Initial Planning:
                if (Config.PlanAhead)
                {
                    StepsPlanned = await Planner.CreateHighLevelBreakdown(userRequest);
                    SendMessageToUI(Role.System, $"Planned steps: {String.Join("\n", StepsPlanned)}");
                }
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
            }
            catch (AuthenticationException ex)
            {
                SendMessageToUI(Role.Error, $"Error:\n\"{ex.Message}\"\n\nDid you forget to add your API Key to \"{Config.ConfigFile}\"?");
                return;
            }

            // Deserialize the command
            ICommand command = null;
            try
            {
                command = CommandFactory.CreateCommand(jsonResponse);

            } catch (Exception ex)
            {
                SendMessageToUI(Role.Error, $"Error:\n\"{ex.Message}\"\n\nUnable to parse GPTs JSON response.");
                StepsTaken.Add($"The last step could not be executed because the generated JSON to decide which command to run could not be parsed. The instruction you generated: {jsonResponse}\nError: {ex.Message}\nPlease try again but this time make sure your response is valid JSON and can be decoded with JObject.Parse.");

                // Continue the process recursively until a final response is obtained
                await ProcessTask();
                return;
            };
            command.Context = this;


            SendMessageToUI(Role.Thinking, "Subconscious Command: " + command.GetPrompt());

            // Check if the command is the final response
            if (command is FinalResponseCommand finalResponse)
            {

                //Messages.Add(new AssistantMessage { Text = finalResponse.Text });
                SendMessageToUI(Role.AI, finalResponse.Text);


                //Reset
                UserRequest = string.Empty;
                StepsTaken.Clear();
                StepsPlanned.Clear();
                SendMessageToUI(Role.System, "Request concluded.");

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
            string additionalKnowledge = string.Empty;

            string prompt = "";
            var Abilities = "List of Available Skills and Functions:" + CommandFactory.CommandList();
            var JTBD = string.Empty;
            if (Config.PlanAhead)
                JTBD = "High-Level Jobs-to-be-Done: "+ String.Join("\n", StepsPlanned) + "\n";
            var AbilityHistory = string.Empty;
            if(StepsTaken.Count>0)
                AbilityHistory = " Skills / Functions Executed and Their Results: "+ String.Join("\n", StepsTaken) + "\n";
            prompt = $@"As an AI chatbot backend, you are responsible for processing user requests and not directly interacting with the frontend user. Based on the information provided, please choose a suitable function, method or skill from the list of available skills and functions to process the user's request. You can also include additional steps if necessary.

                User Request: {UserRequest}
                {JTBD}
                {Abilities}
                {AbilityHistory}

                Select the appropriate function or skill with the required parameters by providing a JSON formatted message that can be parsed by C# .NET. Your response should follow this format:
                {{
                 ""functionName"": ""chosenFunction"",
                ""parameters"": {{
                   ""parameter1"": ""value1"",
                    ""parameter2"": ""value2"",...
                  }}
                }} ";
            
            
            //AddMessage(Role.System,"Subconsciousness:\n" + prompt);

            // Refine the prompt by checking for loops, false assumptions, and improving the focus on the user's goal
            //string refinedPrompt = await RefinePromptAsync(userInput, StepsByAI, prompt);

            //ProcessingMessage(true);
            var request = new ChatRequest();
            var result = await OpenAIApi.Chat.CreateChatCompletionAsync(new ChatRequest()
            {
                Model = Model.GPT4,
                Temperature = 0.1,
                Messages = new ChatMessage[] {
                    new ChatMessage(ChatMessageRole.System, prompt)
                }
              });

            //var result = await OpenAIApi.Completions.CreateCompletionAsync(prompt);
            //ProcessingMessage(false);

            Tokens += result.Usage.TotalTokens;

            //Revise plan
            if (false)
            {
                await Planner.RevisePlan(UserRequest, StepsPlanned, StepsTaken);
                SendMessageToUI(Role.System, $"New plan :{String.Join("\n", StepsPlanned)}");
            }

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

            var result = await OpenAIApi.Completions.CreateCompletionAsync(new CompletionRequest(gptPrompt, model: Model.GPT4, temperature: 0.1));
            Tokens += result.Usage.TotalTokens;

            string refinedPrompt = result.Completions[0].Text;
            return refinedPrompt.Trim();
        }
        public async Task<string> SummarizeFilesContent(string filename, string content)
        {
            string gptPrompt = $"The file \"{filename}\" contains the following data:\n {content} \n-------------\n Summarize the files content and it's purpose in a few sentences so an AI agent can use this information to work with the files.";

            var result = await OpenAIApi.Chat.CreateChatCompletionAsync(new ChatRequest()
            {
                Model = Model.ChatGPTTurbo,
                Temperature = 0.1,
                Messages = new ChatMessage[] {
                    new ChatMessage(ChatMessageRole.System, gptPrompt)
                }
            });

            Tokens += result.Usage.TotalTokens;

            return result.Choices[0].Message.Content.Trim();

        }
        public ICommand DeserializeCommand(string jsonResponse)
        {
            var json = JObject.Parse(jsonResponse);
            var commandName = json["action"]?.ToString();

            if (commandName != null && CommandTypes.TryGetValue(commandName, out Type commandType))
            {
                var command = (ICommand)System.Text.Json.JsonSerializer.Deserialize(jsonResponse, commandType);

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
       
            public async Task<List<string>> GenerateKeywords(string text)
        {

            string keywordExtractionPrompt = $"Extract the main keywords from the following text and return a comma seperated list only: \"{text}\"";
            var result = await OpenAIApi.Completions.CreateCompletionAsync(new CompletionRequest(keywordExtractionPrompt, model: Model.GPT4, temperature: 0.1));

            Tokens += result.Usage.TotalTokens;

            List<string> keywords = result.Completions[0].Text.Split(',').ToList();

            return keywords;
        }
        public async Task<string> CreateSummary(string text)
        {

            string extractionPrompt = $"Your task is to extract relevant information from the following prompt or result in a manner that it can be easily used for generating embedding vectors for data relevant data storage and retrieval from a vector database: \"{text}\"";
            var result = await OpenAIApi.Chat.CreateChatCompletionAsync(extractionPrompt);
            Tokens += result.Usage.TotalTokens;
           
            return result.Choices[0].Message.Content;
        }

        public async Task<float[]> CreateEmbeddingVector(string text)
        {

            string extractionPrompt = await CreateSummary(text);
            var embedding = await OpenAIApi.Embeddings.GetEmbeddingsAsync(extractionPrompt);
            return embedding;
        }

    }
}
