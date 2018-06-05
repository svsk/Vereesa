using System.Collections.Generic;

namespace Vereesa.Data.Models.BattleNet
{
    public class Relic
    {
        public int Socket { get; set; }
        public int ItemId { get; set; }
        public int Context { get; set; }
        public IList<int> BonusLists { get; set; }
    }
}