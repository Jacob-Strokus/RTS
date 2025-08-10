using UnityEngine;

namespace FrontierAges.Presentation {
    public class RTSCameraController : MonoBehaviour {
        public float PanSpeed = 20f;
        public float RotateSpeed = 90f;
        public float ZoomSpeed = 200f;
        public float MinY = 10f;
        public float MaxY = 120f;
        public Vector2 PanBoundsX = new Vector2(-200, 200);
        public Vector2 PanBoundsZ = new Vector2(-200, 200);

        private Camera _cam;
        void Awake() { _cam = GetComponentInChildren<Camera>(); if (!_cam) _cam = Camera.main; }

        void Update() {
            float dt = Time.deltaTime;
            Vector3 pos = transform.position;
            // WASD / Arrow pan
            float h = Input.GetAxisRaw("Horizontal");
            float v = Input.GetAxisRaw("Vertical");
            Vector3 forward = new Vector3(transform.forward.x, 0, transform.forward.z).normalized;
            Vector3 right = new Vector3(transform.right.x, 0, transform.right.z).normalized;
            pos += (forward * v + right * h) * PanSpeed * dt;

            // Edge pan (optional)
            var mp = Input.mousePosition;
            const int edge = 8;
            if (mp.x <= edge) pos += -right * PanSpeed * dt;
            else if (mp.x >= Screen.width - edge) pos += right * PanSpeed * dt;
            if (mp.y <= edge) pos += -forward * PanSpeed * dt;
            else if (mp.y >= Screen.height - edge) pos += forward * PanSpeed * dt;

            // Zoom (mouse wheel)
            float scroll = Input.mouseScrollDelta.y;
            if (Mathf.Abs(scroll) > 0.01f) {
                pos += Vector3.up * (-scroll * ZoomSpeed * dt);
            }
            pos.y = Mathf.Clamp(pos.y, MinY, MaxY);
            pos.x = Mathf.Clamp(pos.x, PanBoundsX.x, PanBoundsX.y);
            pos.z = Mathf.Clamp(pos.z, PanBoundsZ.x, PanBoundsZ.y);
            transform.position = pos;

            // Rotate (Q/E)
            float rot = 0f;
            if (Input.GetKey(KeyCode.Q)) rot -= 1f;
            if (Input.GetKey(KeyCode.E)) rot += 1f;
            if (rot != 0) transform.rotation = Quaternion.Euler(0, transform.eulerAngles.y + rot * RotateSpeed * dt, 0);
        }
    }
}
