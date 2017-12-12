using System.Collections.Generic;
using Vereesa.Data.Interfaces;

namespace Vereesa.Data.Models.GameTracking
{
    public class GameTrackMember : IEntity
    {
        public string Id { get; set; }
        public string Username { get; set; }
        public Dictionary<string, ICollection<GameTrackEntry>> GameHistory { get; set; }

        public ICollection<GameTrackEntry> GetGameHistory(string gameName) 
        {   
            if (!this.GameHistory.ContainsKey(gameName)) 
            {
                this.GameHistory[gameName] = new List<GameTrackEntry>();
            }

            return this.GameHistory[gameName];
        }
    }
}