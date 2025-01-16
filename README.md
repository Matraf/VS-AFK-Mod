# AFK Detector Mod for Vintage Story

This mod detects AFK players on your Vintage Story server by monitoring their movement and allows server admins to configure the AFK timeout duration.

## Features

- Automatically detects AFK players based on their last position.
- Sends notifications to players who have reached halfway through their kick time.
- Admins can set the AFK timeout dynamically with a command.

## Commands

### `/afk`
Manage the AFK mod settings.

#### Usage
- `/afk setTimeout <minutes>`  
  Set the AFK timeout duration in minutes.

#### Examples
- `/afk setTimeout 5`  
  Sets the AFK timeout to 5 minutes.