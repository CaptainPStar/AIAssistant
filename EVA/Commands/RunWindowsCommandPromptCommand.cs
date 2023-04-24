using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Text;

namespace EVA.Commands
{
    public class RunWindowsCommandPromptCommand : Command
    {
        public string Command
        {
            get => CustomProperties.ContainsKey("command") ? CustomProperties["command"].ToString() : null;
            set => CustomProperties["command"] = value;
        }

        public RunWindowsCommandPromptCommand()
        {
            CommandName = "RunWindowsCommandPrompt";
            Description = "Run a command in the Windows command prompt and get the result. Specify the command to execute.";
            Command = "eg ipconfig";
        }

        public override async Task Execute()
        {
            try
            {
                using (var process = new Process())
                {
                    process.StartInfo.FileName = "cmd.exe";
                    process.StartInfo.Arguments = $"/C {Command}";
                    process.StartInfo.UseShellExecute = false;
                    process.StartInfo.RedirectStandardOutput = true;
                    process.StartInfo.RedirectStandardError = true;
                    process.StartInfo.CreateNoWindow = true;

                    StringBuilder output = new StringBuilder();

                    process.OutputDataReceived += (sender, e) =>
                    {
                        if (!String.IsNullOrEmpty(e.Data))
                        {
                            output.AppendLine(e.Data);
                        }
                    };

                    process.ErrorDataReceived += (sender, e) =>
                    {
                        if (!String.IsNullOrEmpty(e.Data))
                        {
                            output.AppendLine(e.Data);
                        }
                    };

                    process.Start();
                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();
                    process.WaitForExit();

                    Result = $"Command executed successfully: {Command}. Result: {output}";
                }
            }
            catch (Exception ex)
            {
                Result = $"Error executing command: {Command}. Exception: {ex.Message}";
            }
        }

        public override string GetResult(string originalPrompt)
        {
            string result = $"AI assistant decided to execute command \"{Command}\" using the \"{CommandName}\" ability. Result: \"{Result}\".\n";
            return result;
        }
    }
}