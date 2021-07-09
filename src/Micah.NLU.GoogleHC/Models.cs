namespace Micah.NLU.GoogleHC.Models
{
    using System;
    using System.Collections.Generic;

    using System.Globalization;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;

    public partial class ApiResponse
    {
        [JsonProperty("entityMentions")]
        public EntityMention[] EntityMentions { get; set; }

        [JsonProperty("entities")]
        public Entity[] EntitiesEntities { get; set; }

        [JsonProperty("relationships")]
        public Relationship[] Relationships { get; set; }
    }

    public partial class Entity
    {
        [JsonProperty("entityId")]
        public string EntityId { get; set; }

        [JsonProperty("preferredTerm", NullValueHandling = NullValueHandling.Ignore)]
        public string PreferredTerm { get; set; }

        [JsonProperty("vocabularyCodes")]
        public string[] VocabularyCodes { get; set; }
    }

    public partial class EntityMention
    {
        [JsonProperty("mentionId")]
        //[JsonConverter(typeof(Newtonsoft.Json.Converters.))]
        public long MentionId { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("text")]
        public Text Text { get; set; }

        [JsonProperty("linkedEntities")]
        public LinkedEntity[] LinkedEntities { get; set; }

        [JsonProperty("temporalAssessment", NullValueHandling = NullValueHandling.Ignore)]
        public CertaintyAssessment TemporalAssessment { get; set; }

        [JsonProperty("certaintyAssessment", NullValueHandling = NullValueHandling.Ignore)]
        public CertaintyAssessment CertaintyAssessment { get; set; }

        [JsonProperty("subject", NullValueHandling = NullValueHandling.Ignore)]
        public CertaintyAssessment Subject { get; set; }

        [JsonProperty("confidence")]
        public double Confidence { get; set; }
    }

    public partial class CertaintyAssessment
    {
        [JsonProperty("value")]
        public string Value { get; set; }

        [JsonProperty("confidence")]
        public double Confidence { get; set; }
    }

    public partial class LinkedEntity
    {
        [JsonProperty("entityId")]
        public string EntityId { get; set; }
    }

    public partial class Text
    {
        [JsonProperty("content")]
        public string Content { get; set; }

        [JsonProperty("beginOffset")]
        public long BeginOffset { get; set; }
    }

    public partial class Relationship
    {
        [JsonProperty("subjectId")]
        //[JsonConverter(typeof(ParseStringConverter))]
        public long SubjectId { get; set; }

        [JsonProperty("objectId")]
        //[JsonConverter(typeof(ParseStringConverter))]
        public long ObjectId { get; set; }

        [JsonProperty("confidence")]
        public double Confidence { get; set; }
    }
}
