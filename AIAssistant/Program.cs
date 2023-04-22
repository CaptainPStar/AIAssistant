using Newtonsoft.Json;
using OpenAI_API;

namespace AIAssistant
{
    internal class Program
    {
        static async Task Main(string[] args)
        {

            Console.WriteLine("Welcome to the Console Chat Assistant!");
            Console.WriteLine("Type 'exit' to end the session.");

            var apiKey = "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";
            var openAIApi = new OpenAIAPI(apiKey);

            var chat = openAIApi.Chat.CreateConversation();
            // Set up the chatbot's behavior
            chat.AppendSystemMessage("You are the core of an assistant AI that responds to user inquiries. You have the ability to respond to the user, query wikipedia or query a memory database of past conversations.");
            chat.AppendSystemMessage("Your response must be in the JSON format only and looks like this: { \"action \" : \"respond/wiki/memory \",text :  \"textresponse, name of a wiki article or a comma seperated list of keywords to query the memory \" }");
            chat.AppendSystemMessage("You decide on your own and based on the information available of you directly respond or do another task (like query the memory db) first.");
            chat.AppendSystemMessage("Your answer must always be valid JSON syntax only!");
            while (true)
            {
                Console.Write("You: ");
                var userInput = Console.ReadLine();

                if (userInput.ToLower() == "exit")
                    break;

                chat.AppendUserInput(userInput);
                Console.Write($"Assistant: ");

               /*await chat.StreamResponseFromChatbotAsync(res =>
                {
                    Console.Write(res);
                });*/
               var result = chat.GetResponseFromChatbotAsync().GetAwaiter().GetResult();

                dynamic json = JsonConvert.DeserializeObject(result);

                switch(json["action"])
                {
                    case "respond":
                        {
                            Console.WriteLine(json["text"]);
                        }
                        break;
                    case "wiki":
                        {
                            Console.WriteLine(json["text"]);
                        }
                        break;
                    case "memory":
                        {
                            Console.WriteLine(json["text"]);
                        }
                        break;
                    default:
                        {
                            Console.WriteLine($"Unknown response: {result}"); 
                        } break;
                }
                Console.WriteLine("");

            }
        }
    }
}