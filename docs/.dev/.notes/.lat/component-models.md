new requirements:
- only default images on repository that come with new deployments are default/placeholder images (e.g. `map_overview.jpg`,)
- new directory for these images is `data/images/default/discord`
- images are customized on deployment by discord admin through /config discord commands
- config commands required:
    - `/config` command group
    - `/config help` (list config commands)
    - `/config images` (ask for reply from admin with attached images to configure, listing all valid filenames)
- map thumbnails would be handled by `/maps` command and subcommands in command group
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

# Match thread initial container for overview of match and results of map ban proceedings.
1. MediaGalleryBuilderComponent() for top banner
    - store default component images in data/images/default/discord
    - use default `match_banner.jpg` from deployment server files as attachment (cache cdn for future use)
        - override with configured match_banner image if it has been configured
2. TextDisplayBuilderComponent() for match info with Discord markdown formatting
    - Get the Challenger and Opponent's `User.DiscordIds` and Discord mention string.
    - Get the Challenger and Opponent's `Team.Name`
    - `## {challenger_mention} {challenger_team_name} ({ScrimmageTeamStats.CurrentRating}) vs. {opponent_mention} {opponent_team_name} ({ScrimmageTeamStats.CurrentRating})\n`
      `**Best of {match_length}**`
3. MediaGalleryBuilderComponent() for map ban section banner
    - store default component images in data/images/default/discord
    - use default `map_ban_banner.jpg` from deployment server files as attachment (cache cdn for future use)
        - override with configured mapban_banner image if it has been configured
4. TextDisplayBuilderComponent() for Map Pool status and team ban status:
    - Get list of maps from map pool for given `TeamSize` of the match.
    - `**Map Pool:**\n`
      ` - {status_emoji} SomeMap\n`
      ` - {status_emoji} AnotherMap\n`
      ` - etc...\n`
      `**Ban status:**`
      `{challenger_team_name} {ban_status}`
      `{opponent_team_name} {ban_status}`
    - Ban status is either "In Progress", or "Confirmed".
    - Ban status needs to be updated for the copy of the embed in the other team's private thread,
      but it needs to be done manually by the players via a Refresh button. If it were to be done
      manually there is a risk of interuptting the other team's interactions with the map ban
      select menu. The refresh button also needs to be rate limited.
    - needs some kind of indicator for map status, such different emojis.
    - map status can be Banned, Played, Available
5. DiscordMediaGalleryBuilderComponent() for all map thumbnails from map pool for `Match.TeamSize`
6. TextDisplayBuilderComponent() for body with Discord markdown formatting
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
7. DiscordActionRowCompont() with DiscordSelectComponet() with map pool for `Match.TeamSize`

# Game embeds (for each Game in the match thread)
1. MediaGalleryBuilderComponent() for top banner
    - store default component images in data/images/default/discord
    - use default `game_1_banner.jpg` from deployment server files as attachment (cache cdn for future use)
        - 7 game_n_banner files for each game up to `game_7_banner.jpg, for best of 7 max match length.
        - override with configured game_banner images if they have been configured
2. DiscordSeparatorComponent()
3. DiscordMediaGalleryBuilderComponent() for the map thumbnail (randomly chosen from the remaining pool of maps)
4. DiscordSeparatorComponent()
5. TextDisplayBuilderComponent() for body with Discord markdown formatting
    - Get the Challenger and Opponent's `Team.Name`
    - `After match completion, reply to this message with all replay file's from your team.\n`
      `Contact an admin if no replay files are available.`

# Match complete embed
1. MediaGalleryBuilderComponent() for top banner
    - store default component images in data/images/default/discord
    - use default `match_complete_banner.jpg` from deployment server files as attachment (cache cdn for future use)
        - override with configured match_complete_banner image if they have been configured
2. DiscordSeparatorComponent()
5. TextDisplayBuilderComponent() for body with Discord markdown formatting
    - Get the Challenger and Opponent's `Team.Name`
    - `## Winner: {winner_team_name} {winner_elo_gain} {winner_new_rating}`
      `{loser_team_name} {loser_elo_loss} {loser_new_rating}`

# Leaderboard container
1. MediaGalleryBuilderComponent() for top banner
    - store default component images in data/images/default/discord
    - use default `leaderboard_1v1_banner.jpg` from deployment server files as attachment (cache cdn for future use)
        - 4 game_nvn_banner files for each `TeamSize` up to `leaderboard_4v4_banner.jpg`, for best of 7 max match length.
        - override with configured game_banner images if they have been configured
        - configuration for leaderboard banner images should be done within `/season` commands.
          - e.g. if `Season.Name` is "Season 1", the uploaded leaderboard_nvn_banner images should be
            permanently associated with that `Season.Name`, including after the Season is archived in the database.
          - We might need some way to ensure images associated with database entities has some way to be recovered
            even if Discord Cdn link dies/stops working.
2. DiscordSeparatorComponent()
3. DiscordSectionComponent()
    - Each section component will contain data for one `LeaderboardItem`, starting with the highest ranked team
      for the page.
    - 20 per page
    - Data should be laid out horizontally as follows:
      ` Rank | **{Team.Name}** | Rating | {list_of_team_member_discord_mention_strings}`
4. DiscordSeparatorComponent()
5. DiscordTextDisplayComponent() showing page information, e.g. `"Teams 1-20", 21-40, 41-60, etc.`
6. DiscordActionRowComponent() with Next and Previous pagebuttons.
      
