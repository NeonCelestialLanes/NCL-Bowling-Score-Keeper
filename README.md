# NCL Bowling Score

Dalamud plugin for **Neon Celestial Lanes** on Goblin. Keep a 10-frame bowling scorecard in party, send `/dice party 11` or `/random 11`, and parse the roll from chat.

Command in game: `/nclbowling`

## Install from the plugin installer

This is a custom plugin repo. You add it once under Dalamud **Experimental**, then install **NCL Bowling Score** from `/xlplugins` like any other plugin.

1. In game, type `/xlsettings` (or open **Dalamud Settings** from the plugin installer).
2. Open the **Experimental** tab.
3. Skip **DevPlugins**. Go to **Custom Plugin Repositories**.
4. Paste this URL into the empty box at the bottom:

```
https://raw.githubusercontent.com/NeonCelestialLanes/NCL-Bowling-Score-Keeper/main/pluginmaster.json
```

5. Click **+**, leave the new row checked, then click the **Save** icon (bottom right).
6. Type `/xlplugins`, find **NCL Bowling Score**, and install it.

After that, `/nclbowling` opens the scorecard.

## House rules (this plugin)

| Roll | Result |
| --- | --- |
| 10 on the first ball | Strike — that bowler's frame is done |
| Two balls that add to exactly 10 | Spare |
| Overshoot remaining pins (including 11) | Gutter (0) |

One click is one throw so you can RP between balls. Party members' `/dice party` rolls still score on your client even if you are not the one clicking. Manual guests use `/random 11` because they may not be in the party.

## Requirements

- Final Fantasy XIV with [XIVLauncher](https://goatcorp.github.io/) / Dalamud
- Dalamud API 15
