# CS2 RockTheVote (RTV)
General purpose map voting plugin.

# Features
- Reads from a custom maplist
- RTV Command (Using !rtv in chat, or with Panorama system that is built into CS2 [F1 = Yes, F2 = No])
- End of Map Vote. Supports map cooldown, map extension option.
- Nominate command (nominate a map to appear in the map vote). Partial name matching, and conflicting map name resolution (surf_beginner, surf_beginner2). Limit 1 per player.

  ![nominate](https://github.com/user-attachments/assets/6ac056bc-9842-4422-ac0d-c7cd814b3ba6)
  
- Supports workshop maps, and custom map names. E.g. "surf_beginner (T1, Staged)"
- Nextmap command. Prints the next map to chat.
- Translated (Google Translate, ymmv)
- Extend command. !extend 10 extends map by 10 minutes (flag restricted)
- Vote Extend command. !ve/!votextend starts a vote to extend the current map (flag restricted)
- Optional sound alert when map vote or !rtv starts (configurable sound)
- Optional chat/hud vote countdown

 ![hudcountdown](https://github.com/user-attachments/assets/e1034f3c-340a-4d88-8d8a-96526f333fad)
 ![chatcountdown](https://github.com/user-attachments/assets/803826a1-665b-4ab7-9e38-fbb0e8d702be)

- Panorama Vote (F1 = Yes, F2 = No) for !rtv & !voteextend

![panoramavote](https://github.com/user-attachments/assets/31ebe223-225f-4cef-812e-3bf6c56e590d)
![voteextend](https://github.com/user-attachments/assets/5cfd9a5f-36a5-4a11-ae26-3e74d5387251)
- ScreenMenu/HudMenu/ChatMenu for EndOfMapVote/!nominate/!votemap

![screenmenu](https://github.com/user-attachments/assets/374a7899-f887-4425-a01e-decae1a203b0)
![chatmenu](https://github.com/user-attachments/assets/8d7e9ee8-b26e-47b1-89d8-ced96b13a392)
![hudmenu](https://github.com/user-attachments/assets/0fd37e45-bf7f-4f97-9b7b-7fab92352392)

- Maplist Validator. Send to error log or Discord when a map is no longer available on the workshop.

  ![DiscordWebhook](https://github.com/user-attachments/assets/eaf8d706-abd1-4258-a7a3-b9cb44500802)
  ![Log](https://github.com/user-attachments/assets/2f65dd9d-1ee9-4217-a753-81358973df2e)




## Requirements
v315+ of [CounterStrikeSharp](https://github.com/roflmuffin/CounterStrikeSharp/releases)

v1.0.35 of [CS2MenuManager](https://github.com/schwarper/CS2MenuManager/releases/tag/v1.0.35)

# Installation
- Download the latest release from https://github.com/M-archand/cs2-rockthevote/releases
- Extract the .zip file into `addons/counterstrikesharp/plugins`
- Update the maplist.example.txt to inlcude your desired maps and then rename it to maplist.txt


# Roadmap
- [ ] Add !maps command that lists all available maps
- [ ] Add check for invalid workshop maps (with optional discord webhook/auto removal)
- [ ] Add ability to set a random starting map on server first start
- [ ] !extend max extension value
- [ ] Add vote percentage required for winning map (e.g. must receive 25% of the vote)
- [ ] Add vote runnoff (e.g. 2nd stage of voting between 2 maps if minimum vote percentage not achieved for a map)

# Translations
| Language             |
| -------------------- |
| English              |
| French               |
| Spanish              |
| Ukrainian            |
| Turkish              |
| Latvian              |
| Hungarian            |
| Polish               |
| Russian              |
| Portuguese (BR)      |
| Chinese (Simplified) |

# Configs
- A config file will be created in `addons/counterstrikesharp/configs/plugins/RockTheVote` the first time you load the plugin.
- Changes in the config file will require you to reload the plugin or restart the server (changing the map won't work).
- Maps that will be used in RTV/nominate/votemap/end of map vote are located in addons/counterstrikesharp/plugins/RockTheVote/maplist.txt

## Rtv
Players can type !rtv to request the map to be changed, once a number of votes is reached (VotePercentage in cfg) a vote will start for the next map, the vote duration is defined in the config.

| Config              | Description                                                                                                            | Default Value | Min   | Max                                  |
| ------------------- | ---------------------------------------------------------------------------------------------------------------------- | ------------- | ----- | ------------------------------------ |
| Enabled             | Enable/Disable RTV functionality                                                                                       | true          | false | true                                 |
| EnabledInWarmup     | Enable/Disable RTV during warmup                                                                                       | false         | false | true                                 |
| NominationEnabled   | Enable/Disable nomination                                                                                              | true          | false | true                                 |
| MinPlayers          | Minimum amount of players to enable RTV/Nominate                                                                       | 0             | 0     | 9999                                 |
| MinRounds           | Minimum rounds to enable RTV/Nominate                                                                                  | 0             | 0     | 9999                                 |
| ChangeMapImmediatly | Whether to change the map immediatly when vote ends or not                                                             | true          | false | true                                 |
| SoundEnabled        | Whether to play a sound when the vote starts                                                                           | false         | false | true                                 |
| SoundPath           | The file path of the sound that will be played. sounds/vo/announcer/cs2_classic/felix_broken_fang_pick_1_map_tk01.vsnd | "Pick 1 Map"  |       | any sound file path                  |
| MapsToShow          | Amount of maps to show in vote                                                                                         | 6             | 1     | 6 with HudMenu, unlimited without it |
| VoteDuration        | Seconds the resulting map vote will last if the rtv vote passes                                                        | 30            | 1     |                                      |
| RtvVoteDuration     | Seconds the RTV vote will last                                                                                         | 45            | 1     | 9999                                 |
| VotePercentage      | Percentage of players that should vote yes to RTV in order to start a map vote                                         | 51            | 1     | 100                                  |
| CooldownDuration    | Amount of time that must pass before another vote can be called (seconds)                                              | 180           | 0     | 9999                                 |
| MapStartDelay       | How long after the map has started before and RTV can be called                                                        | 180           | 0     | 9999                                 |
| VotePercentage      | Percentage of players that need to vote Yes for the extension to pass                                                  | 50            | 1     | 100                                  |
| HudCountdown        | False = Chat countdown, True = Hud Countdown                                                                           | false         | false | true                                 |
| Permission          | The permission required to use this command                                                                            | @css/vip      |       |                                      |

## Votemap
Players can vote to change to a specific map by using the votemap <mapname> command

| Config              | Description                                                              | Default Value | Min   | Max  |
| ------------------- | ------------------------------------------------------------------------ | ------------- | ----- | ---- |
| Enabled             | Enable/disable votemap funtionality                                      | true          | false | true |
| VotePercentage      | Percentage of players that should vote in a map in order to change to it | 60            | 1     | 100  |
| ChangeMapImmediatly | Whether to change the map immediatly when vote ends or not               | true          | false | true |
| EnabledInWarmup     | Enable/Disable votemap during warmup                                     | true          | false | true |
| MinRounds           | Minimum rounds to enable votemap                                         | 0             |       |      |
| MinPlayers          | Minimum amount of players to enable votemap                              | 0             |       |      |
| Permission          | The permission required to use this command                              | @css/vip      |       |      |

## VoteExtend
Players can start a vote to extend the map by an amount set in the config. Uses either Chat or Panorama Vote

| Config              | Description                                                              | Default Value | Min   | Max  |
| ------------------- | ------------------------------------------------------------------------ | ------------- | ----- | ---- |
| Enabled             | Enable/disable votemap funtionality                                      | true          | false | true |
| VoteDuration        | How long the vote will last                                              | 60            | 1     | 100  |
| VotePercentage      | Percentage of players that need to vote Yes for the extension to pass    | 50            | 1     | 100  |
| CooldownDuration    | Amount of time that must pass before another vote can be called (seconds)| 180           | 0     | 999  |
| EnableCountdown     | Whether a countdown timer is shown (hud/chat)                            | true          | false | true |
| HudCountdown        | False = Chat countdown, True = Hud Countdown                             | false         | false | true |
| Permission          | The permission required to use this command                              | @css/vip      |       |      |

## End of Map Vote
When the map is about to end, or when trigger by an !rtv, a random list of maps from your maplist.txt file be shown to choose from to decide the next map to be played

| Config                  | Description                                                                                                            | Default Value | Min   | Max                                  |
| ----------------------- | ---------------------------------------------------------------------------------------------------------------------- | ------------- | ----- | ------------------------------------ |
| Enabled                 | Enable/Disable end of map vote functionality                                                                           | true          | false | true                                 |
| ChangeMapImmediatly     | Whether to change the map immediatly when vote ends or not                                                             | true          | false | true                                 |
| HideHudAfterVote        | Whether to hide vote status hud after vote or not, only matters when HudMenu is true                                   | false         | false | true                                 |
| MapsToShow              | Amount of maps to show in vote,                                                                                        | 6             | 1     | 6 with HudMenu, unlimited without it |
| VoteDuration            | Seconds the RTV should can last                                                                                        | 30            | 1     |                                      |
| HudMenu                 | Whether to use HudMenu or just the chat one, when false the hud only shows which map is winning instead of actual menu | true          | false | true                                 |
| TriggerSecondsBeforeEnd | Amount of seconds before end of the map that should trigger the vote, only used when mp_timelimit is greater than 0    | 120           | 1     |                                      |
| TriggerRoundsBeforeEnd   | Amount of rounds before end of map that should trigger the vote, only used when mp_maxrounds is set                    | 2             | 1     |                                      |
| DelayToChangeInTheEnd   | Delay in seconds that plugin will take to change the map after the win panel is shown to the players                   | 6             | 3     |                                      |

## ScreenMenu
Used if `EnableScreenMenu` = `true`. Extra settings to override your CS2MenuManager Shared API settings if desired
| Config                  | Description                                                                                 | Default Value | Min      | Max           |
| ----------------------- | --------------------------------------------------------------------------------------------| ------------- | -------- | ------------- |
| EnabledResolutionOption | Whether to show the resolution option or not in your map vote ScreenMenu                    | false         | false    | true          |
| EnabledExitOption       | Whether to show the exit option or not in your map vote/nominate ScreenMenu                 | false         | false    | true          |

## VoteType
| Config                | Description                                                                                 | Default Value | Min      | Max           |
| --------------------- | --------------------------------------------------------------------------------------------| ------------- | -------- | ------------- |
| EnableScreenMenu      | Use Screen Menu for EndMapVote/Nominate/Votemap                                             | false         | false    | true          |
| EnableChatMenu        | Use Chat Menu for EndMapVote/Nominate/Votemap                                               | true          | false    | true          |
| EnableHudMenu         | Use HUD Menu for EndMapVote/Nominate/Votemap                                                | false         | false    | true          |
| EnablePanorama        | Use Panorama vote for !rtv/!voteextend   false = uses chat (like original)                  | true          | false    | true          |

## General
| Config                | Description                                                                                 | Default Value | Min      | Max           |
| --------------------- | --------------------------------------------------------------------------------------------| ------------- | -------- | ------------- |
| ShowNextMapToAll      | Show the next map to everyone in chat when the !nextmap command is used                     | false         | false    | true          |
| MaxMapExtensions      | How many extensions are allowed per map. Includes end of map votes, and !voteextend         | 2             | 1        | 0 (unlimited) |
| RoundTimeExtension    | The number of minutes the map will be extended by                                           | 15            | 1        | 999           |
| MapsInCoolDown        | Number of maps that can't be used in vote because they have been played recently            | 3             | 0        | 999           |
| ChatCountdownInterval | How often the time left in the vote is printed to chat (in seconds)                         | 20            | 1        | 999           |
| HideHudAfterVote      | Whether the HUD should be hidden after voting if the "HudMenu" is enabled as a voting type  | true          | false    | true          |


  
# Adding workshop maps
```
surf_beginner:3070321829
surf_nyx (T1, Linear):3129698096
de_dust2
```
