using System.Collections;
using System.Collections.Generic;
using KimbapGame.Data;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace KimbapGame.Order
{
    [RequireComponent(typeof(GoogleSheetOrderLoader))]
    public class OrderSceneController : MonoBehaviour
    {
        [Header("Google Sheet")]
        [SerializeField]
        private string sheetCsvUrl = "https://docs.google.com/spreadsheets/d/13gq3ZV-LhXbPbYeEcljMLyYC_6GyXHLdkcgjHbalqcI/export?format=csv&gid=650686695";

        [Header("Scene")]
        [SerializeField]
        private string kitchenSceneName = "Kitchen";

        [Header("UI")]
        [SerializeField]
        private Image personImage;
        [SerializeField]
        private SpriteRenderer personSpriteRenderer;
        [SerializeField]
        private TMP_Text conversationText;
        [SerializeField]
        private Button hintButton;
        [SerializeField]
        private Button refuseButton;
        [SerializeField]
        private Button toKitchenButton;

        [Header("Fallback")]
        [SerializeField]
        private string loadingMessage = "주문을 불러오는 중...";
        [SerializeField]
        private string emptyMessage = "받을 수 있는 주문이 없습니다.";
        [SerializeField]
        private string errorMessage = "주문을 불러오지 못했습니다.";

        private readonly List<SheetOrderData> orders = new List<SheetOrderData>();
        private GoogleSheetOrderLoader loader;
        private SheetOrderData currentOrder;
        private int currentOrderIndex = -1;

        private void Awake()
        {
            loader = GetComponent<GoogleSheetOrderLoader>();
            loader.CsvUrl = sheetCsvUrl;
            AutoBindMissingReferences();
        }

        private void OnEnable()
        {
            if (hintButton != null)
            {
                hintButton.onClick.AddListener(ShowHint);
            }

            if (refuseButton != null)
            {
                refuseButton.onClick.AddListener(RefuseCurrentOrder);
            }

            if (toKitchenButton != null)
            {
                toKitchenButton.onClick.AddListener(GoToKitchen);
            }
        }

        private void OnDisable()
        {
            if (hintButton != null)
            {
                hintButton.onClick.RemoveListener(ShowHint);
            }

            if (refuseButton != null)
            {
                refuseButton.onClick.RemoveListener(RefuseCurrentOrder);
            }

            if (toKitchenButton != null)
            {
                toKitchenButton.onClick.RemoveListener(GoToKitchen);
            }
        }

        private void Start()
        {
            SetConversation(loadingMessage);
            StartCoroutine(LoadOrders());
        }

        public void RefuseCurrentOrder()
        {
            if (orders.Count == 0)
            {
                SetConversation(emptyMessage);
                return;
            }

            ShowNextOrder();
        }

        public void ShowHint()
        {
            if (currentOrder == null)
            {
                return;
            }

            if (!string.IsNullOrWhiteSpace(currentOrder.hintDialogue))
            {
                SetConversation(currentOrder.hintDialogue);
            }

            if (!string.IsNullOrWhiteSpace(currentOrder.hintImageName))
            {
                SetPersonImage(currentOrder.hintImageName);
            }
        }

        public void GoToKitchen()
        {
            if (currentOrder == null)
            {
                return;
            }

            SharedOrderContext.ReturnSceneName = SceneManager.GetActiveScene().name;
            SharedOrderContext.SetCurrentOrder(currentOrder.ToOrderData(), currentOrder);
            SceneManager.LoadScene(kitchenSceneName);
        }

        private IEnumerator LoadOrders()
        {
            yield return loader.LoadOrders(
                loadedOrders =>
                {
                    orders.Clear();
                    orders.AddRange(loadedOrders);

                    if (!TryRestoreCurrentOrder())
                    {
                        ShowNextOrder();
                    }
                },
                error =>
                {
                    Debug.LogWarning($"Order sheet load failed: {error}");
                    SetConversation(errorMessage);
                });
        }

        private bool TryRestoreCurrentOrder()
        {
            SheetOrderData savedOrder = SharedOrderContext.CurrentSheetOrder;
            if (savedOrder == null)
            {
                return false;
            }

            int restoredIndex = orders.FindIndex(order => order.index == savedOrder.index);
            if (restoredIndex < 0)
            {
                restoredIndex = orders.FindIndex(order => order.customerName == savedOrder.customerName && order.orderDialogue == savedOrder.orderDialogue);
            }

            currentOrderIndex = restoredIndex;
            currentOrder = restoredIndex >= 0 ? orders[restoredIndex] : savedOrder;
            ShowCurrentOrderDialogue();
            return true;
        }

        private void ShowNextOrder()
        {
            if (orders.Count == 0)
            {
                currentOrder = null;
                SetConversation(emptyMessage);
                return;
            }

            if (orders.Count == 1)
            {
                currentOrderIndex = 0;
            }
            else
            {
                int nextIndex = Random.Range(0, orders.Count);
                if (nextIndex == currentOrderIndex)
                {
                    nextIndex = (nextIndex + 1) % orders.Count;
                }

                currentOrderIndex = nextIndex;
            }

            currentOrder = orders[currentOrderIndex];
            ShowCurrentOrderDialogue();
        }

        private void ShowCurrentOrderDialogue()
        {
            if (currentOrder == null)
            {
                return;
            }

            SetConversation(currentOrder.orderDialogue);
            SetPersonImage(currentOrder.orderImageName);
        }

        private void SetConversation(string message)
        {
            if (conversationText != null)
            {
                conversationText.text = string.IsNullOrWhiteSpace(message) ? "..." : message;
            }
        }

        private void SetPersonImage(string imageName)
        {
            if (string.IsNullOrWhiteSpace(imageName))
            {
                return;
            }

            Sprite sprite = Resources.Load<Sprite>(imageName.Trim());
            if (sprite == null)
            {
                Debug.LogWarning($"Person image sprite not found in Resources: {imageName}");
                return;
            }

            if (personImage != null)
            {
                personImage.sprite = sprite;
                personImage.enabled = true;
            }

            if (personSpriteRenderer != null)
            {
                personSpriteRenderer.sprite = sprite;
                personSpriteRenderer.enabled = true;
            }
        }

        private void AutoBindMissingReferences()
        {
            if (hintButton == null)
            {
                hintButton = FindButton("HintButton");
            }

            if (refuseButton == null)
            {
                refuseButton = FindButton("RefuseButton");
            }

            if (toKitchenButton == null)
            {
                toKitchenButton = FindButton("ToKitchenButton");
            }

            GameObject person = GameObject.Find("Person");
            if (person != null)
            {
                if (personImage == null)
                {
                    personImage = person.GetComponent<Image>();
                }

                if (personSpriteRenderer == null)
                {
                    personSpriteRenderer = person.GetComponent<SpriteRenderer>();
                }
            }

            if (conversationText == null)
            {
                GameObject panel = GameObject.Find("conversationPanel") ?? GameObject.Find("conversaion panel") ?? GameObject.Find("conversation panel") ?? GameObject.Find("ConversationPanel") ?? GameObject.Find("Panel");
                if (panel != null)
                {
                    conversationText = panel.GetComponentInChildren<TMP_Text>(true);
                    if (conversationText == null)
                    {
                        conversationText = CreateConversationText(panel.transform);
                    }
                }
            }
        }

        private static TMP_Text CreateConversationText(Transform parent)
        {
            GameObject textObject = new GameObject("ConversationText", typeof(RectTransform), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(parent, false);

            RectTransform rectTransform = textObject.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.08f, 0.12f);
            rectTransform.anchorMax = new Vector2(0.92f, 0.88f);
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;

            TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
            text.text = string.Empty;
            text.fontSize = 28f;
            text.color = Color.black;
            text.alignment = TextAlignmentOptions.Center;
            text.enableWordWrapping = true;
            text.raycastTarget = false;
            return text;
        }

        private static Button FindButton(string name)
        {
            GameObject target = GameObject.Find(name);
            return target != null ? target.GetComponent<Button>() : null;
        }
    }
}
