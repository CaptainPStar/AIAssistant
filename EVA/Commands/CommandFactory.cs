using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace EVA.Commands
{
    public class CommandFactory
    {
        private Dictionary<string, Type> _availableFunctions;
        private Dictionary<string, bool> _functionsStatus;
        public CommandFactory()
        {
            _availableFunctions = new Dictionary<string, Type>();
            _functionsStatus = new Dictionary<string, bool>();
            LoadPlugins();
        }
        public void RegisterCommand(Type type)
        {
            if (type.IsSubclassOf(typeof(ICommand)))
            {
                // Create an instance of the plugin type to access the FunctionName property
                ICommand pluginInstance = (ICommand)Activator.CreateInstance(type);
                _availableFunctions.Add(pluginInstance.CommandName, type);
                _functionsStatus.Add(pluginInstance.CommandName, pluginInstance.EnabledByDefault);
            }
        }
        private void LoadPlugins()
        {
            string pluginsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "plugins");
            if (!Directory.Exists(pluginsPath))
            {
                Directory.CreateDirectory(pluginsPath);
            }

            // Get all DLL files in the plugins directory
            string[] pluginFiles = Directory.GetFiles(pluginsPath, "*.dll");

            foreach (string pluginFile in pluginFiles)
            {
                try
                {
                    // Load the assembly and get its types
                    Assembly assembly = Assembly.LoadFrom(pluginFile);
                    Type[] types = assembly.GetTypes();

                    // Iterate through types to find ones implementing the IPlugin interface
                    foreach (Type type in types)
                    {
                        if (type.GetInterface(typeof(ICommand).FullName) != null)
                        {
                            // Create an instance of the plugin type to access the FunctionName property
                            ICommand pluginInstance = (ICommand)Activator.CreateInstance(type);
                            _availableFunctions.Add(pluginInstance.CommandName, type);
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Log the exception and continue with the next plugin file
                    Console.WriteLine($"Error loading plugin '{pluginFile}': {ex.Message}");
                }
            }
        }

        public ICommand CreateCommand(string jsonResponse)
        {
            // Parse the JSON response
            JObject parsedJson = JObject.Parse(jsonResponse);
            string functionName = parsedJson["functionName"].Value<string>();

            // Check if the requested function exists in the available plugins
            if (_availableFunctions.TryGetValue(functionName, out Type pluginType))
            {
                // Create an instance of the plugin class
                ICommand command = (ICommand)Activator.CreateInstance(pluginType);

                // Set the properties of the command using the JSON parameters
                JObject jsonParameters = parsedJson["parameters"] as JObject;
                if (jsonParameters != null)
                {
                    foreach (var parameter in jsonParameters)
                    {
                        PropertyInfo property = pluginType.GetProperty(parameter.Key);
                        if (property != null)
                        {
                            object value = Convert.ChangeType(parameter.Value, property.PropertyType);
                            property.SetValue(command, value);
                        }
                        else
                        {
                            throw new InvalidOperationException(
                                $"Property '{parameter.Key}' not found in '{pluginType.Name}'.");
                        }
                    }
                }

                return command;
            }

            // Handle case when the function is not found
            // Return a default command or throw an exception
            throw new InvalidOperationException($"Function '{functionName}' not found.");
        }
         public string CommandList()
        {
            var cmds = string.Empty;
            foreach (var a in _availableFunctions)
            {
                var cmd = (ICommand)Activator.CreateInstance(a.Value);
                cmds += cmd.Description + ": " + cmd.GetPrompt()+"\n";
            }
            return cmds;
        }
        public IEnumerable<Tuple<string,bool>> GetAvailableCommands()
        {
            var result = new List<Tuple<string,bool>>();
            foreach(var f in _availableFunctions.Keys)
            {
                result.Add(new Tuple<string, bool>(f, _functionsStatus.GetValueOrDefault(f, true)));
            }

            return result;
        }
        public void EnableFunction(string functionName, bool enable)
        {
            if (_functionsStatus.ContainsKey(functionName))
            {
                _functionsStatus[functionName] = enable;
            }
        }
    }
}
