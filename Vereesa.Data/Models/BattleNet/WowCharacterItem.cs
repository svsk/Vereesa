using System.Collections.Generic;

namespace Vereesa.Data.Models.BattleNet
{
    public class WowCharacterItem
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Icon { get; set; }
        public int Quality { get; set; }
        public int ItemLevel { get; set; }
        // public IList<Stat> Stats { get; set; }
        public int Armor { get; set; }
        public string Context { get; set; }
        public IList<int> BonusLists { get; set; }
        public int ArtifactId { get; set; }
        public int DisplayInfoId { get; set; }
        public int ArtifactAppearanceId { get; set; }
        public IList<ArtifactTrait> ArtifactTraits { get; set; }
        public IList<Relic> Relics { get; set; }
        // public  Appearance { get; set; }
    }
}