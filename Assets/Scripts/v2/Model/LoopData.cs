using System;
using System.Collections.Generic;
using UnityEngine;

namespace MusicPractice.SpatialLoop
{
    [Serializable]
    public sealed class LoopData
    {
        [SerializeField] private string _id;
        [SerializeField] private string _name = "Loop 1";
        [SerializeField, Min(1)] private int _bars = 1;
        [SerializeField] private TimeSignatureData _timeSignature = new TimeSignatureData();
        [SerializeField] private List<SampleInstanceData> _instances = new List<SampleInstanceData>();

        public string Id => _id;
        public string Name => _name;
        public int Bars => Mathf.Max(1, _bars);
        public TimeSignatureData TimeSignature =>
            _timeSignature ?? (_timeSignature = new TimeSignatureData());
        public IReadOnlyList<SampleInstanceData> Instances =>
            _instances ?? (_instances = new List<SampleInstanceData>());
        public long LengthTicks => MusicalTime.GetLoopLengthTicks(TimeSignature, Bars);

        public LoopData()
        {
            _id = Guid.NewGuid().ToString("N");
        }

        public LoopData(string name, int bars, TimeSignatureData timeSignature)
        {
            if (bars < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(bars));
            }

            _id = Guid.NewGuid().ToString("N");
            _name = string.IsNullOrWhiteSpace(name) ? "Loop" : name;
            _bars = bars;
            _timeSignature = timeSignature ?? throw new ArgumentNullException(nameof(timeSignature));
            _instances = new List<SampleInstanceData>();
        }

        public void AddInstance(SampleInstanceData instance)
        {
            if (instance == null)
            {
                throw new ArgumentNullException(nameof(instance));
            }

            if (_instances == null)
            {
                _instances = new List<SampleInstanceData>();
            }

            _instances.Add(instance);
        }

        public bool RemoveInstance(string instanceId)
        {
            if (string.IsNullOrWhiteSpace(instanceId))
            {
                return false;
            }

            if (_instances == null)
            {
                return false;
            }

            return _instances.RemoveAll(instance => instance.Id == instanceId) > 0;
        }
    }
}
