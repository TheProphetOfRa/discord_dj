using Discord;
using Discord.Commands;
using Discord_DJ.Services;
using System.Threading.Tasks;

namespace Discord_DJ.Commands
{
    public class TriviaModule : ModuleBase<SocketCommandContext>
    {
        public TriviaService TriviaService { get; set; }

        [Command("trivia", RunMode = RunMode.Async)]
        public async Task Trivia(int numQuestions = 5)
        {
            var channel = (Context.User as IGuildUser)?.VoiceChannel;
            if (channel == null)
            {
                await Context.Channel.SendMessageAsync("Please join a voice channel and try again.");
                return;
            }
            
            TriviaServiceResult result = await TriviaService.StartQuizForGuild(Context.Guild.Id, channel, Context.Message.Channel, numQuestions);

            if (result == TriviaServiceResult.AlreadyRunningQuiz)
            {
                await ReplyAsync("I'm already hosting a quiz, wait until this one's finished then come join the fun!");
            }
        }

        [Command("end-trivia")]
        public async Task EndTrivia()
        {
            var channel = (Context.User as IGuildUser)?.VoiceChannel;
            if (channel == null)
            {
                await Context.Channel.SendMessageAsync("Please join a voice channel and try again.");
                return;
            }

            TriviaService.EndQuiz(Context.Guild.Id);
        }
    }
}
