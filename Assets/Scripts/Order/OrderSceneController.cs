using System.Collections;
using System.Collections.Generic;
using KimbapGame.Data;
using KimbapGame.Evaluation;
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
        private string kitchenSceneName = "kitchen";

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
        [SerializeField]
        private Button confirmButton;

        [Header("Sound")]
        [SerializeField]
        private string buttonSoundName = "Order/Sound/버튼";
        [SerializeField]
        private string successReactionSoundName = "Order/Sound/손님반응-성공";
        [SerializeField]
        private string failReactionSoundName = "Order/Sound/손님반응-실패";
        [SerializeField]
        private float soundVolume = 1f;

        [Header("Emotion Particles")]
        [SerializeField]
        private string successStarImageName = "Order/별_0";
        [SerializeField]
        private string successHeartImageName = "Order/하트_0";
        [SerializeField]
        private string failSweatImageName = "Order/땀방울_0";
        [SerializeField]
        private int successParticleCount = 12;
        [SerializeField]
        private int failParticleCount = 8;
        [SerializeField]
        private float emotionParticleLifetime = 2.4f;
        private readonly Vector2 emotionParticleUiOffset = Vector2.zero;
        private readonly Vector3 emotionParticleWorldOffset = Vector3.zero;

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
        private readonly List<GameObject> emotionParticleObjects = new List<GameObject>();
        private Coroutine emotionParticleRoutine;
        private AudioSource orderAudioSource;

        private void Awake()
        {
            loader = GetComponent<GoogleSheetOrderLoader>();
            loader.CsvUrl = sheetCsvUrl;
            AutoBindMissingReferences();
            EnsureAudioSource();
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

            if (confirmButton != null)
            {
                confirmButton.onClick.AddListener(ConfirmEvaluationResult);
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

            if (confirmButton != null)
            {
                confirmButton.onClick.RemoveListener(ConfirmEvaluationResult);
            }
        }

        private void Start()
        {
            SetEvaluationMode(false);
            ShowSavedOrderImageWhileLoading();
            SetConversation(loadingMessage);
            StartCoroutine(LoadOrders());
        }

        public void RefuseCurrentOrder()
        {
            PlayButtonSound();
            if (orders.Count == 0)
            {
                SetConversation(emptyMessage);
                return;
            }

            ShowNextOrder();
        }

        public void ShowHint()
        {
            PlayButtonSound();
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
                SetPersonImage(currentOrder.hintImageName, "Order/사람_힌트_10");
            }
        }

        public void ShowEvaluationResult(KimbapEvaluationResult result)
        {
            if (result == null)
            {
                return;
            }

            string responseDialogue = result.responseDialogue;
            if (string.IsNullOrWhiteSpace(responseDialogue))
            {
                responseDialogue = result.isSuccess ? "성공!" : "실패...";
            }

            SetConversation(responseDialogue);

            string fallbackImageName = result.isSuccess ? "Order/사람_성공_10" : "Order/사람_실패_10";
            SetPersonImage(result.responseImageName, fallbackImageName);
            PlayEmotionParticles(result.isSuccess);
            PlayCustomerReactionSound(result.isSuccess);

            SetEvaluationMode(true);
        }

        public void GoToKitchen()
        {
            PlayButtonSound();
            if (currentOrder == null)
            {
                return;
            }

            SharedOrderContext.ReturnSceneName = SceneManager.GetActiveScene().name;
            SharedOrderContext.SetCurrentOrder(currentOrder.ToOrderData(), currentOrder, GetOrderImageName(currentOrder));
            SceneManager.LoadScene(kitchenSceneName);
        }

        private void ShowSavedOrderImageWhileLoading()
        {
            SheetOrderData savedOrder = SharedOrderContext.CurrentSheetOrder;
            string savedImageName = SharedOrderContext.CurrentOrderImageName;
            if (!string.IsNullOrWhiteSpace(savedImageName))
            {
                SetPersonImage(savedImageName, savedOrder == null ? string.Empty : GetFallbackOrderImageName(savedOrder));
                return;
            }

            if (savedOrder == null)
            {
                return;
            }

            SetPersonImage(GetOrderImageName(savedOrder));
        }

        private static string GetOrderImageName(SheetOrderData order)
        {
            if (order != null && !string.IsNullOrWhiteSpace(order.orderImageName))
            {
                return order.orderImageName;
            }

            return GetFallbackOrderImageName(order);
        }

        private static string GetFallbackOrderImageName(SheetOrderData order)
        {
            int imageNumber = 10;
            if (order != null && !string.IsNullOrWhiteSpace(order.index))
            {
                int parsedNumber = 0;
                for (int i = 0; i < order.index.Length; i++)
                {
                    if (char.IsDigit(order.index[i]))
                    {
                        parsedNumber = (parsedNumber * 10) + (order.index[i] - '0');
                    }
                }

                if (parsedNumber > 0)
                {
                    int oldIndex = (parsedNumber - 1) % 10;
                    imageNumber = oldIndex == 0 ? 10 : oldIndex;
                    if (parsedNumber == 10)
                    {
                        imageNumber = 1;
                    }
                }
            }

            return $"Order/사람_기본_{imageNumber}";
        }

        private IEnumerator LoadOrders()
        {
            yield return loader.LoadOrders(
                loadedOrders =>
                {
                    orders.Clear();
                    orders.AddRange(loadedOrders);

                    if (!TryShowPendingEvaluationResult() && !TryRestoreCurrentOrder())
                    {
                        ShowNextOrder();
                    }
                },
                error =>
                {
                    Debug.LogWarning($"Order sheet load failed: {error}");
                    if (!TryShowPendingEvaluationResult())
                    {
                        SetConversation(errorMessage);
                    }
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
            ClearEmotionParticles();
            SharedOrderContext.ClearEvaluationResult();
            SetEvaluationMode(false);

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
            SetPersonImage(GetOrderImageName(currentOrder));
        }

        private bool TryShowPendingEvaluationResult()
        {
            if (!SharedOrderContext.HasPendingEvaluation)
            {
                return false;
            }

            SheetOrderData savedOrder = SharedOrderContext.CurrentSheetOrder;
            if (savedOrder != null)
            {
                int restoredIndex = orders.FindIndex(order => order.index == savedOrder.index);
                if (restoredIndex < 0)
                {
                    restoredIndex = orders.FindIndex(order => order.customerName == savedOrder.customerName && order.orderDialogue == savedOrder.orderDialogue);
                }

                currentOrderIndex = restoredIndex;
                currentOrder = restoredIndex >= 0 ? orders[restoredIndex] : savedOrder;
            }

            ShowEvaluationResult(SharedOrderContext.PendingEvaluationResult);
            return true;
        }

        private void ConfirmEvaluationResult()
        {
            PlayButtonSound();
            ClearEmotionParticles();
            SharedOrderContext.Clear();
            ShowNextOrder();
        }

        private void PlayButtonSound()
        {
            PlaySound(buttonSoundName);
        }

        private void PlayCustomerReactionSound(bool isSuccess)
        {
            PlaySound(isSuccess ? successReactionSoundName : failReactionSoundName);
        }

        private void PlaySound(string resourcePath)
        {
            if (string.IsNullOrWhiteSpace(resourcePath))
            {
                return;
            }

            EnsureAudioSource();
            AudioClip clip = Resources.Load<AudioClip>(resourcePath.Trim());
            if (clip == null)
            {
                Debug.LogWarning($"Order sound clip not found in Resources: {resourcePath}");
                return;
            }

            orderAudioSource.PlayOneShot(clip, soundVolume);
        }

        private void EnsureAudioSource()
        {
            if (orderAudioSource != null)
            {
                return;
            }

            orderAudioSource = GetComponent<AudioSource>();
            if (orderAudioSource == null)
            {
                orderAudioSource = gameObject.AddComponent<AudioSource>();
            }

            orderAudioSource.playOnAwake = false;
        }
        private void PlayEmotionParticles(bool isSuccess)
        {
            ClearEmotionParticles();

            Sprite[] sprites = isSuccess
                ? new[] { LoadPersonSprite(successStarImageName), LoadPersonSprite(successHeartImageName) }
                : new[] { LoadPersonSprite(failSweatImageName) };

            List<Sprite> usableSprites = new List<Sprite>();
            for (int i = 0; i < sprites.Length; i++)
            {
                if (sprites[i] != null)
                {
                    usableSprites.Add(sprites[i]);
                }
            }

            if (usableSprites.Count == 0)
            {
                Debug.LogWarning("Emotion particle sprites not found in Resources/Order.");
                return;
            }

            if (personImage != null)
            {
                emotionParticleRoutine = StartCoroutine(PlayUiEmotionParticles(usableSprites, isSuccess));
                return;
            }

            if (personSpriteRenderer != null)
            {
                emotionParticleRoutine = StartCoroutine(PlayWorldEmotionParticles(usableSprites, isSuccess));
            }
        }

        private IEnumerator PlayUiEmotionParticles(List<Sprite> sprites, bool isSuccess)
        {
            RectTransform personRect = personImage.rectTransform;
            int particleCount = isSuccess ? Mathf.Max(successParticleCount, 12) : Mathf.Max(failParticleCount, 8);
            float width = Mathf.Max(personRect.rect.width, 160f);
            float height = Mathf.Max(personRect.rect.height, 220f);

            for (int i = 0; i < particleCount; i++)
            {
                Sprite sprite = sprites[i % sprites.Count];
                GameObject particle = new GameObject("EmotionParticle", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
                particle.transform.SetParent(personRect, false);
                particle.transform.SetAsLastSibling();
                emotionParticleObjects.Add(particle);

                RectTransform rect = particle.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);

                Vector2 startOffset;
                if (isSuccess)
                {
                    float angle = (Mathf.PI * 2f * i / particleCount) + Random.Range(-0.28f, 0.28f);
                    float radiusX = width * Random.Range(0.32f, 0.48f);
                    float radiusY = height * Random.Range(0.2f, 0.38f);
                    startOffset = new Vector2(Mathf.Cos(angle) * radiusX, Mathf.Sin(angle) * radiusY);
                }
                else
                {
                    float lane = particleCount <= 1 ? 0f : Mathf.Lerp(-0.32f, 0.32f, i / (float)(particleCount - 1));
                    startOffset = new Vector2(width * lane + Random.Range(-18f, 18f), height * Random.Range(0.22f, 0.42f));
                }

                rect.anchoredPosition = emotionParticleUiOffset + startOffset;
                float size = isSuccess ? Random.Range(92f, 132f) : Random.Range(78f, 108f);
                rect.sizeDelta = new Vector2(size, size);
                rect.localRotation = Quaternion.Euler(0f, 0f, Random.Range(-18f, 18f));

                Image image = particle.GetComponent<Image>();
                image.sprite = sprite;
                image.preserveAspect = true;
                image.raycastTarget = false;

                CanvasGroup canvasGroup = particle.GetComponent<CanvasGroup>();
                canvasGroup.alpha = 0f;
            }

            yield return AnimateEmotionParticles(true, isSuccess);
        }

        private IEnumerator PlayWorldEmotionParticles(List<Sprite> sprites, bool isSuccess)
        {
            Bounds bounds = personSpriteRenderer.bounds;
            int particleCount = isSuccess ? Mathf.Max(successParticleCount, 12) : Mathf.Max(failParticleCount, 8);

            for (int i = 0; i < particleCount; i++)
            {
                Sprite sprite = sprites[i % sprites.Count];
                GameObject particle = new GameObject("EmotionParticle", typeof(SpriteRenderer));
                emotionParticleObjects.Add(particle);

                SpriteRenderer renderer = particle.GetComponent<SpriteRenderer>();
                renderer.sprite = sprite;
                renderer.sortingLayerID = personSpriteRenderer.sortingLayerID;
                renderer.sortingOrder = personSpriteRenderer.sortingOrder + 1;

                Vector3 offset;
                if (isSuccess)
                {
                    float angle = (Mathf.PI * 2f * i / particleCount) + Random.Range(-0.28f, 0.28f);
                    offset = new Vector3(
                        Mathf.Cos(angle) * bounds.size.x * Random.Range(0.32f, 0.48f),
                        Mathf.Sin(angle) * bounds.size.y * Random.Range(0.2f, 0.38f),
                        0f);
                }
                else
                {
                    float lane = particleCount <= 1 ? 0f : Mathf.Lerp(-0.32f, 0.32f, i / (float)(particleCount - 1));
                    offset = new Vector3(bounds.size.x * lane + Random.Range(-0.08f, 0.08f), bounds.size.y * Random.Range(0.22f, 0.42f), 0f);
                }

                particle.transform.position = bounds.center + emotionParticleWorldOffset + offset;
                float scale = isSuccess ? Random.Range(0.36f, 0.52f) : Random.Range(0.3f, 0.44f);
                particle.transform.localScale = Vector3.one * scale;
                particle.transform.localRotation = Quaternion.Euler(0f, 0f, Random.Range(-18f, 18f));
            }

            yield return AnimateEmotionParticles(false, isSuccess);
        }

        private IEnumerator AnimateEmotionParticles(bool isUi, bool isSuccess)
        {
            List<Vector3> startPositions = new List<Vector3>();
            List<Vector3> baseScales = new List<Vector3>();
            List<float> delays = new List<float>();
            List<float> sparkleSeeds = new List<float>();
            for (int i = 0; i < emotionParticleObjects.Count; i++)
            {
                GameObject particle = emotionParticleObjects[i];
                if (particle == null)
                {
                    continue;
                }

                Vector3 start = isUi
                    ? (Vector3)particle.GetComponent<RectTransform>().anchoredPosition
                    : particle.transform.position;

                startPositions.Add(start);
                baseScales.Add(particle.transform.localScale);
                delays.Add(isSuccess ? i * 0.03f : i * 0.05f);
                sparkleSeeds.Add(Random.Range(0f, Mathf.PI * 2f));
            }

            float lifetime = Mathf.Max(emotionParticleLifetime, 2.2f);
            float totalTime = lifetime + 0.8f;
            float elapsed = 0f;
            while (elapsed < totalTime)
            {
                elapsed += Time.deltaTime;
                for (int i = 0; i < emotionParticleObjects.Count; i++)
                {
                    GameObject particle = emotionParticleObjects[i];
                    if (particle == null || i >= startPositions.Count)
                    {
                        continue;
                    }

                    float t = Mathf.Clamp01((elapsed - delays[i]) / lifetime);
                    float fadeIn = Mathf.Clamp01(t / 0.18f);
                    float fadeOut = Mathf.Clamp01((1f - t) / 0.22f);

                    if (isSuccess)
                    {
                        float sparkle = (Mathf.Sin((elapsed * 7.5f) + sparkleSeeds[i]) + 1f) * 0.5f;
                        float breathe = 0.88f + (sparkle * 0.34f);
                        float alpha = Mathf.Min(fadeIn, fadeOut) * Mathf.Lerp(0.55f, 1f, sparkle);
                        Vector3 twinkleOffset = isUi
                            ? new Vector3(Mathf.Sin((elapsed * 3.2f) + sparkleSeeds[i]) * 8f, Mathf.Cos((elapsed * 2.7f) + sparkleSeeds[i]) * 8f, 0f)
                            : new Vector3(Mathf.Sin((elapsed * 3.2f) + sparkleSeeds[i]) * 0.06f, Mathf.Cos((elapsed * 2.7f) + sparkleSeeds[i]) * 0.06f, 0f);

                        if (isUi)
                        {
                            RectTransform rect = particle.GetComponent<RectTransform>();
                            Vector3 position = startPositions[i] + twinkleOffset;
                            rect.anchoredPosition = new Vector2(position.x, position.y);
                            rect.localScale = baseScales[i] * breathe;
                            rect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin((elapsed * 4f) + sparkleSeeds[i]) * 14f);

                            CanvasGroup canvasGroup = particle.GetComponent<CanvasGroup>();
                            canvasGroup.alpha = alpha;
                        }
                        else
                        {
                            particle.transform.position = startPositions[i] + twinkleOffset;
                            particle.transform.localScale = baseScales[i] * breathe;
                            particle.transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin((elapsed * 4f) + sparkleSeeds[i]) * 14f);

                            SpriteRenderer renderer = particle.GetComponent<SpriteRenderer>();
                            Color color = renderer.color;
                            color.a = alpha;
                            renderer.color = color;
                        }
                    }
                    else
                    {
                        float dropDistance = isUi ? 150f : 0.8f;
                        float wobble = isUi ? Mathf.Sin((elapsed * 5.2f) + sparkleSeeds[i]) * 10f : Mathf.Sin((elapsed * 5.2f) + sparkleSeeds[i]) * 0.06f;
                        Vector3 dropOffset = new Vector3(wobble, -dropDistance * t, 0f);
                        float alpha = Mathf.Min(fadeIn, fadeOut) * 0.95f;

                        if (isUi)
                        {
                            RectTransform rect = particle.GetComponent<RectTransform>();
                            Vector3 position = startPositions[i] + dropOffset;
                            rect.anchoredPosition = new Vector2(position.x, position.y);
                            rect.localScale = baseScales[i];
                            rect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin((elapsed * 4f) + sparkleSeeds[i]) * 8f);

                            CanvasGroup canvasGroup = particle.GetComponent<CanvasGroup>();
                            canvasGroup.alpha = alpha;
                        }
                        else
                        {
                            particle.transform.position = startPositions[i] + dropOffset;
                            particle.transform.localScale = baseScales[i];
                            particle.transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin((elapsed * 4f) + sparkleSeeds[i]) * 8f);

                            SpriteRenderer renderer = particle.GetComponent<SpriteRenderer>();
                            Color color = renderer.color;
                            color.a = alpha;
                            renderer.color = color;
                        }
                    }
                }

                yield return null;
            }

            emotionParticleRoutine = null;
            ClearEmotionParticles();
        }
        private void ClearEmotionParticles()
        {
            if (emotionParticleRoutine != null)
            {
                StopCoroutine(emotionParticleRoutine);
                emotionParticleRoutine = null;
            }

            for (int i = 0; i < emotionParticleObjects.Count; i++)
            {
                if (emotionParticleObjects[i] != null)
                {
                    Destroy(emotionParticleObjects[i]);
                }
            }

            emotionParticleObjects.Clear();
        }
        private void SetEvaluationMode(bool enabled)
        {
            if (hintButton != null)
            {
                hintButton.gameObject.SetActive(!enabled);
            }

            if (refuseButton != null)
            {
                refuseButton.gameObject.SetActive(!enabled);
            }

            if (toKitchenButton != null)
            {
                toKitchenButton.gameObject.SetActive(!enabled);
            }

            if (confirmButton != null)
            {
                confirmButton.gameObject.SetActive(enabled);
            }
        }

        private void SetConversation(string message)
        {
            if (conversationText != null)
            {
                conversationText.text = string.IsNullOrWhiteSpace(message) ? "..." : message;
            }
        }

        private void SetPersonImage(string imageName, string fallbackImageName = "")
        {
            string targetImageName = string.IsNullOrWhiteSpace(imageName) ? fallbackImageName : imageName;
            if (string.IsNullOrWhiteSpace(targetImageName))
            {
                return;
            }

            Sprite sprite = LoadPersonSprite(targetImageName);
            if (sprite == null)
            {
                Debug.LogWarning($"Person image sprite not found in Resources: {targetImageName}");
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

        private static Sprite LoadPersonSprite(string imageName)
        {
            string normalizedName = imageName.Trim();
            normalizedName = NormalizeLegacyPersonImageName(normalizedName);
            Sprite sprite = LoadSpriteByPathOrSubSprite(normalizedName);
            if (sprite != null)
            {
                return sprite;
            }

            if (!normalizedName.StartsWith("Order/", System.StringComparison.OrdinalIgnoreCase))
            {
                sprite = LoadSpriteByPathOrSubSprite("Order/" + normalizedName);
            }

            return sprite;
        }

        private static string NormalizeLegacyPersonImageName(string imageName)
        {
            string normalizedName = imageName.Replace("\\", "/");
            if (normalizedName.EndsWith("_0", System.StringComparison.Ordinal))
            {
                string prefix = normalizedName.Substring(0, normalizedName.Length - 2);
                if (prefix.EndsWith("사람_기본", System.StringComparison.Ordinal)
                    || prefix.EndsWith("사람_힌트", System.StringComparison.Ordinal)
                    || prefix.EndsWith("사람_성공", System.StringComparison.Ordinal)
                    || prefix.EndsWith("사람_실패", System.StringComparison.Ordinal))
                {
                    return prefix + "_10";
                }
            }

            return normalizedName;
        }

        private static Sprite LoadSpriteByPathOrSubSprite(string resourcePath)
        {
            Sprite sprite = Resources.Load<Sprite>(resourcePath);
            if (sprite != null)
            {
                return sprite;
            }

            string folder = string.Empty;
            string spriteName = resourcePath;
            int slashIndex = resourcePath.LastIndexOf('/');
            if (slashIndex >= 0)
            {
                folder = resourcePath.Substring(0, slashIndex);
                spriteName = resourcePath.Substring(slashIndex + 1);
            }

            Sprite[] sprites = Resources.LoadAll<Sprite>(folder);
            for (int i = 0; i < sprites.Length; i++)
            {
                if (sprites[i] != null && sprites[i].name == spriteName)
                {
                    return sprites[i];
                }
            }

            string firstSliceName = spriteName + "_0";
            for (int i = 0; i < sprites.Length; i++)
            {
                if (sprites[i] != null && sprites[i].name == firstSliceName)
                {
                    return sprites[i];
                }
            }

            return null;
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

            if (confirmButton == null)
            {
                confirmButton = FindButton("ConfirmButton");
            }

            if (confirmButton == null)
            {
                confirmButton = CreateConfirmButton();
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
            if (target != null && target.TryGetComponent(out Button activeButton))
            {
                return activeButton;
            }

            Button[] buttons = Resources.FindObjectsOfTypeAll<Button>();
            for (int i = 0; i < buttons.Length; i++)
            {
                Button button = buttons[i];
                if (button != null && button.name == name && button.gameObject.scene.IsValid())
                {
                    return button;
                }
            }

            return null;
        }

        private static Button CreateConfirmButton()
        {
            Canvas canvas = FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                return null;
            }

            GameObject buttonObject = new GameObject("ConfirmButton", typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(canvas.transform, false);

            RectTransform rectTransform = buttonObject.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.5f, 0f);
            rectTransform.anchorMax = new Vector2(0.5f, 0f);
            rectTransform.pivot = new Vector2(0.5f, 0f);
            rectTransform.anchoredPosition = new Vector2(0f, 60f);
            rectTransform.sizeDelta = new Vector2(220f, 72f);

            Image image = buttonObject.GetComponent<Image>();
            image.color = new Color(1f, 0.93f, 0.7f, 1f);

            GameObject textObject = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(buttonObject.transform, false);

            RectTransform textRect = textObject.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
            text.text = "확인";
            text.fontSize = 30f;
            text.color = Color.black;
            text.alignment = TextAlignmentOptions.Center;
            text.raycastTarget = false;

            return buttonObject.GetComponent<Button>();
        }
    }
}
