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

using System;
using System.Buffers.Text;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using MahApps.Metro.Controls;
using OpenAI_API;
using OpenAI_API.Chat;
using OpenAI_API.Completions;
using OpenAI_API.Models;
using System.Speech.Synthesis;
using EVA.Commands;
using Newtonsoft.Json.Linq;
using System.Globalization;
using System.Threading;

namespace EVA
{
   
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MetroWindow
    {
        public MainWindowView View { get; set; }

        public MainWindow()
        {
            // Set the default culture to InvariantCulture, workaround wor Weaviate Client
            CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
            CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;


            InitializeComponent();
            Config cfg = Config.LoadConfig();

            AgentContext context = new AgentContext(cfg);

            View = new MainWindowView(context);
            this.DataContext = View;
            View.Messages.CollectionChanged += Messages_CollectionChanged;
        }
        private async void SendButton_Click(object sender, RoutedEventArgs e)
        {
            string userInput = UserInputTextBox.Text.Trim();
            if (string.IsNullOrEmpty(userInput)) return;

            //ChatListBox.Items.Add(new UserMessage { Text = "User: "+userInput });
            View.AddMessage(Role.User, userInput);

            View.AgentContext.HandleUserRequestAsync(userInput);

            UserInputTextBox.Clear();

        }
        private void Messages_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add)
            {
                // Get the last added message
                var lastMessage = e.NewItems[e.NewItems.Count - 1] as Message;

                // Scroll the ListBox to the last message
                App.Current.Dispatcher.InvokeAsync(() => ChatListBox.ScrollIntoView(lastMessage));
            }
        }

        private void AbortButton_Click(object sender, RoutedEventArgs e)
        {
            
        }

        private void CopyMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (ChatListBox.SelectedItem != null)
            {
                var message = ChatListBox.SelectedItem as Message;
                Clipboard.SetText(message.Text);
            }
        }
    }
}
