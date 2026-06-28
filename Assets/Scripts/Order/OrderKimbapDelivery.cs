using System.IO;
using KimbapGame.Data;
using KimbapGame.Evaluation;
using KimbapGame.Gameplay;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace KimbapGame.Order
{
    [DisallowMultipleComponent]
    public sealed class OrderKimbapDelivery : MonoBehaviour
    {
        [SerializeField] private string orderSceneName = "order";
        [SerializeField] private string resultFileName = "KimbapResults.xlsx";
        [SerializeField] private string customerObjectName = "Person";
        [SerializeField] private bool requirePickupBeforeDelivery = true;
        [SerializeField] private Camera targetCamera;

        private Collider2D[] kimbapColliders;
        private Rigidbody2D kimbapBody;
        private MouseThrow2D mouseThrow;
        private Transform customerTransform;
        private Collider2D customerCollider;
        private SpriteRenderer customerRenderer;
        private bool hasBeenPickedUp;
        private bool hasDelivered;
        private bool isDeliveryDragging;
        private Vector3 dragOffset;

        private void Awake()
        {
            CacheKimbapReferences();
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += HandleSceneLoaded;
            ConfigureForCurrentScene();
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
        }

        private void Update()
        {
            if (hasDelivered || SceneManager.GetActiveScene().name != orderSceneName)
            {
                return;
            }

            if (!isDeliveryDragging)
            {
                return;
            }

            transform.position = GetMouseWorldPosition() + dragOffset;

            if (Input.GetMouseButtonUp(0))
            {
                EndDeliveryDrag();
            }
        }

        private void OnMouseDown()
        {
            BeginDeliveryDragFromCurrentMouse();
        }

        public void BeginDeliveryDragFromCurrentMouse()
        {
            if (hasDelivered || SceneManager.GetActiveScene().name != orderSceneName)
            {
                return;
            }

            hasBeenPickedUp = true;
            isDeliveryDragging = true;
            dragOffset = transform.position - GetMouseWorldPosition();
            PrepareHeldPhysics();
        }

        private void EndDeliveryDrag()
        {
            if (!isDeliveryDragging)
            {
                return;
            }

            isDeliveryDragging = false;

            if ((!requirePickupBeforeDelivery || hasBeenPickedUp) && IsReleasedOnCustomer())
            {
                DeliverToCustomer();
                return;
            }

            PrepareRestingPhysics();
        }

        public void ConfigureForOrderDelivery(string savedResultFileName = "")
        {
            if (!string.IsNullOrWhiteSpace(savedResultFileName))
            {
                resultFileName = savedResultFileName;
            }

            CacheKimbapReferences();
            PrepareRestingPhysics();
            FindCustomer();
        }

        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            ConfigureForCurrentScene();
        }

        private void ConfigureForCurrentScene()
        {
            if (SceneManager.GetActiveScene().name == orderSceneName)
            {
                ConfigureForOrderDelivery();
            }
        }

        private void CacheKimbapReferences()
        {
            kimbapColliders = GetComponentsInChildren<Collider2D>(true);
            kimbapBody = GetComponent<Rigidbody2D>();
            mouseThrow = GetComponent<MouseThrow2D>();
        }

        private void PrepareRestingPhysics()
        {
            if (mouseThrow != null)
            {
                mouseThrow.enabled = false;
            }

            if (kimbapColliders != null)
            {
                for (int i = 0; i < kimbapColliders.Length; i++)
                {
                    if (kimbapColliders[i] != null)
                    {
                        kimbapColliders[i].isTrigger = true;
                    }
                }
            }

            if (kimbapBody != null)
            {
                kimbapBody.simulated = true;
                kimbapBody.gravityScale = 0f;
                kimbapBody.linearVelocity = Vector2.zero;
                kimbapBody.angularVelocity = 0f;
            }
        }

        private void PrepareHeldPhysics()
        {
            if (mouseThrow != null)
            {
                mouseThrow.enabled = false;
            }

            if (kimbapBody != null)
            {
                kimbapBody.linearVelocity = Vector2.zero;
                kimbapBody.angularVelocity = 0f;
                kimbapBody.simulated = false;
            }
        }
        private void FindCustomer()
        {
            customerTransform = null;
            customerCollider = null;
            customerRenderer = null;

            if (TryFindCustomerSpriteRenderer(true) || TryFindCustomerSpriteRenderer(false))
            {
                return;
            }

            GameObject namedCustomer = GameObject.Find(customerObjectName);
            if (namedCustomer != null)
            {
                AssignCustomer(namedCustomer.transform);
            }
        }

        private bool TryFindCustomerSpriteRenderer(bool requireRoot)
        {
            SpriteRenderer[] renderers = FindObjectsOfType<SpriteRenderer>();
            for (int i = 0; i < renderers.Length; i++)
            {
                SpriteRenderer renderer = renderers[i];
                if (renderer == null || renderer.gameObject.name != customerObjectName)
                {
                    continue;
                }

                if (requireRoot && renderer.transform.parent != null)
                {
                    continue;
                }

                AssignCustomer(renderer.transform);
                customerRenderer = renderer;
                return true;
            }

            return false;
        }

        private void AssignCustomer(Transform target)
        {
            customerTransform = target;
            customerCollider = target == null ? null : target.GetComponent<Collider2D>();
            customerRenderer = target == null ? null : target.GetComponent<SpriteRenderer>();
        }

        private bool IsTouchingCustomer()
        {
            if (customerTransform == null)
            {
                FindCustomer();
            }

            if (customerTransform == null)
            {
                return false;
            }

            Bounds kimbapBounds = GetKimbapBounds();
            Bounds customerBounds = GetCustomerBounds();
            ExpandDeliveryBounds(ref kimbapBounds);
            ExpandDeliveryBounds(ref customerBounds);

            return kimbapBounds.size != Vector3.zero
                && customerBounds.size != Vector3.zero
                && kimbapBounds.Intersects(customerBounds);
        }

        private bool IsReleasedOnCustomer()
        {
            return IsTouchingCustomer() || IsMouseOverCustomer();
        }

        private bool IsMouseOverCustomer()
        {
            if (customerTransform == null)
            {
                FindCustomer();
            }

            if (customerTransform == null)
            {
                return false;
            }

            Bounds customerBounds = GetCustomerBounds();
            ExpandDeliveryBounds(ref customerBounds);
            if (customerBounds.size == Vector3.zero)
            {
                return false;
            }

            Vector3 mouseWorldPosition = GetMouseWorldPosition();
            mouseWorldPosition.z = customerBounds.center.z;
            return customerBounds.Contains(mouseWorldPosition);
        }

        private static void ExpandDeliveryBounds(ref Bounds bounds)
        {
            if (bounds.size == Vector3.zero)
            {
                return;
            }

            float padding = Mathf.Max(0.35f, Mathf.Max(bounds.size.x, bounds.size.y) * 0.12f);
            bounds.Expand(new Vector3(padding, padding, 0f));
        }

        private Bounds GetKimbapBounds()
        {
            if (isDeliveryDragging || (kimbapBody != null && !kimbapBody.simulated))
            {
                return GetKimbapRendererBounds();
            }

            Bounds bounds = default;
            bool hasBounds = false;

            if (kimbapColliders == null || kimbapColliders.Length == 0)
            {
                CacheKimbapReferences();
            }

            for (int i = 0; i < kimbapColliders.Length; i++)
            {
                Collider2D collider = kimbapColliders[i];
                if (collider == null || !collider.enabled)
                {
                    continue;
                }

                if (!hasBounds)
                {
                    bounds = collider.bounds;
                    hasBounds = true;
                }
                else
                {
                    bounds.Encapsulate(collider.bounds);
                }
            }

            if (hasBounds)
            {
                return bounds;
            }

            SpriteRenderer renderer = GetComponentInChildren<SpriteRenderer>();
            return renderer == null ? new Bounds(transform.position, Vector3.zero) : renderer.bounds;
        }

        private Bounds GetKimbapRendererBounds()
        {
            SpriteRenderer[] renderers = GetComponentsInChildren<SpriteRenderer>(true);
            Bounds bounds = default;
            bool hasBounds = false;
            for (int i = 0; i < renderers.Length; i++)
            {
                SpriteRenderer renderer = renderers[i];
                if (renderer == null || !renderer.enabled)
                {
                    continue;
                }

                if (!hasBounds)
                {
                    bounds = renderer.bounds;
                    hasBounds = true;
                }
                else
                {
                    bounds.Encapsulate(renderer.bounds);
                }
            }

            return hasBounds ? bounds : new Bounds(transform.position, Vector3.one);
        }
        private Bounds GetCustomerBounds()
        {
            if (customerCollider != null && customerCollider.enabled)
            {
                return customerCollider.bounds;
            }

            Bounds rendererBounds = GetSpriteRendererBounds(customerTransform);
            if (rendererBounds.size != Vector3.zero)
            {
                return rendererBounds;
            }

            if (customerRenderer != null)
            {
                return customerRenderer.bounds;
            }

            return new Bounds(customerTransform.position, Vector3.one);
        }

        private static Bounds GetSpriteRendererBounds(Transform root)
        {
            if (root == null)
            {
                return default;
            }

            SpriteRenderer[] renderers = root.GetComponentsInChildren<SpriteRenderer>(true);
            Bounds bounds = default;
            bool hasBounds = false;
            for (int i = 0; i < renderers.Length; i++)
            {
                SpriteRenderer renderer = renderers[i];
                if (renderer == null || !renderer.enabled)
                {
                    continue;
                }

                if (!hasBounds)
                {
                    bounds = renderer.bounds;
                    hasBounds = true;
                }
                else
                {
                    bounds.Encapsulate(renderer.bounds);
                }
            }

            return hasBounds ? bounds : default;
        }

        private Vector3 GetMouseWorldPosition()
        {
            if (targetCamera == null)
            {
                targetCamera = Camera.main;
            }

            if (targetCamera == null)
            {
                return transform.position;
            }

            Vector3 screenPosition = Input.mousePosition;
            screenPosition.z = Mathf.Abs(targetCamera.transform.position.z - transform.position.z);
            return targetCamera.ScreenToWorldPoint(screenPosition);
        }
        private void DeliverToCustomer()
        {
            if (hasDelivered)
            {
                return;
            }

            hasDelivered = true;
            HideDeliveredKimbap();

            string path = Path.Combine(Application.persistentDataPath, resultFileName);
            KimbapEvaluationResult result = KimbapEvaluator.EvaluateLatestResultRow(SharedOrderContext.CurrentSheetOrder, path);
            SharedOrderContext.SetEvaluationResult(result);
            SharedOrderContext.CompleteCurrentOrder();

            OrderSceneController controller = FindObjectOfType<OrderSceneController>();
            if (controller != null)
            {
                controller.ShowEvaluationResult(result);
            }

            Destroy(gameObject);
        }

        private void HideDeliveredKimbap()
        {
            if (mouseThrow != null)
            {
                mouseThrow.enabled = false;
            }

            if (kimbapBody != null)
            {
                kimbapBody.linearVelocity = Vector2.zero;
                kimbapBody.angularVelocity = 0f;
                kimbapBody.simulated = false;
            }

            if (kimbapColliders != null)
            {
                for (int i = 0; i < kimbapColliders.Length; i++)
                {
                    if (kimbapColliders[i] != null)
                    {
                        kimbapColliders[i].enabled = false;
                    }
                }
            }

            SpriteRenderer[] renderers = GetComponentsInChildren<SpriteRenderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] != null)
                {
                    renderers[i].enabled = false;
                }
            }
        }
    }
}
