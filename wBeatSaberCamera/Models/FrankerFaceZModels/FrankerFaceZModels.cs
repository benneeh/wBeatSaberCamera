using System.Collections.Generic;
using System.Runtime.Serialization;

namespace wBeatSaberCamera.Models.FrankerFaceZModels
{
    [DataContract]
    public class FfzRoot
    {
        [DataMember(Name = "room")]
        public Room Room { get; set; }

        [DataMember(Name = "sets")]
        public Dictionary<int, Set> Sets { get; set; }
    }

    [DataContract]
    public class Room
    {
        [DataMember(Name = "set")]
        public int Set { get; set; }
    }

    [DataContract]
    public class Set
    {
        [DataMember(Name = "emoticons")]
        public Emoticon[] Emoticons { get; set; }
    }

    [DataContract]
    public class Emoticon
    {
        [DataMember(Name = "hidden")]
        public bool Hidden { get; set; }

        [DataMember(Name = "name")]
        public string Name { get; set; }
    }
}