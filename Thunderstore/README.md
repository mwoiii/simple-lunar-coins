# SimpleLunarCoins

## About

SimpleLunarCoins is a mod that aims to provide subtle customisation of how lunar coins are handled without otherwise reworking any major gameplay elements. 

By default, the mod will turn lunar coins into a per-run currency. Coins are automatically picked up, and coin pickups are replaced with a new effect of the enemy dropping said coin in the same fashion as gold. Coin drop chance is also set to a fixed value, and all players receive a coin when one is dropped. However, all of the aforementioned settings are customisable in the **BepInEx config file**, and a full list of the settings can be read below. You could even change the settings back to default RoR2, if you really wanted. I don't know why you'd want to do that.

If you're looking for a mod that provides a full rework of the lunar coin system, including most of the changes this mod provides, check out [Ephemeral Coins](https://thunderstore.io/package/VarnaScelestus/Ephemeral_Coins/) instead.

## Customisation
At this point in time, the following settings are configurable:

* ***Initial Coin Chance***: The starting % chance for a coin to be dropped.

* ***Coin Chance Multiplier***: The value that the coin chance is multiplied by after a coin is dropped.

* ***Starting Coins***: The number of coins that each player holds at the start of a run, if the 'Reset Coins Each Run' setting is enabled.

* ***Distribute Coins***: Whenever a lunar coin is picked up, a lunar coin is given to every player.

* ***No Coin Droplets***: Enemies no longer drop physical coins that need to be manually picked up, and instead are 
automatically picked up alongside a emitting a custom effect.

* ***Reset Coins Each Run***: Each player starts a run with the amount of coins specified in 'Starting Coins'.
    * Bear in mind that using 'Reset Coins Each Run' will (currently) permanently overwrite the lunar coins of each player in the run.