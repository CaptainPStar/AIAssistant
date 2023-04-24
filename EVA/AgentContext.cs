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
using IO.Milvus.Client;
using Flurl.Http;
using SearchPioneer.Weaviate.Client;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using EVA.Weaviate;
using Flurl.Http.Configuration;
using System.Globalization;
using GraphQL.Client.Http;
using GraphQL.Client.Abstractions;
using System.Data;
using GraphQL;
using System.Net.Http.Headers;
using System.Net.Http;
using ControlzEx.Standard;

namespace EVA
{
    public enum Role { User, AI, Thinking, System , Error}
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
        public ObservableCollection<Message> Messages { get; } = new ObservableCollection<Message>();
        private Dictionary<string, Type> CommandTypes { get; } = new Dictionary<string, Type>();
        public bool HasActiveConversation { get; private set; } = false;
        public bool IsProcessing { get; private set; } = false;
        public Config Config { get; private set; }
        public List<string> StepsTaken { get; private set; } = new List<string>();
        public List<Command> PlannedCommands { get; private set; } = new List<Command>();
        public WeaviateClient Database { private set; get; } = null;
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

            CommandTypes.Add((new AskUserCommand()).CommandName, typeof(AskUserCommand));
            CommandTypes.Add((new AskWikipediaCommand()).CommandName, typeof(AskWikipediaCommand));
            CommandTypes.Add((new FinalResponseCommand()).CommandName, typeof(FinalResponseCommand));
            CommandTypes.Add((new ProcessWithGPTCommand()).CommandName, typeof(ProcessWithGPTCommand));
            //CommandTypes.Add((new VoiceOutputCommand()).CommandName, typeof(VoiceOutputCommand));
            if (Config.UseFileIO)
            {
                CommandTypes.Add((new ListLocalFilesCommand()).CommandName, typeof(ListLocalFilesCommand));
                CommandTypes.Add((new ReadLocalFileCommand()).CommandName, typeof(ReadLocalFileCommand));
                CommandTypes.Add((new WriteLocalFileCommand()).CommandName, typeof(WriteLocalFileCommand));
            }

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
            if (Config.UseMemory && Database == null)
            {
                SendMessageToUI(Role.System, "Using Memory-Feature. Spinning up Weaviate Vector Database...");
                var flurlClient = new FlurlClient();
                Database = new WeaviateClient(new SearchPioneer.Weaviate.Client.Config("http", "localhost:8080"), flurlClient);
                var meta = Database.Misc.Meta();
                //Console.WriteLine(meta.Error != null ? meta.Error.Message : meta.Result.Version);
                string weaviateVersion = meta.Error != null ? meta.Error.Message : meta.Result.Version;
                SendMessageToUI(Role.System, $"Weaviate Version {weaviateVersion} found.");
                //client.query.aggregate(< ClassName >).with_meta_count().do ()


                //await SaveAsMemoryAsync("Request by the user: " + UserRequest);

            }
            //We are already processing a request so this is additional info from the user
            if (!string.IsNullOrEmpty(UserRequest))
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
            }
            catch (AuthenticationException ex)
            {
                SendMessageToUI(Role.Error, $"Error:\n\"{ex.Message}\"\n\nDid you forget to add your API Key to \"{Config.ConfigFile}\"?");
                return;
            }

            // Deserialize the command
            var command = DeserializeCommand(jsonResponse);
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
            prompt = "As part of an AI assistant it is your task to selects the most appropriate programmatic command from a list of possible commands based on a useriput and the AIs steps to solve the task so far. Depending on the command fill in all the necessary parameters of the command based on the userinput.";
            if (Config.UseMemory)
            {
                prompt += "You are also assistent by a persistent memory that might recall earlier UserRequests and the processed responses.\n";
            }
                prompt += "Available commands:" + "\n";
            foreach (var a in CommandTypes)
            {
                var cmd = (Command)Activator.CreateInstance(a.Value);
                prompt += cmd.Description + ": " + cmd.GetPrompt() + "\n";
            }
            prompt += "Respond only in the JSON format that can be parsed with JSON.parse of the selected action. Only use actions from the provided list! Fill in the template \"{\"action\":\"NameofActionfromList\",... other parameter specified in the list based on the useriput}\"";

            prompt += $"User request:\n\"{UserRequest}\"\n";

            if (Config.UseMemory)
            {
                // Create a Memory object

                string memoryLookup = "";
                memoryLookup += $"Original user Request: {UserRequest}\n";
                memoryLookup += $"Steps taken by AI assistant:";
                foreach(var s in StepsTaken)
                {
                    memoryLookup += $"{s}\n";
                }
                var mems = await FindSimilarMemoriesAsync(memoryLookup);
                if(mems.Count > 0)
                {
                    prompt += $"You remember {mems.Count} information from earlier tasks and conversations:";
                    int c = 1;
                    foreach (var m in mems)
                    {
                        prompt += $"Memory {c}:\nKeywords: {String.Join(", ", m.Keywords)}\nSummary: {m.Summary}\nText: {m.Text}\n\n";
                        c++;

                    }
                    prompt += "If this information already answers the users input, give the final answer to the user.";
                }

                //var r = result.Result.Data;
            }

            prompt += "\nSteps and tasks already taken:\n";
            if (StepsTaken.Count == 0)
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
                var command = (Command)System.Text.Json.JsonSerializer.Deserialize(jsonResponse, commandType);

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
        public async Task<List<Memory>> FindSimilarMemoriesAsync(string text)
        {
            //var embedding = await OpenAIApi.Embeddings.GetEmbeddingsAsync(UserRequest);
            var embedding = await CreateEmbeddingVector(text);
            //FindSimilarMemoriesAsync(embedding);
            var request = new GraphGetRequest();
            //request.NearVector = new NearVector() { Vector = embedding };
            request.Class = "Memory";
            request.NearVector = new NearVector() { Vector = embedding, Certainty = 0.85f };
            request.Fields = new Field[] { "summary","text","keywords" };
            request.Limit = 3;
            var memorySearch = await Database.Graph.GetAsync(request);

            var memoryResponse = System.Text.Json.JsonSerializer.Deserialize<GraphQLMemoryResponse>(memorySearch.Result.Data);

            
            return memoryResponse.Get.Memory.ToList();

        }
        public async Task SaveAsMemoryAsync(string text)
        {
            var summary = await CreateSummary(text);
            var embedding = await OpenAIApi.Embeddings.GetEmbeddingsAsync(summary);
            var keywords = await GenerateKeywords(text);

            // Serialize Memory object to JSON
            var obj = new CreateObjectRequest("Memory");
            obj.Vector = embedding;
            obj.Properties = new Dictionary<string, object> { { "summary", summary }, { "text", text }, { "keywords" , keywords } };
            var dbResult = await Database.Data.CreateAsync(obj);


        }
            public async Task<List<string>> GenerateKeywords(string text)
        {

            string keywordExtractionPrompt = $"Extract the main keywords from the following text and return a comma seperated list only: \"{text}\"";
            var result = await OpenAIApi.Chat.CreateChatCompletionAsync(keywordExtractionPrompt);
            Tokens += result.Usage.TotalTokens;

            List<string> keywords = result.Choices[0].Message.Content.Split(',').ToList();

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

         public class GraphQLMemoryResponse
        {
            public GetObject Get { get; set; }

            public class GetObject
            {
                public Memory[] Memory { get; set; }
            }
        }
    }
}
