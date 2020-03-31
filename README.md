# discord_dj

## Requirements
Node 12
An unprivelidged user: discord_dj

## Install

Check out or symlink this project to /opt/discord_dj

symlink discord_dj.service /libs/systemd/system/discord_dj.service

run: 
sudo systemctl daemon-reload
sudo systemctl start discord_dj

If you want to have the service run automatically at boot run:
sudo systemctl enable discord_dj


