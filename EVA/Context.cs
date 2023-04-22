// ------------------------------------------------------------------------------
// Project: AI Assistant
// Author: Dr. Dennis "Captain P. Star" Meyer
// Copyright (c) 2023 Dr. Dennis "Captain P. Star" Meyer
// Date: 22.04.2023
// License: GNU General Public License v3.0
// ------------------------------------------------------------------------------
//
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program. If not, see <https://www.gnu.org/licenses/>.
//
// ------------------------------------------------------------------------------

using OpenAI_API.Chat;
using OpenAI_API;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EVA.Commands;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Data;
using System.Speech.Synthesis;
using System.Collections.ObjectModel;
using static System.Net.Mime.MediaTypeNames;
using System.ComponentModel;
using System.Windows.Threading;
using FParsec;

namespace EVA
{
    public class Context : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public readonly OpenAIAPI openAIApi;
        public readonly Conversation chat;
        private List<OpenAI_API.Models.Model> models;
        public ObservableCollection<Message> Messages { get; } = new ObservableCollection<Message>();
        private Dictionary<string, Type> CommandTypes { get; } = new Dictionary<string, Type>();
        public bool HasActiveConversation { get; private set; } = false;
        public bool IsProcessing { get; private set; } = false;
        public Config Config { get; private set; }

        public string Cost { 
            get { 
                return Math.Round(((Tokens / 1000.0) * 0.2),1).ToString()+ " Cent"; 
            } 
        }
        public int Tokens { get; set; } = 0;
        public Context(Config config)
        {
            this.Config = config;
            openAIApi = new OpenAIAPI(Config.OpenAIAccessToken);
            //models = openAIApi.Models.GetModelsAsync().GetAwaiter().GetResult();
            //openAIApi.Completions.DefaultCompletionRequestArgs.Model = 
            chat = openAIApi.Chat.CreateConversation();

            SetupSystemMessages();


            CommandTypes.Add((new AskUserCommand()).CommandName, typeof(AskUserCommand));
            CommandTypes.Add((new AskWikipediaCommand()).CommandName, typeof(AskWikipediaCommand));
            CommandTypes.Add((new FinalResponseCommand()).CommandName, typeof(FinalResponseCommand));
            CommandTypes.Add((new ProcessWithGPTCommand()).CommandName, typeof(ProcessWithGPTCommand));
            CommandTypes.Add((new VoiceOutputCommand()).CommandName, typeof(VoiceOutputCommand));
            CommandTypes.Add((new ListLocalFilesCommand()).CommandName, typeof(ListLocalFilesCommand));
            CommandTypes.Add((new ReadLocalFileCommand()).CommandName, typeof(ReadLocalFileCommand));
            CommandTypes.Add((new WriteLocalFileCommand()).CommandName, typeof(WriteLocalFileCommand));
            CommandTypes.Add((new CalculateExpressionCommand()).CommandName, typeof(CalculateExpressionCommand));
            Config = config;
        }
        public void SetupSystemMessages()
        {
            // Add your system messages here
            // Example:
            // chat.AppendSystemMessage("Your system message");
        }
        public async Task<string> AnalyzeInputWithGPT4Async(string prompt)
        {
            // Implement your GPT-4 API call here
            // Example:
            // return await chat.StreamResponseFromChatbotAsync(...);

            var result = await openAIApi.Chat.CreateChatCompletionAsync(prompt);
            Tokens += result.Usage.TotalTokens;
            return result.Choices[0].Message.Content;
        }
        public async Task<string> HandleUserRequestAsync(string userRequest,List<string> intermediateResults = null)
        {


            if (intermediateResults == null)
                intermediateResults = new List<string>();

            // First, get the suggested command from the LLM
            var jsonResponse = await SubconsciousnessActionAsync(userRequest, intermediateResults);

            // Deserialize the command
            var command = DeserializeCommand(jsonResponse);
            command.Context = this;

         
            AddMessage(Role.System, "Subconscious Command: " + command.GetPrompt());
            // Check if the command is the final response
            if (command is FinalResponseCommand finalResponse)
            {
                
                //Messages.Add(new AssistantMessage { Text = finalResponse.Text });
                AddMessage(Role.AI, finalResponse.Text);

                HasActiveConversation = false;

                return finalResponse.Text;
            }

            // Execute the command and get the result
            ProcessingMessage(true);
            await command.Execute();
            ProcessingMessage(false);

            if (command is AskUserCommand question)
            {
                // Create a TaskCompletionSource to wait for the next user message
                TaskCompletionSource<string> tcs = new TaskCompletionSource<string>();

                // Subscribe to an event that will be triggered when a new user message is added
                NewUserMessage += (sender, userMessage) =>
                {
                    tcs.TrySetResult(userMessage.Text);
                };

                // Wait for the user's message
                question.Result = await tcs.Task;
                //question.Result = (Messages.Last() as UserMessage).Text;
            }

            string result = command.GetResult("");
            intermediateResults.Add(result);
            //Messages.Add(new SystemMessage { Text = "Subconsciousness:\n" + result });

            // Combine the original user request and the command result to create a new prompt
            //string newPrompt = $"Request: {userRequest}\n Processing Step: {result}";

            // Continue the process recursively until a final response is obtained
            return await HandleUserRequestAsync(userRequest, intermediateResults);
        }

  

