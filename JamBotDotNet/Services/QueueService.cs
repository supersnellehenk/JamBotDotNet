using System.Collections.Concurrent;
using System.Text;
using Discord.Audio;
using JamBotDotNet.Models;
using YoutubeExplode;
using YoutubeExplode.Videos;

namespace JamBotDotNet.Services;

public class QueueService
{
    public AudioService AudioService { get; set; }
    public QueueItem? CurrentlyPlayingItem { get; set; }
    public DateTime? StartedPlaying { get; set; }
    private readonly List<QueueItem> _queue = new();

    public QueueService(AudioService audioService)
    {
        this.AudioService = audioService;
    }

    public void Enqueue(QueueItem item)
    {
        _queue.Add(item);
    }

    public bool Dequeue(VideoId id)
    {
        return _queue.Remove(_queue.FirstOrDefault(x => x.Id == id));
    }

    public override string ToString()
    {
        var sb = new StringBuilder();

        var index = 1;
        foreach (var item in _queue.Select(item => item.videoMetadata))
        {
            sb.AppendLine($"{index++} - {item?.Title} - {item?.Duration}");
        }
        
        return sb.ToString();
    }

    public void Clear()
    {
        _queue.Clear();
    }

    public void DequeueFirst()
    {
        _queue.RemoveAt(0);
    }

    public bool IsEmpty()
    {
        return _queue.Count == 0;
    }

    public async Task Next()
    {
        if (AudioService.isPlaying)
            return;
        var item = _queue.FirstOrDefault();
        if (item == null) return;

        var youtube = new YoutubeClient();

        var audioStream = await youtube.Videos.Streams.GetAsync(item.StreamInfo);

        try
        {
            DequeueFirst();
            CurrentlyPlayingItem = item;
            StartedPlaying = new DateTime();
            await AudioService.TransmitAudioAsync(audioStream);
        }
        finally
        {
            CurrentlyPlayingItem = null;
            StartedPlaying = null;
            await Next();
        }
    }
}