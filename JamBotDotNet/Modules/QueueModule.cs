using Discord;
using Discord.Interactions;
using JamBotDotNet.Services;
using YoutubeExplode.Common;

namespace JamBotDotNet.Modules;

public class QueueModule : InteractionModuleBase<SocketInteractionContext>
{
    public QueueService queueService { get; set; }

    [SlashCommand("queue", "Shows the current queue")]
    public async Task Queue()
    {
        var queue = queueService.ToString();

        if (queue.Length == 0)
        {
            queue = "Empty";
        }

        var embed = new EmbedBuilder
        {
            Fields = new List<EmbedFieldBuilder>
            {
                new()
                {
                    Name = "Queue",
                    Value = queue
                }
            }
        };

        await RespondAsync(embed: embed.Build());
    }

    [SlashCommand("clear-queue", "Clears the queue")]
    public async Task ClearQueue()
    {
        queueService.Clear();
        await RespondAsync("Queue cleared");
    }

    [SlashCommand("playing", "Shows the current playing song")]
    public async Task Playing()
    {
        var playingItem = queueService.CurrentlyPlayingItem;

        if (playingItem == null)
        {
            await RespondAsync("Nothing is playing");
            return;
        }

        var embed = new EmbedBuilder()
            .WithTitle($"{playingItem.videoMetadata?.Author} - {playingItem.videoMetadata?.Title} - {playingItem.videoMetadata?.Id}")
            .WithImageUrl(playingItem.videoMetadata?.Thumbnails.GetWithHighestResolution().Url);

        await RespondAsync(embed: embed.Build());
    }
}