        public async Task<string> SubconsciousnessActionAsync(string userInput, List<string> StepsByAI)
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

            prompt += $"User request:\n\"{userInput}\"\n";
            prompt += "\nSteps and tasks already done by the AI:\n";
            //if(StepsByAI.Count==0)
            //    prompt += "Nothing yet.\n\n";
            foreach (var s in StepsByAI)
                prompt += s + "\n";
            prompt += "If the user request can be answered with this information, give a final response with the \"respond\" command. Otherwise try another command. Avoid loops like endlessly checking the same wikipedia article. Make sure the output is in JSON format like specified in the command list.";
            //AddMessage(Role.System,"Subconsciousness:\n" + prompt);

            // Refine the prompt by checking for loops, false assumptions, and improving the focus on the user's goal
            string refinedPrompt = await RefinePromptAsync(userInput, StepsByAI, prompt);

            //openAIApi.Completions.DefaultCompletionRequestArgs.Model = openAIApi.Models.RetrieveModelDetailsAsync
            ProcessingMessage(true);
            var result = await openAIApi.Chat.CreateChatCompletionAsync(prompt);
            ProcessingMessage(false);
            Tokens += result.Usage.TotalTokens;
            return result.Choices[0].Message.Content;
            // or);
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

            // Check for false assumptions
            // Customize this part to detect any specific false assumptions in your application
            // ...

            // If a loop or false assumption is detected, update the prompt to avoid them
            if (loopDetected || falseAssumptionDetected)
            {
                refinedPrompt += "\nImportant: ";
                if (loopDetected)
                {
                    refinedPrompt += "The AI has detected a loop in the previous steps. ";
                }
                if (falseAssumptionDetected)
                {
                    refinedPrompt += "The AI has detected a false assumption in the previous steps. ";
                }
                refinedPrompt += "Please take this into account and avoid repeating the same actions. Focus on the user's goal and provide a helpful and accurate response.";
            }
            if(Config.RefinePromptsWithGPT)
                refinedPrompt = await RefinePromptWithGPTAsync(refinedPrompt);
            // Optionally, you can add any other refinements or improvements to the prompt here
            // ...

            return refinedPrompt;
        }

        public async Task<string> RefinePromptWithGPTAsync(string prompt)
        {
            string gptPrompt = $"The following is a prompt for an AI assistant: \"{prompt}\". Please improve and refine the prompt to make it more clear and effective.";

            var result = await openAIApi.Chat.CreateChatCompletionAsync(gptPrompt);
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
        public delegate void NewUserMessageEventHandler(object sender, UserMessage userMessage);
        public event NewUserMessageEventHandler NewUserMessage;

        protected virtual void OnNewUserMessage(UserMessage userMessage)
        {
            NewUserMessage?.Invoke(this, userMessage);
        }

        public enum Role {User,AI, System }
        public void AddMessage(Role role, string text)
        {
            switch (role)
            {
                case Role.User:
                    var userMessage = new UserMessage { Text = "User:\n"+text };

                    App.Current.Dispatcher.InvokeAsync(() => Messages.Add(userMessage));


                    // Raise the event when a new user message is added
                    OnNewUserMessage(userMessage);
                    if (!HasActiveConversation)
                    {
                        HasActiveConversation = true;
                        HandleUserRequestAsync(text);
                    }
                    break;
                case Role.AI:
                    var aiMessage = new AssistantMessage { Text = "E.V.A:\n" + text };
                    App.Current.Dispatcher.InvokeAsync(() => Messages.Add(aiMessage)); 


                    break;
                    case Role.System:
                    var systemMessage = new SystemMessage { Text = text };

                    App.Current.Dispatcher.InvokeAsync(() => Messages.Add(systemMessage));

                    break;
                default:    break;
            }
           
        }
        void ProcessingMessage(bool processing=true)
        {
            if (processing)
            {
                var procMessage = new Processing {  };
                App.Current.Dispatcher.InvokeAsync(() => Messages.Add(procMessage));
            } else
            {
                App.Current.Dispatcher.InvokeAsync(() =>
                {
                    for (int i = Messages.Count - 1; i >= 0; i--)
                    {
                        if (Messages[i].GetType() == typeof(Processing))
                        {
                            Messages.RemoveAt(i);
                        }
                    }
                });
            }
        }
    }
}
