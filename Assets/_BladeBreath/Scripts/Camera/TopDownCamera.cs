using UnityEngine;

namespace BladeBreath
{
    [RequireComponent(typeof(Camera))]
    public sealed class TopDownCamera : MonoBehaviour
    {
        [SerializeField] private Vector3 offset = new Vector3(8.5f, 10.5f, -8.5f);
        [SerializeField, Min(0f)] private float followSharpness = 9f;
        [SerializeField, Min(1f)] private float orthographicSize = 7.4f;

        private Transform _target;
        private Camera _camera;

        private void Awake()
        {
            _camera = GetComponent<Camera>();
            _camera.orthographic = true;
            _camera.orthographicSize = orthographicSize;
        }

        private void LateUpdate()
        {
            if (_target == null)
            {
                return;
            }

            Vector3 desired = _target.position + offset;
            transform.position = Vector3.Lerp(
                transform.position,
                desired,
                1f - Mathf.Exp(-followSharpness * Time.deltaTime));

            transform.LookAt(_target.position + Vector3.up * 0.7f, Vector3.up);
        }

        public void Configure(Transform target)
        {
            _target = target;
            if (_camera == null)
            {
                _camera = GetComponent<Camera>();
            }

            _camera.orthographic = true;
            _camera.orthographicSize = orthographicSize;
            transform.position = target.position + offset;
            transform.LookAt(target.position + Vector3.up * 0.7f, Vector3.up);
        }
    }
}
