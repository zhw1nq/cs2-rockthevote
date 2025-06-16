# CS2 RockTheVote (RTV)
General purpose map voting plugin.

# Features
- Reads from a custom maplist
- RTV Command
- Nominate command
- Supports workshop maps
- Nextmap command
- Translated by the community

Features added since fork:
- Better !nominate (partial name matching). Credits: [exd02](https://github.com/abnerfs/cs2-rockthevote/pull/31)
- Clarify nomination if 2 maps with similar names (surf_beginner, surf_beginner2)
- Limit players to 1 nomination per map
- "VotePercentage" logic now works properly with 2 players on the server
- Ability to add map info after name w/ capitalization
- Add optional Extend Map feature for End of Map Vote
- Add optional sound when map vote starts (configurable sound)
- Add vote extend (!ve/!voteextend)
- Add !extend {0}
- Add Panorama Vote (F1 = Yes, F2 = No) for !rtv & !voteextend
- Add ScreenMenu integration for end of map vote/nominate

![nextmap1](https://github.com/user-attachments/assets/87d34a7c-3333-4272-aba1-2dae6f9d5d3a)
![nextmap2](https://github.com/user-attachments/assets/4f536075-2b9d-4be1-9572-7c728d79ef4c)
![screenmenu](https://github.com/user-attachments/assets/10bfa73e-2ea3-4c49-b874-f87b85211136)

![panoramavote](https://github.com/user-attachments/assets/31ebe223-225f-4cef-812e-3bf6c56e590d)
![voteextend](https://github.com/user-attachments/assets/5cfd9a5f-36a5-4a11-ae26-3e74d5387251)

![nominate](https://github.com/user-attachments/assets/6ac056bc-9842-4422-ac0d-c7cd814b3ba6)



## Requirements
v315+ of [CounterStrikeSharp](https://github.com/roflmuffin/CounterStrikeSharp/releases)

v1.0.32 of [CS2MenuManager](https://github.com/schwarper/CS2MenuManager/releases/tag/v1.0.32)

# Installation
- Download the latest release from https://github.com/M-archand/cs2-rockthevote/releases
- Extract the .zip file into `addons/counterstrikesharp/plugins`
- Update the maplist.example.txt to inlcude your desired maps and then rename it to maplist.txt


# Roadmap
- [x] Add time extension feature
- [x] Fix !nominate name matching for conflicting names
- [X] Fix HudMenu voting
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
Players can type !rtv to request the map to be changed, once a number of votes is reached (set in cfg) a vote will start for the next map, the vote duration is defined in the config.

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
Players can vote to change to an specific map by using the votemap <mapname> command

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
Players can vote to change to an specific map by using the votemap <mapname> command

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
Based on mp_timelimit and mp_maxrounds cvar before the map ends a RTV like vote will start to define the next map, it can be configured to change immediatly or only when the map actually ends

| Config                  | Description                                                                                                            | Default Value | Min   | Max                                  |
| ----------------------- | ---------------------------------------------------------------------------------------------------------------------- | ------------- | ----- | ------------------------------------ |
| Enabled                 | Enable/Disable end of map vote functionality                                                                           | true          | false | true                                 |
| ChangeMapImmediatly     | Whether to change the map immediatly when vote ends or not                                                             | true          | false | true                                 |
| HideHudAfterVote        | Whether to hide vote status hud after vote or not, only matters when HudMenu is true                                   | false         | false | true                                 |
| MapsToShow              | Amount of maps to show in vote,                                                                                        | 6             | 1     | 6 with HudMenu, unlimited without it |
| VoteDuration            | Seconds the RTV should can last                                                                                        | 30            | 1     |                                      |
| HudMenu                 | Whether to use HudMenu or just the chat one, when false the hud only shows which map is winning instead of actual menu | true          | false | true                                 |
| TriggerSecondsBeforeEnd | Amount of seconds before end of the map that should trigger the vote, only used when mp_timelimit is greater than 0    | 120           | 1     |                                      |
| TriggerRoundsBeforEnd   | Amount of rounds before end of map that should trigger the vote, only used when mp_maxrounds is set                    | 2             | 1     |                                      |
| DelayToChangeInTheEnd   | Delay in seconds that plugin will take to change the map after the win panel is shown to the players                   | 6             | 3     |                                      |

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
- If you are not hosting a workshop map collection you need to know the maps ID and put in the maplist.txt file in the following format: `<mapname>:<workshop-id>`.
- If you are already hosting a collection and can change to workshop maps using the command `ds_workshop_changelevel <map-name>` you don't need the ID, just put the actual map name and it will work.

```
surf_beginner:3070321829
surf_nyx (T1, Linear):3129698096
de_dust2
```
