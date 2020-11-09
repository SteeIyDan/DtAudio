using System.Collections.Generic;
using UnityEngine;

namespace SteelyDan
{
    public class AudioClipCollection
    {
        public string Name { get; set; }
        public List<NamedAudioClip> AudioClips { get; set; }

        public AudioClipCollection()
        {
            Name = "";
            AudioClips = new List<NamedAudioClip>();
        }

        public AudioClipCollection(string name)
        {
            Name = name;
            AudioClips = new List<NamedAudioClip>();
        }

        public static implicit operator AudioClipCollection(string name)
        {
            return new AudioClipCollection(name);
        }
    }
}