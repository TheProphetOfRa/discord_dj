﻿using Discord;
using Discord.Commands;
using Discord_DJ.Model;
using Discord_DJ.Services;
using System.Collections.Generic;
using System.Threading.Tasks;
using static Discord_DJ.Model.MusicPlayer;

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

        [Command("stop")]
        public async Task Stop()
        {
            var channel = (Context.User as IGuildUser)?.VoiceChannel;
            if (channel == null)
            {
                await ReplyAsync("You need to be in a voice channel to stop music.");
                return;
            }

            MusicServiceResult result = await MusicService.Stop(Context.Guild.Id, channel);
            if (result == MusicServiceResult.AlreadyPlayingInAnotherChannel)
            {
                await ReplyAsync("I'm not playing music in your channel");
            }
        }

        [Command("skip")]
        public async Task Skip()
        {
            var channel = (Context.User as IGuildUser)?.VoiceChannel;
            if (channel == null)
            {
                await ReplyAsync("You need to be in a voice channel to skip music.");
                return;
            }

            MusicServiceResult result = await MusicService.Skip(Context.Guild.Id, channel);
            if (result == MusicServiceResult.AlreadyPlayingInAnotherChannel)
            {
                await ReplyAsync("I'm not playing music in your channel");
            }
        }

        [Command("skipto")]
        public async Task SkipTo(int songIndex)
        {
            var channel = (Context.User as IGuildUser)?.VoiceChannel;
            if (channel == null)
            {
                await ReplyAsync("You need to be in a voice channel to skip music.");
                return;
            }

            if (songIndex < 1 || songIndex >= MusicService.Queue(Context.Guild.Id).Count)
            {
                await ReplyAsync("Please select a number to skip to. You can see what's comming with the queue command.");
                return;
            }

            MusicServiceResult result = await MusicService.SkipTo(Context.Guild.Id, channel, songIndex);
            if (result == MusicServiceResult.AlreadyPlayingInAnotherChannel)
            {
                await ReplyAsync("I'm not playing music in your channel");
            }
        }

        [Command("queue")]
        public async Task Queue()
        {
            var channel = (Context.User as IGuildUser)?.VoiceChannel;
            if (channel == null)
            {
                await Context.Channel.SendMessageAsync("You need to be in a voice channel to view the queue.");
                return;
            }

            List<VideoInfo> queue = MusicService.Queue(Context.Guild.Id);

            if (queue.Count == 0)
            {
                await ReplyAsync("There are no more songs in the queue. Let's add some!");
                return;
            }

            EmbedBuilder builder = new EmbedBuilder();
            builder.WithTitle("Queue:");
            int count = 1;
            foreach (VideoInfo entry in queue)
            {
                builder.AddField(count.ToString(), entry.title, false);
                count++;
            }

            await ReplyAsync("", false, builder.Build());
        }
    }
}
