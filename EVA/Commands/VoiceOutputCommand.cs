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
using System.Collections.Generic;
using System.Linq;
using System.Speech.Synthesis;
using System.Text;
using System.Threading.Tasks;

namespace EVA.Commands
{
    public class VoiceOutputCommand : ICommand
    {
        public string Text
        {
            get => CustomProperties.ContainsKey("text") ? CustomProperties["text"].ToString() : null;
            set => CustomProperties["text"] = value;
        }
        public VoiceOutputCommand()
        {
            CommandName = "voiceOutput";
            Description = "Voice output";
            Text = "Text to be spoken by the AI assistent";
        }

        /*public override string GetPrompt()
        {
            return $"{{\"action\":\"{CommandName}\",\"text\":\"{Text}\"}}";
        }*/

        public override Task Execute()
        {
            return Task.Factory.StartNew(() =>
            {
                using (var synthesizer = new SpeechSynthesizer())
                {
                    synthesizer.SetOutputToDefaultAudioDevice();
                    synthesizer.SpeakAsync(Text);
                }
            });
          
        }
        public override string GetResult(string originalPrompt)
        {
            string result = $"AI assistant decided to say \"{Text}\" with text-to-speech using command \"{CommandName}\".";
            return result;
        }
    }

}
