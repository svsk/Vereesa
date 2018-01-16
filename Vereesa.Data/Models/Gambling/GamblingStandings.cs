using System;
using System.Collections.Generic;
using Vereesa.Data.Interfaces;

namespace Vereesa.Data.Models.Gambling
{
    public class GamblingStandings : IEntity
    {
        public GamblingStandings()
        {
            Id = Guid.NewGuid().ToString();
            Ranking = new Dictionary<ulong, int>();
        }

        public string Id { get; set; }
        public Dictionary<ulong, int> Ranking { get; set; }
    }
}