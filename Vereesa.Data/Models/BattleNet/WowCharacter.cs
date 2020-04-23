using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;


namespace Vereesa.Data.Models.BattleNet
{
    public class BattleNetMediaResponse 
    {
        [JsonProperty("avatar_url")]
        public string AvatarUrl { get; set; }

        [JsonProperty("bust_url")]
        public string BustUrl { get; set; }

        [JsonProperty("render_url")]
        public string RenderUrl { get; set; }
    }

    public partial class BattleNetCharacterResponse
    {
        [JsonProperty("equipped_items")]
        public List<EquippedItem> EquippedItems { get; set; }
    }

    public partial class Character
    {
        [JsonProperty("key")]
        public Self Key { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("id")]
        public long Id { get; set; }

        [JsonProperty("realm", NullValueHandling = NullValueHandling.Ignore)]
        public Character Realm { get; set; }

        [JsonProperty("slug", NullValueHandling = NullValueHandling.Ignore)]
        public string Slug { get; set; }
    }

    public partial class Self
    {
        [JsonProperty("href")]
        public Uri Href { get; set; }
    }

    public partial class Effect
    {
        [JsonProperty("display_string")]
        public string DisplayString { get; set; }

        [JsonProperty("required_count")]
        public long RequiredCount { get; set; }
    }

    public partial class ItemElement
    {
        [JsonProperty("item")]
        public Character Item { get; set; }

        [JsonProperty("is_equipped", NullValueHandling = NullValueHandling.Ignore)]
        public bool? IsEquipped { get; set; }
    }

    public partial class EquippedItem
    {
        [JsonProperty("slot")]
        public Binding Slot { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("level")]
        public Level Level { get; set; }

        [JsonProperty("azerite_details", NullValueHandling = NullValueHandling.Ignore)]
        public AzeriteDetails AzeriteDetails { get; set; }
    }


    public partial class AzeriteDetails
    {
        [JsonProperty("selected_powers", NullValueHandling = NullValueHandling.Ignore)]
        public List<SelectedPower> SelectedPowers { get; set; }

        [JsonProperty("selected_powers_string", NullValueHandling = NullValueHandling.Ignore)]
        public string SelectedPowersString { get; set; }

        [JsonProperty("percentage_to_next_level", NullValueHandling = NullValueHandling.Ignore)]
        public double? PercentageToNextLevel { get; set; }

        [JsonProperty("selected_essences", NullValueHandling = NullValueHandling.Ignore)]
        public List<SelectedEssence> SelectedEssences { get; set; }

        [JsonProperty("level", NullValueHandling = NullValueHandling.Ignore)]
        public Level Level { get; set; }
    }

    public partial class Level
    {
        [JsonProperty("value")]
        public double Value { get; set; }

        [JsonProperty("display_string")]
        public string DisplayString { get; set; }
    }

    public partial class SelectedEssence
    {
        [JsonProperty("slot")]
        public long Slot { get; set; }

        [JsonProperty("rank")]
        public long Rank { get; set; }

        [JsonProperty("main_spell_tooltip", NullValueHandling = NullValueHandling.Ignore)]
        public SpellTooltip MainSpellTooltip { get; set; }

        [JsonProperty("passive_spell_tooltip")]
        public SpellTooltip PassiveSpellTooltip { get; set; }

        [JsonProperty("essence")]
        public Character Essence { get; set; }

        [JsonProperty("media")]
        public MediaClass Media { get; set; }
    }

    public partial class SpellTooltip
    {
        [JsonProperty("spell")]
        public Character Spell { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("cast_time")]
        public CastTime CastTime { get; set; }

        [JsonProperty("range", NullValueHandling = NullValueHandling.Ignore)]
        public string Range { get; set; }
    }

    public partial class MediaClass
    {
        [JsonProperty("key")]
        public Self Key { get; set; }

        [JsonProperty("id")]
        public long Id { get; set; }
    }

    public partial class SelectedPower
    {
        [JsonProperty("id")]
        public long Id { get; set; }

        [JsonProperty("tier")]
        public long Tier { get; set; }

        [JsonProperty("spell_tooltip")]
        public SpellTooltip SpellTooltip { get; set; }

        [JsonProperty("is_display_hidden", NullValueHandling = NullValueHandling.Ignore)]
        public bool? IsDisplayHidden { get; set; }
    }

    public partial class Binding
    {
        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }
    }

    public enum CastTime { Instant, Passive };
}
