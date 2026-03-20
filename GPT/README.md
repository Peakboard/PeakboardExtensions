# Peakboard Extension: GPT (OpenAI)

This extension integrates Peakboard with the [OpenAI GPT API](https://platform.openai.com/), allowing you to send questions to GPT and receive AI-generated answers as a data source.

## Custom List: GPT Query

Sends a prompt to the OpenAI GPT API and returns the response as a Peakboard data source.

### Configuration

| Parameter | Description |
|-----------|-------------|
| Token | OpenAI API key (obtain from the [OpenAI API dashboard](https://platform.openai.com/api-keys)) |
| Question | The prompt/question to send to GPT |

The role of the bot (system message) can be adjusted in the `GPTCustomList.cs` source file.

## Installation

1. Download `PeakboardExtensionGPT.zip` from the `Binaries` folder.
2. Add the extension to Peakboard Designer via **Manage Extensions**.
3. Add a new data source and select **GPT** from the extensions list.
