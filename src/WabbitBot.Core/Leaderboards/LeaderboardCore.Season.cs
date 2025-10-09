using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WabbitBot.Common.Attributes;
using WabbitBot.Common.Data.Interfaces;
using WabbitBot.Common.Data.Service;
using WabbitBot.Common.ErrorService;
using WabbitBot.Common.Events.Interfaces;
using WabbitBot.Common.Models;
using WabbitBot.Core.Common.BotCore;
using WabbitBot.Core.Common.Database;
using WabbitBot.Core.Common.Models.Common;
using WabbitBot.Core.Common.Models.Leaderboard;
using WabbitBot.Core.Common.Services;

namespace WabbitBot.Core.Leaderboards;

/// <summary>
/// Season-specific business logic service operations with multi-source data access
/// </summary>
public partial class LeaderboardCore { }
