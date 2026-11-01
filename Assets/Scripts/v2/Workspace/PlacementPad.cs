using UnityEngine;

namespace MusicPractice.SpatialLoop
{
    public enum PlacementPadKind
    {
        Role,
        Distribution,
        Arpeggiator
    }

    [RequireComponent(typeof(BoxCollider))]
    public sealed class PlacementPad : MonoBehaviour
    {
        private SpatialWorkspaceController _workspace;
        private PlacementPadKind _kind;
        private SampleRole _role;
        private int _distributionCount;
        private ArpeggiatorPattern _arpeggiatorPattern;
        private BoxCollider _collider;

        public PlacementPadKind Kind => _kind;
        public SampleRole Role => _role;
        public int DistributionCount => _distributionCount;
        public ArpeggiatorPattern ArpeggiatorPattern => _arpeggiatorPattern;
        public Bounds WorldBounds => _collider.bounds;

        private void Awake()
        {
            _collider = GetComponent<BoxCollider>();
            _collider.isTrigger = true;
        }

        public void InitializeRole(
            SpatialWorkspaceController workspace,
            SampleRole role)
        {
            _workspace = workspace;
            _kind = PlacementPadKind.Role;
            _role = role;
            name = $"Role {role}";
        }

        public void InitializeDistribution(
            SpatialWorkspaceController workspace,
            int count)
        {
            _workspace = workspace;
            _kind = PlacementPadKind.Distribution;
            _distributionCount = count;
            name = $"Distribution {count}";
        }

        public void InitializeArpeggiator(
            SpatialWorkspaceController workspace,
            ArpeggiatorPattern pattern)
        {
            _workspace = workspace;
            _kind = PlacementPadKind.Arpeggiator;
            _arpeggiatorPattern = pattern;
            name = $"Arp {pattern.Name}";
        }

        private void OnTriggerEnter(Collider other)
        {
            SampleTokenView token = other.GetComponent<SampleTokenView>();
            if (token != null)
            {
                _workspace?.BeginPreview(this, token.Asset);
                return;
            }

            SampleInstanceView instance = other.GetComponent<SampleInstanceView>();
            if (instance != null)
            {
                _workspace?.BeginPreview(this, instance.Data);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.GetComponent<SampleTokenView>() != null
                || other.GetComponent<SampleInstanceView>() != null)
            {
                _workspace?.EndPreview(this);
            }
        }
    }
}
