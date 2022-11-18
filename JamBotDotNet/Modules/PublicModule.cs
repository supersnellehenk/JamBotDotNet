using System.Diagnostics;
using Discord;
using Discord.Commands;
using System.IO;
using System.Threading.Tasks;
using CliWrap;
using Discord.Audio;
using Discord.Interactions;
using JamBotDotNet.Services;
using YoutubeExplode;
using YoutubeExplode.Common;
using YoutubeExplode.Videos.Streams;
using ContextType = Discord.Commands.ContextType;
using System;
using System.Text.RegularExpressions;
using JamBotDotNet.Models;
using YoutubeExplode.Videos;

namespace JamBotDotNet.Modules
{
    // Modules must be public and inherit from an IModuleBase
    public class PublicModule : InteractionModuleBase<SocketInteractionContext>
    {
        // Dependency Injection will fill this value in for us
        public PictureService PictureService { get; set; }

        private AudioOutStream? _audioOutStream;
        
        private CancellationTokenSource? _cancellationTokenSource;
        
        public AudioService audioService { get; set; }
        
        public QueueService queueService { get; set; }

        [SlashCommand("ping", "Ping the bot.")]
        public async Task PingAsync()
        {
            await RespondAsync("pong!");
        }

        [SlashCommand("cat", "Get a random cat picture.")]
        public async Task CatAsync()
        {
            // Get a stream containing an image of a cat
            var stream = await PictureService.GetCatPictureAsync();
            // Streams must be seeked to their beginning before being uploaded!
            stream.Seek(0, SeekOrigin.Begin);
            await RespondWithFileAsync(stream, "cat.png");
        }

        // Get info on a user, or the user who invoked the command if one is not specified
        [SlashCommand("userinfo", "Get info on a user.")]
        public async Task UserInfoAsync(IUser user = null)
        {
            user ??= Context.User;

            await RespondAsync($"{user.Username}#{user.Discriminator} - Status: {user.Status}");
        }

        // [Remainder] takes the rest of the command's arguments as one argument, rather than splitting every space
        [SlashCommand("echo", "Echo a message.")]
        public async Task EchoAsync([Remainder] string text)
            // Insert a ZWSP before the text to prevent triggering other bots!
            => await RespondAsync('\u200B' + text);

        // Setting a custom ErrorMessage property will help clarify the precondition error
        [SlashCommand("guild_only", "This command can only be used in a guild.")]
        [Discord.Commands.RequireContext(ContextType.Guild,
            ErrorMessage = "Sorry, this command must be ran from within a server, not a DM!")]
        public async Task GuildOnlyCommand()
            => await RespondAsync("Nothing to see here!");

        // The command's Run Mode MUST be set to RunMode.Async, otherwise, being connected to a voice channel will block the gateway thread.
        [SlashCommand("join", "Join your connected voice channel.", runMode: Discord.Interactions.RunMode.Async)]
        public async Task Join(IVoiceChannel channel = null)
        {
            // Get the audio channel
            channel = channel ?? (Context.User as IGuildUser)?.VoiceChannel;
            if (channel == null)
            {
                await RespondAsync(
                    "You must be in a voice channel, or a voice channel must be passed as an argument.");
                return;
            }

            // For the next step with transmitting audio, you would want to pass this Audio Client in to a service.
            await channel.ConnectAsync(selfDeaf: true);
            await RespondAsync($"Connected to `{channel}`!");
        }

        private async Task JoinChannel(IVoiceChannel channel = null)
        {
            channel = channel ?? (Context.User as IGuildUser)?.VoiceChannel;
            await channel.ConnectAsync(selfDeaf: true);
        }
        
        [SlashCommand("leave", "Leave the voice channel.", runMode: Discord.Interactions.RunMode.Async)]
        public async Task LeaveChannel()
        {
            var audioClient = Context.Guild.AudioClient;
            if (audioClient is not {ConnectionState: ConnectionState.Connected})
            {
                await RespondAsync("I'm not connected to a voice channel!");
                return;
            }

            _cancellationTokenSource?.Cancel();
            await audioClient.StopAsync();
            await RespondAsync("Disconnected.");
        }
        
        [SlashCommand("stop", "Stop the current song.", runMode: Discord.Interactions.RunMode.Async)]
        public async Task StopSong()
        {
            var audioClient = Context.Guild.AudioClient;
            if (audioClient is not {ConnectionState: ConnectionState.Connected})
            {
                await RespondAsync("I'm not connected to a voice channel!");
                return;
            }

            queueService.DequeueFirst();
            await RespondAsync("Stopped.");
            audioService.StopTransmitting();
        }

        [SlashCommand("play", "Play a song from YouTube.", runMode: Discord.Interactions.RunMode.Async)]
        public async Task PlaySong([Remainder] string query)
        {
            var adminId = Environment.GetEnvironmentVariable("ADMIN_ID") ?? "";
            // TODO: implement queue
            if (Context.User is not IGuildUser guildUser || guildUser.VoiceChannel is null && guildUser.Id != Convert.ToUInt64(adminId))
            {
                await RespondAsync("You must be in a voice channel to use this command.", ephemeral: true);
                return;
            }
            
            await DeferAsync();
            
            // Todo: split

            var youtubeRegex = new Regex("^(http(|s)://.*|[\\w\\-]{11})$", RegexOptions.IgnoreCase);
            var isYoutubeUrl = youtubeRegex.Match(query).Success;
            string videoId;
            var youtube = new YoutubeClient();

            if (isYoutubeUrl)
            {
                videoId = (VideoId) query;
            }
            else
            {
                var videos = await youtube.Search.GetVideosAsync(query);
                videoId = videos[0].Id;
            }
            
            var videoMetadata = await youtube.Videos.GetAsync(videoId);
            var streamManifest = await youtube.Videos.Streams.GetManifestAsync(videoId);
            var streamInfo = streamManifest.GetAudioOnlyStreams().GetWithHighestBitrate();
            
            var audioClient = Context.Guild.AudioClient;
            if (audioClient is not {ConnectionState: ConnectionState.Connected})
            {
                await JoinChannel();
            }

            audioClient = Context.Guild.AudioClient;
            audioService.audioClient = audioClient;
            
            var embed = new EmbedBuilder()
                .WithTitle($"{videoMetadata.Author} - {videoMetadata.Title} - {videoMetadata.Id}")
                .WithImageUrl(videoMetadata.Thumbnails.GetWithHighestResolution().Url);
            await ModifyOriginalResponseAsync(msg => msg.Embed = embed.Build());
            
            queueService.Enqueue(new QueueItem
            {
                Id = videoMetadata.Id,
                videoMetadata = videoMetadata,
                StreamInfo = streamInfo
            });

            await queueService.Next();
        }
    }
}