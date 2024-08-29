# CS2 RockTheVote (RTV)
General purpose map voting plugin.

![image](https://github.com/abnerfs/cs2-rockthevote/assets/14078661/a603d1b6-ba35-4d5a-b887-1b14058a8050)
  
## Requirements
Latest release of [CounterStrikeSharp](https://github.com/roflmuffin/CounterStrikeSharp/releases)

# Instalation
- Download the latest release from https://github.com/M-archand/cs2-rockthevote/releases
- Extract the .zip file into `addons/counterstrikesharp/plugins`
- Update the maplist.example.txt to inlcude your desired maps and then rename it to maplist.txt

# Features
- Reads from a custom maplist
- RTV Command
- Timeleft command
- Nominate command
- Votemap command
- Supports workshop maps
- Nextmap command
- Fully configurable
- Translated by the community

# Roadmap
- [ ] Add time extension feature

# Translations
| Language             | Contributor          |
| -------------------- | -------------------- |
| Brazilian Portuguese | abnerfs              |
| English              | abnerfs              |
| Ukrainian            | panikajo             |
| Turkish              | brkvlr               |
| Russian              | Auttend              |
| Latvian              | rcon420              |
| Hungarian            | Chickender, lovasatt |
| Polish               | D3X                  |
| French               | o3LL                 |
| Chinese (zh-Hans)    | himenekocn           |

# Configs
- A config file will be created in `addons/counterstrikesharp/configs/plugins/RockTheVote` the first time you load the plugin.
- Changes in the config file will require you to reload the plugin or restart the server (changing the map won't work).
- Maps that will be used in RTV/nominate/votemap/end of map vote are located in addons/counterstrikesharp/configs/plugins/RockTheVote/maplist.txt

## General config
| Config         | Description                                                                      | Default Value | Min | Max |
| -------------- | -------------------------------------------------------------------------------- | ------------- | --- | --- |
| MapsInCoolDown | Number of maps that can't be used in vote because they have been played recently | 3             | 0   |     |

## RockTheVote
Players can type rtv to request the map to be changed, once a number of votes is reached (by default 60% of players in the server) a vote will start for the next map, this vote lasts up to 30 seconds (hardcoded for now), in the end server changes to the winner map.

| Config              | Description                                                                                                            | Default Value | Min   | Max                                  |
| ------------------- | ---------------------------------------------------------------------------------------------------------------------- | ------------- | ----- | ------------------------------------ |
| Enabled             | Enable/Disable RTV functionality                                                                                       | true          | false | true                                 |
| EnabledInWarmup     | Enable/Disable RTV during warmup                                                                                       | false         | false | true                                 |
| NominationEnabled   | Enable/Disable nomination                                                                                              | true          | false | true                                 |
| MinPlayers          | Minimum amount of players to enable RTV/Nominate                                                                       | 0             | 0     |                                      |
| MinRounds           | Minimum rounds to enable RTV/Nominate                                                                                  | 0             | 0     |                                      |
| ChangeMapImmediatly | Whether to change the map immediatly when vote ends or not                                                             | true          | false | true                                 |
| HudMenu             | Whether to use HudMenu or just the chat one, when false the hud only shows which map is winning instead of actual menu | true          | false | true                                 |
| HideHudAfterVote    | Whether to hide vote status hud after vote or not, only matters when HudMenu is true                                   | false         | false | true                                 |
| MapsToShow          | Amount of maps to show in vote,                                                                                        | 6             | 1     | 6 with HudMenu, unlimited without it |
| VoteDuration        | Seconds the RTV should can last                                                                                        | 30            | 1     |                                      |
| VotePercentage      | Percentage of players that should type RTV in order to start a vote                                                    | 60            | 0     | 100                                  |


## End of map vote
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

## Votemap
Players can vote to change to an specific map by using the votemap <mapname> command

| Config              | Description                                                              | Default Value | Min   | Max  |
| ------------------- | ------------------------------------------------------------------------ | ------------- | ----- | ---- |
| Enabled             | Enable/disable votemap funtionality                                      | true          | false | tru  |
| VotePercentage      | Percentage of players that should vote in a map in order to change to it | 60            | 1     | 100  |
| ChangeMapImmediatly | Whether to change the map immediatly when vote ends or not               | true          | false | true |
| EnabledInWarmup     | Enable/Disable votemap during warmup                                     | true          | false | true |
| MinRounds           | Minimum rounds to enable votemap                                         | 0             |       |      |
| MinPlayers          | Minimum amount of players to enable votemap                              | 0             |       |      |


## Timeleft
Players can type `timeleft` to see how much time is left in the current map 

| Config    | Description                                                                      | Default Value | Min   | Max  |
| --------- | -------------------------------------------------------------------------------- | ------------- | ----- | ---- |
| ShowToAll | Whether to show command response to everyone or just the player that executed it | false         | false | true |

## Nextmap
Players can type `nextmap` to see which map is going to be played next

| Config    | Description                                                                      | Default Value | Min   | Max  |
| --------- | -------------------------------------------------------------------------------- | ------------- | ----- | ---- |
| ShowToAll | Whether to show command response to everyone or just the player that executed it | false         | false | true |


  
# Adding workshop maps
- If you are not hosting a workshop map collection you need to know the maps ID and put in the maplist.txt file in the following format: `<mapname>:<workshop-id>`.
- If you are already hosting a collection and can change to workshop maps using the command `ds_workshop_changelevel <map-name>` you don't need the ID, just put the actual map name and it will work.

```
surf_beginner:3070321829
de_dust2
```