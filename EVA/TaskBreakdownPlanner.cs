using Google.Protobuf.WellKnownTypes;
using OpenAI_API;
using OpenAI_API.Moderation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Speech.Synthesis;
using System.Text;
using System.Threading.Tasks;

namespace EVA
{
    internal class TaskBreakdownPlanner
    {
        private List<string> _abilities;
        private AgentContext _agentContext;
        public TaskBreakdownPlanner(AgentContext context, List<string> abilities)
        {
            _agentContext = context;
            _abilities = abilities;
        }

        public async Task<List<string>> CreateHighLevelBreakdown(string complexTask)
        {
            var prompt = $"Given a complex task: \"{complexTask}\", create a high-level breakdown of JTBD (jobs-to-be-done) to fulfill it, using the following abilities: {string.Join(", ", _abilities)}\nOne step per line. Just discribe the high-level JTBD and not the actual commands/functions.";
            var result = await _agentContext.OpenAIApi.Chat.CreateChatCompletionAsync(prompt);
            _agentContext.Tokens += result.Usage.TotalTokens;


            var tasks = result.Choices.FirstOrDefault()?.Message.Content.Split('\n')
                .Select(task => task.Trim())
                .Where(task => !string.IsNullOrWhiteSpace(task))
                .ToList();

            return tasks;
        }

        public async Task RevisePlan(string complexTask,List<string> originalPlan, List<string> subtaskResults)
        {
            var prompt = $"Given a complex task: \"{complexTask}\", revise and improve a high-level breakdown of JTBD to fulfill it, using the following abilities: {string.Join(", ", _abilities)}\nOriginal plan:\n{string.Join("\n", originalPlan)}\n\nAlready executed steps and their result results:\n{string.Join("\n", subtaskResults)}\nOne step per line. Just refine the high-level JTBD if necessary. Don't write out the actual commands/functions.";
            var result = await _agentContext.OpenAIApi.Chat.CreateChatCompletionAsync(prompt);
            _agentContext.Tokens += result.Usage.TotalTokens;
            var revisedPlan = result.Choices.FirstOrDefault()?.Message.Content.Split('\n')
                .Select(task => task.Trim())
                .Where(task => !string.IsNullOrWhiteSpace(task))
                .ToList();

            originalPlan.Clear();
            originalPlan.AddRange(revisedPlan);
        }
    }
}
