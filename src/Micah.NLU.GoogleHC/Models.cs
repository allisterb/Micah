namespace Micah.NLU.GoogleHC.Models
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
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

        public override string ToString() => string.Format("Entities: {0}. Relationships: {1}.", EntityMentions.Select(e => e.ToString()).Aggregate((e1, e2) => e1 + " " + e2), Relationships.Select(e => e.ToString()).Aggregate((e1, e2) => e1 + " " + e2));      
    }

    public partial class Entity
    {
        [JsonProperty("entityId")]
        public string EntityId { get; set; }

        [JsonProperty("preferredTerm", NullValueHandling = NullValueHandling.Ignore)]
        public string PreferredTerm { get; set; }

        [JsonProperty("vocabularyCodes")]
        public string[] VocabularyCodes { get; set; }

        public override string ToString() => string.Format("Id:{0}. PreferredTerm:{1}. Codes:{2}.", EntityId, PreferredTerm, VocabularyCodes);
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

        public override string ToString() => string.Format("(Id:{0}, Type:{1}, Text:{2}, TemporalAssessment:{3}, CertaintyAssessment:{4}, Subject:{5}, Confidence:{6})", MentionId, Type, Text, TemporalAssessment, CertaintyAssessment, Subject, Confidence);
    }

    public partial class CertaintyAssessment
    {
        [JsonProperty("value")]
        public string Value { get; set; }

        [JsonProperty("confidence")]
        public double Confidence { get; set; }

        public override string ToString() => string.Format("(Value:{0}, Confidence:{1})", Value, Confidence);
    }

    public partial class LinkedEntity
    {
        [JsonProperty("entityId")]
        public string EntityId { get; set; }

        public override string ToString() => string.Format("(Id: {0})", EntityId);
    }

    public partial class Text
    {
        [JsonProperty("content")]
        public string Content { get; set; }

        [JsonProperty("beginOffset")]
        public long BeginOffset { get; set; }

        public override string ToString() => Content;
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

        public override string ToString() => string.Format("(Subject:{0}, Object:{1}, Confidence:{2})", SubjectId, ObjectId, Confidence);
    }
}
