using Discord;
using Discord.Commands;
using System.IO;
using System.Threading.Tasks;
using Discord.Interactions;
using JamBotDotNet.Services;
using ContextType = Discord.Commands.ContextType;

namespace JamBotDotNet.Modules
{
    // Modules must be public and inherit from an IModuleBase
    public class PublicModule : InteractionModuleBase<SocketInteractionContext>
    {
        // Dependency Injection will fill this value in for us
        public PictureService PictureService { get; set; }

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
        [Discord.Commands.RequireContext(ContextType.Guild, ErrorMessage = "Sorry, this command must be ran from within a server, not a DM!")]
        public async Task GuildOnlyCommand()
            => await RespondAsync("Nothing to see here!");
    }
}
