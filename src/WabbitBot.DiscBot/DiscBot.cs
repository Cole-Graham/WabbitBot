// using DSharpPlus;
// using DSharpPlus.Commands;
// using DSharpPlus.Commands.Processors.SlashCommands;
// using DSharpPlus.Commands.Processors.TextCommands;
// using DSharpPlus.Entities;
// using WabbitBot.Common.Events;
// using WabbitBot.Common;
// using WabbitBot.DiscBot.DSharpPlus.Interactions;
// using WabbitBot.DiscBot.DSharpPlus.Commands;
// using WabbitBot.DiscBot.DiscBot.Events;
// using WabbitBot.DiscBot.DiscBot.Services;

// namespace WabbitBot.DiscBot.DSharpPlus;

// public class DiscordBot
// {
//     private readonly DiscordClient _client;
//     private readonly DiscordEventBus _eventBus;
//     private readonly ErrorHandler _errorHandler;
//     private readonly IGlobalEventBus _globalEventBus;
//     private readonly IBotConfigurationService _config;
//     private readonly CommandRegistrationHandler _commandRegistrationHandler;
//     public DiscordBot(IBotConfigurationService config)
//     {
//         _globalEventBus = GlobalEventBusProvider.GetGlobalEventBus();
//         _config = config;
//         _eventBus = DiscordEventBus.Instance;
//         _errorHandler = new ErrorHandler(_globalEventBus);
//         _commandRegistrationHandler = new CommandRegistrationHandler(_eventBus);

//         // Create client using builder pattern
//         _client = DiscordClientBuilder
//             .CreateDefault(
//                 _config.GetToken(),  // Token set in InitializeAsync
//                 DiscordIntents.MessageContents |
//                 DiscordIntents.DirectMessages |
//                 DiscordIntents.GuildMessages |
//                 DiscordIntents.Guilds,
//                 null  // No service collection since we're avoiding DI
//             )
//             .UseCommands((services, commands) =>
//             {
//                 // Configure slash commands
//                 commands.AddProcessor<SlashCommandProcessor>();
//                 commands.AddProcessor<TextCommandProcessor>();

//                 // Register all Discord command classes using the new DSharpPlus 5.0 API
//                 commands.AddCommands<ScrimmageCommandsDiscord>();
//                 commands.AddCommands<TeamCommandsDiscord>();
//                 commands.AddCommands<MapCommandsDiscord>();
//                 commands.AddCommands<ConfigurationCommandsDiscord>();
//             })
//             .ConfigureEventHandlers(b =>
//             {
//                 b.HandleSocketClosed((client, args) => _errorHandler.HandleError(
//                     new Exception($"Socket closed with code {args.CloseCode}: {args.CloseMessage}")
//                 ));
//                 b.HandleSessionCreated((client, args) => Task.CompletedTask);
//                 b.HandleZombied((client, args) => _errorHandler.HandleError(
//                     new Exception("Discord connection zombied")
//                 ));
//                 b.HandleComponentInteractionCreated((client, args) =>
//                 {
//                     // Handle both button interactions and dropdown selections
//                     if (args.Interaction.Data.ComponentType == DiscordComponentType.Button)
//                     {
//                         return ScrimmageButtonInteractions.HandleButtonInteractionAsync(client, args);
//                     }
//                     else if (args.Interaction.Data.ComponentType == DiscordComponentType.StringSelect)
//                     {
//                         return ScrimmageModalInteractions.HandleDropdownSelectionAsync(client, args);
//                     }
//                     return Task.CompletedTask;
//                 });
//             })
//             .Build();
//     }


//     public async Task InitializeAsync()
//     {
//         try
//         {
//             // Database is already initialized in Program.cs (Core project)
//             // TODO: Need to confirm correct DSharpPlus 5.0 token and connection handling
//             await _client.ConnectAsync();
//         }
//         catch (Exception ex)
//         {
//             await _errorHandler.HandleError(ex);
//             throw;
//         }
//     }

//     public async Task StartAsync()
//     {
//         // Register the client with the service locator for other services to use
//         DiscordClientProvider.SetClient(_client);

//         _eventBus.RegisterDSharpPlusEventHandlers(_client);

//         // Initialize command registration handler
//         await _commandRegistrationHandler.InitializeAsync();

//         // Publish DiscordClientReadyEvent to trigger command registration (Discord-internal event)
//         await _eventBus.PublishAsync(new DiscordClientReadyEvent
//         {
//             Client = _client
//         });
//     }

//     public async Task StopAsync()
//     {
//         try
//         {
//             if (_client != null)
//             {
//                 await _client.DisconnectAsync();
//                 _client.Dispose();
//             }
//         }
//         catch (Exception ex)
//         {
//             await _errorHandler.HandleError(ex);
//             throw;
//         }
//     }
// }
