using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EVA.Commands
{
    public class SummarizeFileCommand : ICommand
    {
        public string FilePath
        {
            get => CustomProperties.ContainsKey("FilePath") ? CustomProperties["FilePath"].ToString() : null;
            set => CustomProperties["FilePath"] = value;
        }

        public SummarizeFileCommand()
        {
            CommandName = "summarizeFile";
            Description = "Summarize a text or code file";
            FilePath = "Path of the file";
        }

        public override async Task Execute()
        {
            try
            {
                string fileContent = File.ReadAllText(FilePath);

                // This example uses GPT-4 to summarize the content.
                // Replace this line with your own GPT-4 summarization implementation.
                string summary = await Context.SummarizeFilesContent(FilePath, fileContent);

                Result = summary;
            }
            catch (Exception ex)
            {
                Result = $"Error while summarizing file: {ex.Message}";
            }
        }

        public override string GetResult(string originalPrompt)
        {
            string result = $"AI assistant summarized the file \"{FilePath}\" using command \"{CommandName}\". The summary is: \"{Result}\".\n";
            return result;
        }
    }
}
