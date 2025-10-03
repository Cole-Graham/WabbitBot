new requirements:
- only default images on repository that come with new deployments are default/placeholder images (e.g. `map_thumbnail.jpg`,)
- new directory for these images is `data/images/default`
- images are customized on deployment by discord admin through /config discord commands
- config commands required:
    - `/config` command group
    - `/config help` (list config commands)
    - `/config images` (ask for reply from admin with attached images to configure, listing all valid filenames)
- map thumbnails would still be handled by `/maps` command and subcommands in command group
- admin replies with all the images attached that they want configured, with filenames like `challenge_banner.jpg`, `initial_match_header.jpg`, etc.
- doesn't have to be .jpg, accept any valid image file type
- configuration is handled automatically by reading the filename, ensure cdn is saved
- next time the `/config images` command is run, the filenames list have formatting to distinguish which are still using defaults

# Challenge Container components:
1. MediaGalleryBuilderComponent() for top banner
    - store default component images in data/images/default/discord
    - use default `challenge_banner.jpg` from server files as attachment (cache cdn for future use)
        - override with configured challenge_banner image if it has been configured
2. TextDisplayBuilderComponent() for body with Discord markdown formatting
    - Get the Challenger and Opponent's `User.DiscordIds` and Discord mention string.
    - Get the Challenger and Opponent's `Team.Name`
    - `## {challenger_mention} {challenger_team_name} ({ScrimmageTeamStats.CurrentRating})\n\n`
      `{opponent_mention} {opponent_team_name} has been challenged to a Scrimmage Match.`
3. DiscordActionRowComponent() with Accept and Decline buttons.

# Match thread initial container for overview of match and Map Ban proceedings.
1. MediaGalleryBuilderComponent() for top banner
    - store default component images in data/images/default/discord
    - use default `match_banner.jpg` from deployment server files as attachment (cache cdn for future use)
        - override with configured match_banner image if it has been configured
2. TextDisplayBuilderComponent() for body with Discord markdown formatting
    - Get the Challenger and Opponent's `User.DiscordIds` and Discord mention string.
    - Get the Challenger and Opponent's `Team.Name`
    - `## {challenger_mention} {challenger_team_name} ({ScrimmageTeamStats.CurrentRating}) vs. {opponent_mention} {opponent_team_name} ({ScrimmageTeamStats.CurrentRating})\n\n`
      `{opponent_mention} {opponent_team_name} has been challenged to a Scrimmage Match.`
3. DiscordActionRowComponent() with Accept and Decline buttons.

# Map ban container:
1. MediaGalleryBuilderComponent() for top banner
    - store default component images in data/images/default/discord
    - use default `mapban_banner.jpg` from deployment server files as attachment (cache cdn for future use)
        - override with configured mapban_banner image if it has been configured
2. DiscordMediaGalleryBuilderComponent() for all map thumbnails from map pool for `Match.TeamSize`
3. TextDisplayBuilderComponent() for body with Discord markdown formatting
    - Get the Challenger and Opponent's `Team.Name`
    - Get map banning instructions from utility or helper that determines instructions based on the
      size of the map pool and the match length (bo1, bo3, bo5, bo7).
    - Instructions should be configurable by one or more commands in `/maps` discord command group
        - due to complexity, configuration should be done by requesting reply from admin with 
          json configuration code.
        - something like:
          ```json
            "bo1": {
                "pool_size_7": {
                    "guaranteed_bans": 2,
                    "coinflip_bans": 0
                },
                "pool_size_8": {
                    "guaranteed_bans": 2,
                    "coinflip_bans": 0,
                },
                "pool_size_9": {
                    "guaranteed_bans": 2,
                    "coinflip_bans": 1
                },
                "pool_size_10": {
                    "guaranteed_bans": 3,
                    "coinflip_bans": 0,
                },
                etc.
            }
            "bo3": {
                "pool_size_7": {
                    "guaranteed_bans": 2,
                    "coinflip_bans": 0
                },
                "pool_size_8": {
                    "guaranteed_bans": 2,
                    "coinflip_bans": 1,
                }
                etc.
            }
            "bo5": {
                etc.
            }
            "bo7": {
                etc.
            }
          ```
        - json submission should be validated against current map pool sizes
        - /maps commands should also request configuration when expanding pool beyond current json scope
          - e.g. If current json configuration only goes up to pool_size_7 and admin adds 8th and 9th map to the pool,
            then it should request configuration for pool_size_8 and pool_size_9.
        - `Please select your map bans in order of priority. You will have a chance to preview\n`
          `your selections before confirming. You have {guaranteed_n} guaranteeed bans, and \n`
          `{coinflip_n} coinflip bans. Coinflip bans come into play depending on the number of\n`
          `games played.`
    - `## Map bans - {challenger_team_name} vs. {opponent_team_name}\n\n`
    - `{map_ban_instructions}`
4. DiscordActionRowCompont() with DiscordSelectComponet() with map pool for `Match.TeamSize`

# Game embeds (for each Game in the match thread)
1. MediaGalleryBuilderComponent() for top banner
    - store default component images in data/images/default/discord
    - use default `game_banner.jpg` from deployment server files as attachment (cache cdn for future use)
        - override with configured game_banner image if it has been configured
2. DiscordSeparatorComponent()
3. DiscordMediaGalleryBuilderComponent() for the map thumbnail (randomly chosen from the remaining pool of maps)
4. DiscordSeparatorComponent()
5. TextDisplayBuilderComponent() for body with Discord markdown formatting
    - Get the Challenger and Opponent's `User.DiscordIds` and Discord mention string.
    - `Reply to this message with one replay from your team after the game is complete.\n`
      `Contact an admin if one or more team's replay files are unavailable.`
