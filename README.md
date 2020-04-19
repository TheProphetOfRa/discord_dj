# discord_dj

A Music Trivia bot written in C# for Discord.

To run:

Replace trivia.json with your desired trivia items and then add the music files in 48000Hz OPUS files.

Then build using VS2019.

Currently this does not work on Linux as there is some problem copying the output from ffmpeg to the discord library.

You will also need to make sure that the opus and libsodium dlls are copied to your build directory and all the files from your Resources folder.

If you do not have ffmpeg installed system wide it will need to be included in the build directory also.

As a final point you will need to follow discords documentation to setup a bot and then configure a config.json file in the build directory with your API key listed under "botAPIKey"
