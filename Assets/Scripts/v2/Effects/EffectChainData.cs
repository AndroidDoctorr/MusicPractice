using System;
using UnityEngine;

namespace MusicPractice.SpatialLoop
{
    public enum EffectPairId
    {
        Filter,
        Space
    }

    [Serializable]
    public sealed class EffectChainData
    {
        [SerializeField, Range(0f, 1f)] private float _filterX = 0.5f;
        [SerializeField, Range(0f, 1f)] private float _filterY;
        [SerializeField] private bool _filterBypassed;
        [SerializeField, Range(0f, 1f)] private float _spaceX;
        [SerializeField, Range(0f, 1f)] private float _spaceY;
        [SerializeField] private bool _spaceBypassed;

        public Vector2 GetXY(EffectPairId pair)
        {
            return pair == EffectPairId.Filter
                ? new Vector2(_filterX, _filterY)
                : new Vector2(_spaceX, _spaceY);
        }

        public void SetXY(EffectPairId pair, Vector2 value)
        {
            value.x = Mathf.Clamp01(value.x);
            value.y = Mathf.Clamp01(value.y);

            if (pair == EffectPairId.Filter)
            {
                _filterX = value.x;
                _filterY = value.y;
            }
            else
            {
                _spaceX = value.x;
                _spaceY = value.y;
            }
        }

        public bool IsBypassed(EffectPairId pair)
        {
            return pair == EffectPairId.Filter
                ? _filterBypassed
                : _spaceBypassed;
        }

        public void SetBypassed(EffectPairId pair, bool bypassed)
        {
            if (pair == EffectPairId.Filter)
            {
                _filterBypassed = bypassed;
            }
            else
            {
                _spaceBypassed = bypassed;
            }
        }

        public EffectChainData Clone()
        {
            var clone = new EffectChainData();
            clone._filterX = _filterX;
            clone._filterY = _filterY;
            clone._filterBypassed = _filterBypassed;
            clone._spaceX = _spaceX;
            clone._spaceY = _spaceY;
            clone._spaceBypassed = _spaceBypassed;
            return clone;
        }
    }

    [Serializable]
    public sealed class EffectPresetData
    {
        public string Name;
        public EffectPairId Pair;
        public Vector2 XY;
        public bool Bypassed;

        public EffectPresetData(
            string name,
            EffectPairId pair,
            Vector2 xy,
            bool bypassed = false)
        {
            Name = name;
            Pair = pair;
            XY = xy;
            Bypassed = bypassed;
        }
    }
}
