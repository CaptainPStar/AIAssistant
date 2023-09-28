using EVA.Commands;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EVA
{
    public class MainWindowView : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }


        public Action<Role,string> MessageReceivedCallback { get; set; }
        
        public ObservableCollection<Message> Messages { get; } = new ObservableCollection<Message>();

        private string _userRequest;
        public string UserRequest
        {
            get { return _userRequest; }
            set
            {
                _userRequest = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(UserRequest)));
            }
        }
        public ObservableCollection<CommandViewModel> Commands { get; set; }
        public AgentContext AgentContext { get; private set; }

        public MainWindowView(AgentContext agentContext)
        {
            AgentContext = agentContext;

            //Give the AgentContext the callback to add chat messages to the UI
            agentContext.MessageReceivedCallback += AddMessage;

                Commands = new ObservableCollection<CommandViewModel>();

                // Load available commands/functions from the CommandFactory
                LoadCommands();
            }

        private void LoadCommands()
        {
                // Use CommandFactory to get available commands/functions
                var availableCommands = AgentContext.Commands.GetAvailableCommands();

            foreach (var command in availableCommands)
            {
                Commands.Add(new CommandViewModel { Name = command.Item1, IsEnabled = command.Item2, Description = "Description here" });
            }
        }
        public void OnMessageReceived(Role role, string message)
        {
            // Process the received message and update the UI accordingly
            // For example, add the message to the chat window
            AddMessage(role, message);
        }

        // Implement methods for adding messages, handling user input, and updating the UI.
        public void AddMessage(Role role, string text)
        {
            switch (role)
            {
                case Role.User:
                    var userMessage = new UserMessage { Text = "User:\n" + text };

                    App.Current.Dispatcher.InvokeAsync(() => Messages.Add(userMessage));

                   
                    break;
                case Role.AI:
                    var aiMessage = new AssistantMessage { Text = "E.V.A:\n" + text };
                    App.Current.Dispatcher.InvokeAsync(() => Messages.Add(aiMessage));


                    break;
                case Role.System:
                    var systemMessage = new SystemMessage { Text = text };

                    App.Current.Dispatcher.InvokeAsync(() => Messages.Add(systemMessage));

                    break;
                case Role.Thinking:
                    var thinkMessage = new ThinkingMessage { Text = text };

                    App.Current.Dispatcher.InvokeAsync(() => Messages.Add(thinkMessage));

                    break;
                case Role.Error:
                    var errorMessage = new ErrorMessage { Text = text };

                    App.Current.Dispatcher.InvokeAsync(() => Messages.Add(errorMessage));

                    break;
                default: break;
            }

        }
        void ProcessingMessage(bool processing = true)
        {
            if (processing)
            {
                var procMessage = new Processing { };
                App.Current.Dispatcher.InvokeAsync(() => Messages.Add(procMessage));
            }
            else
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
