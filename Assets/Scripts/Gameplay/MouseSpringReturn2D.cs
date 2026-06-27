using UnityEngine;

namespace KimbapGame.Gameplay
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
    [RequireComponent(typeof(HingeJoint2D), typeof(TargetJoint2D))]
    [AddComponentMenu("Gameplay/Mouse Spring Return 2D")]
    public sealed class MouseSpringReturn2D : MonoBehaviour
    {
        [SerializeField] private Camera targetCamera;
        [SerializeField] private Vector2 anchorLocalPoint = new Vector2(0f, -0.5f);
        [SerializeField] private float returnMotorSpeed = 8f;
        [SerializeField] private float returnMotorDamping = 0.7f;
        [SerializeField] private float returnMaxMotorTorque = 1500f;
        [SerializeField] private float dragFrequency = 5f;
        [SerializeField] private float dragDampingRatio = 0.7f;
        [SerializeField] private float dragMaxForce = 1500f;

        private Rigidbody2D body;
        private HingeJoint2D pivotJoint;
        private TargetJoint2D dragJoint;
        private float startRotation;
        private Vector2 startAnchorWorld;
        private bool dragging;
        private bool initialized;

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            pivotJoint = GetComponent<HingeJoint2D>();

            if (pivotJoint == null)
            {
                pivotJoint = gameObject.AddComponent<HingeJoint2D>();
            }

            dragJoint = GetComponent<TargetJoint2D>();

            if (dragJoint == null)
            {
                dragJoint = gameObject.AddComponent<TargetJoint2D>();
            }

            if (targetCamera == null)
            {
                targetCamera = Camera.main;
            }
        }

        private void OnEnable()
        {
            if (body == null)
            {
                body = GetComponent<Rigidbody2D>();
            }

            if (pivotJoint == null)
            {
                pivotJoint = GetComponent<HingeJoint2D>();
            }

            if (dragJoint == null)
            {
                dragJoint = GetComponent<TargetJoint2D>();
            }

            InitializeHinge();
            ConfigureDragJoint(false);
        }

        private void OnDisable()
        {
            if (pivotJoint != null)
            {
                pivotJoint.enabled = false;
            }

            if (dragJoint != null)
            {
                dragJoint.enabled = false;
            }

            dragging = false;
        }

        private void OnMouseDown()
        {
            if (!EnsureCamera())
            {
                return;
            }

            if (!initialized)
            {
                InitializeHinge();
            }

            dragging = true;
            ConfigureDragJoint(true);
        }

        private void Update()
        {
            if (!dragging)
            {
                return;
            }

            if (Input.GetMouseButtonUp(0))
            {
                dragging = false;
                ConfigureDragJoint(false);
                return;
            }

            ApplyDragTarget();
        }

        private void FixedUpdate()
        {
            if (!initialized)
            {
                InitializeHinge();
            }

            ApplyHingeSettings();
            ApplyReturnMotor();
        }

        private void InitializeHinge()
        {
            startRotation = body.rotation;
            startAnchorWorld = transform.TransformPoint(anchorLocalPoint);

            if (pivotJoint == null)
            {
                return;
            }

            pivotJoint.autoConfigureConnectedAnchor = false;
            pivotJoint.connectedBody = null;
            pivotJoint.anchor = anchorLocalPoint;
            pivotJoint.connectedAnchor = startAnchorWorld;
            pivotJoint.useConnectedAnchor = true;
            pivotJoint.useLimits = false;
            pivotJoint.enabled = true;

            ApplyHingeSettings();
            initialized = true;
        }

        private void ApplyHingeSettings()
        {
            if (pivotJoint == null)
            {
                return;
            }

            pivotJoint.anchor = anchorLocalPoint;
            pivotJoint.connectedAnchor = startAnchorWorld;
            pivotJoint.useConnectedAnchor = true;
        }

        private void ApplyReturnMotor()
        {
            if (pivotJoint == null)
            {
                return;
            }

            float angleDelta = Mathf.DeltaAngle(body.rotation, startRotation);
            JointMotor2D motor = pivotJoint.motor;
            motor.motorSpeed = angleDelta * Mathf.Max(0f, returnMotorSpeed);
            motor.motorSpeed -= body.angularVelocity * Mathf.Max(0f, returnMotorDamping);
            motor.maxMotorTorque = Mathf.Max(0f, returnMaxMotorTorque);

            pivotJoint.motor = motor;
            pivotJoint.useMotor = motor.maxMotorTorque > 0f;
        }

        private void ConfigureDragJoint(bool enabled)
        {
            if (dragJoint == null)
            {
                return;
            }

            dragJoint.enabled = enabled;
            dragJoint.autoConfigureTarget = false;
            dragJoint.frequency = Mathf.Max(0f, dragFrequency);
            dragJoint.dampingRatio = Mathf.Max(0f, dragDampingRatio);
            dragJoint.maxForce = Mathf.Max(0f, dragMaxForce);

            if (enabled)
            {
                Vector2 mouseWorld = GetMouseWorldPosition();
                dragJoint.anchor = transform.InverseTransformPoint(mouseWorld);
                dragJoint.target = mouseWorld;
            }
        }

        private void ApplyDragTarget()
        {
            if (dragJoint == null)
            {
                return;
            }

            dragJoint.target = GetMouseWorldPosition();
        }

        private Vector2 GetMouseWorldPosition()
        {
            Vector3 screenPosition = Input.mousePosition;
            screenPosition.z = Mathf.Abs(targetCamera.transform.position.z - transform.position.z);
            return targetCamera.ScreenToWorldPoint(screenPosition);
        }

        private bool EnsureCamera()
        {
            if (targetCamera == null)
            {
                targetCamera = Camera.main;
            }

            return targetCamera != null;
        }

        private void OnDrawGizmosSelected()
        {
            Vector3 currentAnchor = transform.TransformPoint(anchorLocalPoint);
            Vector3 anchor = Application.isPlaying && initialized ? (Vector3)startAnchorWorld : currentAnchor;

            Gizmos.color = Color.red;
            Gizmos.DrawSphere(anchor, 0.08f);

            Gizmos.color = Color.green;
            Gizmos.DrawLine(anchor, currentAnchor);

            if (Application.isPlaying && initialized)
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawLine(anchor, anchor + Quaternion.Euler(0f, 0f, startRotation) * Vector3.right);
            }
        }
    }
}
