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

using MathNet.Symbolics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EVA.Commands
{
    public class CalculateExpressionCommand : ICommand
    {
        public string Expression
        {
            get => CustomProperties.ContainsKey("expression") ? CustomProperties["expression"].ToString() : null;
            set => CustomProperties["expression"] = value;
        }

        public CalculateExpressionCommand()
        {
            CommandName = "CalculateExpression";
            Description = "Evaluate a mathematical expression or perform calculations like unit conversions.";
            Expression = "Enter your mathematical expression here";
        }

        public override async Task Execute()
        {
            try
            {
                // Use MathNet.Symbolics for parsing and evaluating the expression
                var expression = Infix.ParseOrThrow(Expression);
                var simplifiedExpression = Algebraic.Expand(expression);
                var result = simplifiedExpression.ToString();

                Result = result;
            }
            catch (Exception ex)
            {
                Result = $"Error: {ex.Message}";
            }
        }

        public override string GetResult(string originalPrompt)
        {
            string result = $"AI assistant decided to calculate the expression \"{Expression}\" using command \"{CommandName}\". The result is: {Result}\n";
            return result;
        }
    }
}
