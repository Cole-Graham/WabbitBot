// using System;
// using System.Linq;
// using Xunit;

// namespace WabbitBot.Core.Common.Config
// {
//     public class EntityConfigTests
//     {
//         [Fact]
//         public void PlayerDbConfig_ShouldHaveCorrectSettings()
//         {
//             var config = EntityConfigFactory.Player;
//             Assert.Equal("players", config.TableName);
//             Assert.Equal("players_archive", config.ArchiveTableName);
//         }

//         [Fact]
//         public void TeamDbConfig_ShouldHaveCorrectSettings()
//         {
//             var config = EntityConfigFactory.Team;
//             Assert.Equal("teams", config.TableName);
//             Assert.Equal("teams_archive", config.ArchiveTableName);
//         }

//         [Fact]
//         public void MapConfig_ShouldHaveCorrectSettings()
//         {
//             var config = EntityConfigFactory.Map;
//             Assert.Equal("maps", config.TableName);
//             Assert.Equal("maps_archive", config.ArchiveTableName);
//         }

//         [Fact]
//         public void UserDbConfig_ShouldHaveCorrectSettings()
//         {
//             var config = EntityConfigFactory.User;
//             Assert.Equal("users", config.TableName);
//             Assert.Equal("users_archive", config.ArchiveTableName);
//         }

//         [Fact]
//         public void GetAllConfigurations_ShouldReturnSomeConfigs()
//         {
//             var configs = EntityConfigFactory.GetAllConfigurations().ToList();
//             Assert.True(configs.Count >= 10);
//             Assert.Contains(configs, c => c.TableName == "players");
//             Assert.Contains(configs, c => c.TableName == "teams");
//             Assert.Contains(configs, c => c.TableName == "maps");
//             Assert.Contains(configs, c => c.TableName == "users");
//         }
//     }
// }


