using System;
using System.IO;
using System.Threading.Tasks;
using YoutubeExplode;
using YoutubeExplode.Videos;
using YoutubeExplode.Videos.Streams;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;

public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        // Get the YouTube video URL
        var videoIdOrUrl = args.Length == 1 ? args[0] : null;
        if (string.IsNullOrEmpty(videoIdOrUrl))
        {
            Console.Error.WriteLine("USAGE: ytd ID");
            Console.Error.WriteLine("   OR: ytd URL");
            return 1;
        }

        // Get the Azure Speech endpoint and subscription key
        var region = Environment.GetEnvironmentVariable("AZURE_AI_SPEECH_REGION");
        var key = Environment.GetEnvironmentVariable("AZURE_AI_SPEECH_KEY");
        if (string.IsNullOrEmpty(region) || string.IsNullOrEmpty(key))
        {
            Console.Error.WriteLine("ERROR: Environment variables AZURE_AI_SPEECH_REGION and/or AZURE_AI_SPEECH_KEY are not set.");
            return 2;
        }

        // Download the video and recognize the speech content
        var fileName = await DownloadVideo(videoIdOrUrl);
        var transcript = await RecognizeSpeech(region, key, fileName);

        return 0;
    }

    private async static Task<string> DownloadVideo(string videoIdOrUrl)
    {
        var client = new YoutubeClient();

        var streamManifest = await client.Videos.Streams.GetManifestAsync(videoIdOrUrl);
        var streamInfo = streamManifest.GetAudioOnlyStreams().GetWithHighestBitrate();
        if (streamInfo is null)
        {
            Console.Error.WriteLine("WARNING! This video has no audio streams.");
            System.Environment.Exit(3);
        }

        var fileName = $"{videoIdOrUrl}.{streamInfo.Container.Name}";
        if (!Console.IsOutputRedirected)
        {
            Console.Write($"Downloading audio to {fileName} ... ");
        }

        var progress = new ConsoleProgress();
        await client.Videos.Streams.DownloadAsync(streamInfo, fileName, progress);
        if (!Console.IsOutputRedirected)
        {
            Console.WriteLine(" ... Done!");
            Console.WriteLine();
        }

        return fileName;
    }

    private static async Task<string> RecognizeSpeech(string region, string key, string audioFile)
    {
        // Create a speech config from the subscription key and region
        var speechConfig = SpeechConfig.FromSubscription(key, region);
        speechConfig.SpeechRecognitionLanguage = "en-US";

        // Create an audio config from the audio file
        var audioConfig = CreateAudioConfigFromFile(audioFile, "any");
        var recognizer = new SpeechRecognizer(speechConfig, audioConfig);

        // We'll use a task completion source to know when the recognition is done
        var transcript = string.Empty;
        var finished = new TaskCompletionSource<int>();

        // Handle the events
        recognizer.Recognizing += (s, e) => HandleRecognizingEvent(e);
        recognizer.Recognized += (s, e) => HandleRecognizedEvent(e, ref transcript);
        recognizer.Canceled += (s, e) => HandleCanceledEvent(e, finished);

        // Start the recognition, and wait for it to finish
        await recognizer.StartContinuousRecognitionAsync();
        await finished.Task;

        return transcript;
    }

    private static void HandleRecognizingEvent(SpeechRecognitionEventArgs e)
    {
        if (!Console.IsOutputRedirected && !string.IsNullOrEmpty(e.Result.Text))
        {
            // if the text is too big to print with a trailing '...', then, print it with a leading '...' and ensure it doesn't exceed the console width
            var text = e.Result.Text.Length > Console.WindowWidth - 9 // (9 = 3 for the leading '...', 3 for the trailing '...', and 3 for the space between the '...' and the text)
                ? "... " + e.Result.Text.Substring(e.Result.Text.Length - (Console.WindowWidth - 9)) + " ..."
                : e.Result.Text + "...";

            // erase the last line
            Console.CursorLeft = 0;
            Console.Write(new string(' ', Console.WindowWidth - 1));

            // print the new text
            Console.CursorLeft = 0;
            Console.Write(text);
        }
    }

    private static void HandleRecognizedEvent(SpeechRecognitionEventArgs e, ref string transcript)
    {
        if (e.Result.Reason == ResultReason.RecognizedSpeech && !string.IsNullOrEmpty(e.Result.Text))
        {
            // append the new text
            transcript += e.Result.Text + "\n";

            // erase the last line
            Console.CursorLeft = 0;
            Console.Write(new string(' ', Console.WindowWidth - 1));

            // print the new text
            Console.CursorLeft = 0;
            Console.WriteLine($"{e.Result.Text}\n");
        }
    }

    private static void HandleCanceledEvent(SpeechRecognitionCanceledEventArgs e, TaskCompletionSource<int> finished)
    {
        if (e.Reason == CancellationReason.EndOfStream)
        {
            finished.SetResult(0);
        }
        else if (e.Reason == CancellationReason.Error)
        {
            Console.Error.WriteLine($"CANCELED: ErrorCode={e.ErrorCode}");
            Console.Error.WriteLine($"CANCELED: ErrorDetails={e.ErrorDetails}");
            Console.Error.WriteLine($"CANCELED: Did you update the subscription info?");
            System.Environment.Exit(4);
        }
        else
        {
            System.Environment.Exit(5);
        }
    }

    private static AudioConfig CreateAudioConfigFromFile(string file, string? format)
    {
        return !string.IsNullOrEmpty(format)
            ? AudioConfig.FromStreamInput(CreatePushStream(file, format))
            : AudioConfig.FromWavFileInput(file);
    }

    static public PushAudioInputStream CreatePushStream(string file, string format)
    {
        return !string.IsNullOrEmpty(format)
            ? CreatePushStream(file, ContainerFormatFrom(format))
            : CreatePushStream(file);
    }

    static public AudioStreamContainerFormat ContainerFormatFrom(string format)
    {
        return format switch {
            "any" => AudioStreamContainerFormat.ANY,
            "alaw" => AudioStreamContainerFormat.ALAW,
            "amrnb" => AudioStreamContainerFormat.AMRNB,
            "amrwb" => AudioStreamContainerFormat.AMRWB,
            "flac" => AudioStreamContainerFormat.FLAC,
            "mp3" => AudioStreamContainerFormat.MP3,
            "ogg" => AudioStreamContainerFormat.OGG_OPUS,
            "mulaw" => AudioStreamContainerFormat.MULAW,
            _ => AudioStreamContainerFormat.ANY
        };
    }

    static public PushAudioInputStream CreatePushStream(string file, AudioStreamContainerFormat containerFormat)
    {
        var pushFormat = AudioStreamFormat.GetCompressedFormat(containerFormat);
        var push = AudioInputStream.CreatePushStream(pushFormat);

        push.Write(File.ReadAllBytes(file));
        push.Close();
        
        return push;
    }

    static public PushAudioInputStream CreatePushStream(string file)
    {
        var push = AudioInputStream.CreatePushStream();

        push.Write(File.ReadAllBytes(file));
        push.Close();
        
        return push;
    }
}
