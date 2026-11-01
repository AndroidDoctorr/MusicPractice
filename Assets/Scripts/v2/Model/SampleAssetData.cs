using System;
using UnityEngine;

namespace MusicPractice.SpatialLoop
{
    [Serializable]
    public sealed class SampleAssetData
    {
        [SerializeField] private string _id;
        [SerializeField] private string _displayName;
        [SerializeField] private AudioClip _clip;
        [SerializeField] private Color _color = Color.red;
        [SerializeField] private EffectChainData _defaultEffects = new EffectChainData();

        public string Id => _id;
        public string DisplayName => _displayName;
        public AudioClip Clip => _clip;
        public Color Color => _color;
        public EffectChainData DefaultEffects =>
            _defaultEffects ?? (_defaultEffects = new EffectChainData());
        public float DurationSeconds => _clip != null ? _clip.length : 0f;

        public SampleAssetData(
            string displayName,
            AudioClip clip,
            Color color,
            EffectChainData defaultEffects = null,
            string id = null)
        {
            if (clip == null)
            {
                throw new ArgumentNullException(nameof(clip));
            }

            _id = string.IsNullOrWhiteSpace(id)
                ? Guid.NewGuid().ToString("N")
                : id;
            _displayName = string.IsNullOrWhiteSpace(displayName)
                ? "Sample"
                : displayName;
            _clip = clip;
            _color = color;
            _defaultEffects = defaultEffects?.Clone() ?? new EffectChainData();
        }
    }
}
