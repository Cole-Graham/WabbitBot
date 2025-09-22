using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WabbitBot.Common.Data;
using WabbitBot.Core.Matches;

namespace WabbitBot.Core.Matches.Data
{
    public class MatchCache : Cache<Match, MatchListWrapper>
    {
        private const string KeyPrefix = "match:";
        private const string ListKey = "matches:active";
        private const int MaxMatchCacheSize = 2000; // Reasonable limit for matches

        public MatchCache() : base(MaxMatchCacheSize)
        {
        }

        public async Task SetMatchAsync(Match match, TimeSpan? expiry = null)
        {
            await SetAsync($"{KeyPrefix}{match.Id}", match, expiry);
        }

        public async Task<bool> RemoveMatchAsync(string matchId)
        {
            return await RemoveAsync($"{KeyPrefix}{matchId}");
        }

        public async Task<bool> MatchExistsAsync(string matchId)
        {
            return await ExistsAsync($"{KeyPrefix}{matchId}");
        }

        public async Task<MatchListWrapper> GetActiveMatchesAsync()
        {
            return await GetCollectionAsync(ListKey) ?? new MatchListWrapper();
        }

        public async Task SetActiveMatchesAsync(MatchListWrapper matches, TimeSpan? expiry = null)
        {
            await SetCollectionAsync(ListKey, matches, expiry);
        }

        public async Task<bool> RemoveActiveMatchesAsync()
        {
            return await RemoveCollectionAsync(ListKey);
        }

        public async Task<bool> ActiveMatchesExistAsync()
        {
            return await CollectionExistsAsync(ListKey);
        }
    }
}