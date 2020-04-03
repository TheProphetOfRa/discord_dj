using Discord;
using Discord.Commands;
using Discord_DJ.Services;
using System.Threading.Tasks;

namespace Discord_DJ.Commands
{
    public class MusicModule : ModuleBase<SocketCommandContext>
    {
        public MusicService MusicService { get; set; }

        [Command("play", RunMode = RunMode.Async)]
        [Summary("Play some music from youtube")]
        public async Task PlayAsync([Remainder] [Summary("The link or search term")] string music = "")
        {
            var channel = (Context.User as IGuildUser)?.VoiceChannel;
            if (channel == null)
            {
                await Context.Channel.SendMessageAsync("Please join a voice channel and try again.");
                return;
            }

            if (music == "")
            {
                await ReplyAsync("What would you like to play? You can send me a direct link or search like you would in the youtube search bar!");
                return;
            }

            MusicServiceResult result = await MusicService.TryAddToGuidQueue(Context.Guild.Id, channel, music);

            if (result == MusicServiceResult.FailedToGetResults)
            {
                await ReplyAsync("Your search may have returned no results. Please try again.");
            }
            else if (result == MusicServiceResult.AlreadyPlayingInAnotherChannel)
            {
                await ReplyAsync("I'm already playing in another channel. Come join in!");
            }
        }
    }
}
