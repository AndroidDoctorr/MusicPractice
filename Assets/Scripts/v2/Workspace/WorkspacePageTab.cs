using UnityEngine;
using Valve.VR.InteractionSystem;

namespace MusicPractice.SpatialLoop
{
    [RequireComponent(typeof(BoxCollider))]
    public sealed class WorkspacePageTab : MonoBehaviour
    {
        private SpatialWorkspaceController _workspace;
        private WorkspacePage _page;
        private float _lastSelectTime;

        public WorkspacePage Page => _page;

        public void Initialize(
            SpatialWorkspaceController workspace,
            WorkspacePage page)
        {
            _workspace = workspace;
            _page = page;
            BoxCollider collider = GetComponent<BoxCollider>();
            collider.isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (_workspace == null)
            {
                return;
            }

            if (other.GetComponentInParent<Hand>() == null
                && other.GetComponentInParent<SampleTokenView>() == null
                && other.GetComponentInParent<SampleInstanceView>() == null)
            {
                return;
            }

            if (Time.unscaledTime - _lastSelectTime < 0.35f)
            {
                return;
            }

            _lastSelectTime = Time.unscaledTime;
            _workspace.SetPage(_page);
        }
    }
}
