# AI-Assistant E.V.A (Electronic Virtual Assistant)

The AI-Assistant E.V.A is a C# desktop application that uses OpenAI's GPT-3/4 language model to assist users with a variety of tasks.
The project is focused on creating an AI assistant that can respond to user requests, search for information on the internet, summarize text, and more.

This is my learning project for prompt engineering and exploring GPT-3/4. The idea behind this project was slightly inspired by AutoGPT, but I decided to write it from scratch in C# .NET because I always have to reinvent the wheel (also working in large Python projects cause me headaches).

The AI assistant was designed with expandability in mind, and it is my hope that others will contribute new commands to make the AI assistant even more useful.

## Features
- Communicate with the AI assistant through a chat interface
- Ability to complete tasks such as answering questions, searching the web, and performing calculations
- Easy to add new functions by using the command class.

![image](https://user-images.githubusercontent.com/5654543/233810615-71caec2c-7113-4094-833d-580d74605359.png)

## Installation
1. Clone the repository
2. Open the solution file in Visual Studio
3. Restore NuGet packages
4. Build, run and close the application
5. Add your OpenAI API key to the newly created config.json
6. Restart the application

## Usage
1. Type a message in the chat textbox and press the "Send" button to send the message to the AI assistant
2. The AI assistant will respond with its best answer or request additional information if necessary
3. The AI assistant can also chain tasks to collect information from multiple sources.

## Contributing
Contributions to AI-Assistant are welcome and encouraged! If you would like to contribute, please follow the steps below:
1. Fork the repository
2. Create a new branch for your changes
3. Make your changes and commit them
4. Push your changes to your fork
5. Create a pull request to merge your changes into the main repository

## License
This project is released under the GPL-3.0 license. See `LICENSE.md` for more information.
