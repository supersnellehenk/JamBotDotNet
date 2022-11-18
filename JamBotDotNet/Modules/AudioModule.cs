using CliWrap;
using Discord.Audio;

namespace JamBotDotNet.Modules;

using ContextType = Discord.Commands.ContextType;

public class AudioModule
{
    private CancellationTokenSource? _cancellationTokenSource;
    private AudioOutStream? _audioOutStream;
    
    public async Task TransmitAudioAsync(IAudioClient client, Stream audioStream)
    {
        var memoryStream = new MemoryStream();
        await Cli.Wrap("ffmpeg")
            .WithArguments(" -hide_banner -loglevel panic -i pipe:0 -ac 2 -f s16le -ar 48000 pipe:1")
            .WithStandardInputPipe(PipeSource.FromStream(audioStream))
            .WithStandardOutputPipe(PipeTarget.ToStream(memoryStream))
            .ExecuteAsync();

        _cancellationTokenSource ??= new CancellationTokenSource();
        _audioOutStream ??= client.CreatePCMStream(AudioApplication.Music);

        await _audioOutStream.WriteAsync(memoryStream.ToArray().AsMemory(0, (int)memoryStream.Length), _cancellationTokenSource.Token);
    }
    
    public Task StopTransmitting()
    {
        _cancellationTokenSource?.Cancel();
        _cancellationTokenSource?.Dispose();
        _cancellationTokenSource = null;
        
        _audioOutStream?.Dispose();
        _audioOutStream = null;
        return Task.CompletedTask;
    }
}