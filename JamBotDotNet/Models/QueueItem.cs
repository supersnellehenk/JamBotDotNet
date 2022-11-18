using YoutubeExplode.Videos;
using YoutubeExplode.Videos.Streams;

namespace JamBotDotNet.Models;

public class QueueItem
{
    public VideoId Id { get; set; }
    public Video? videoMetadata { get; set; }
    public IStreamInfo? StreamInfo { get; set; }

    public override string ToString()
    {
        return $"{videoMetadata?.Title} - {videoMetadata?.Duration}";
    }
}