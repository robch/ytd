# YouTube Audio to Text Converter

This project provides a simple tool to download YouTube video audio and transcribe it using Azure Cognitive Services' Speech to Text feature.

## Requirements

- .NET Core SDK
- Azure Cognitive Services Speech API subscription
- YouTubeExplode library

## Setup

1. Clone the repository.
2. Set up environment variables for Azure Cognitive Services:
   ```
   export AZURE_AI_SPEECH_REGION=your_region
   export AZURE_AI_SPEECH_KEY=your_key
   ```
3. Install the required NuGet packages:
   ```
   dotnet add package YoutubeExplode
   dotnet add package Microsoft.CognitiveServices.Speech
   ```

## Usage

Run the application with the YouTube video ID or URL as an argument:

```sh
./ytd https://www.youtube.com/watch?v=your_video_id
```
The transcript of the audio will be printed to the console.

## Code Overview

- `Main` method: Entry point of the application. Validates the input and environment variables, then downloads the video and recognizes speech.
- `DownloadVideo` method: Uses `YoutubeExplode` to download the audio stream of the video.
- `RecognizeSpeech` method: Uses Azure Cognitive Services to transcribe the downloaded audio file.
- Event handlers: Handle speech recognition events to update the transcript and handle errors.