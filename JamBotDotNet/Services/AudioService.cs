using CliWrap;
using Discord.Audio;
using JamBotDotNet.Models;

namespace JamBotDotNet.Services;

public class AudioService
{
    private CancellationTokenSource? _cancellationTokenSource;
    private AudioOutStream? _audioOutStream;
    public IAudioClient audioClient { get; set; }
    public bool isPlaying { get; set; } = false;
    public async Task TransmitAudioAsync(Stream audioStream)
    {
        var memoryStream = new MemoryStream();
        await Cli.Wrap("ffmpeg")
            .WithArguments(" -hide_banner -loglevel panic -i pipe:0 -ac 2 -f s16le -ar 48000 pipe:1")
            .WithStandardInputPipe(PipeSource.FromStream(audioStream))
            .WithStandardOutputPipe(PipeTarget.ToStream(memoryStream))
            .ExecuteAsync();

        _cancellationTokenSource ??= new CancellationTokenSource();
        _audioOutStream ??= audioClient.CreatePCMStream(AudioApplication.Music);

        isPlaying = true;
        await _audioOutStream.WriteAsync(memoryStream.ToArray().AsMemory(0, (int)memoryStream.Length), _cancellationTokenSource.Token);
        isPlaying = false;
    }
    
    public async Task StopTransmitting()
    {
        _cancellationTokenSource?.Cancel();
        _cancellationTokenSource?.Dispose();
        _cancellationTokenSource = null;
        isPlaying = false;
    }
}