using System.Collections.Generic;
using KimbapGame.Data;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace KimbapGame.Kitchen
{
    public sealed class KitchenSceneBootstrap : MonoBehaviour
    {
        private const float TableSpacing = 9f;

        [SerializeField] private bool buildOnAwake = true;

        private void Awake()
        {
            if (buildOnAwake)
            {
                BuildScene();
            }
        }

        public void BuildScene()
        {
            Camera camera = EnsureCamera();
            EnsureEventSystem();

            KitchenController controller = new GameObject("KitchenController").AddComponent<KitchenController>();
            KitchenDropZone dropZone = BuildFixedKimbapMat();
            Transform movingTablesRoot = new GameObject("MovingTablesRoot").transform;

            BuildTable(movingTablesRoot, 0, "Seaweed Table", BuildSeaweedDefinitions(), controller, dropZone, camera);
            BuildTable(movingTablesRoot, 1, "Rice Table", BuildRiceDefinitions(), controller, dropZone, camera);
            BuildTable(movingTablesRoot, 2, "Filling Table", BuildFillingDefinitions(), controller, dropZone, camera);
            BuildCompleteTable(movingTablesRoot, 3, controller, camera);

            KitchenRicePaintBridge riceBridge = new GameObject("KitchenRicePaintBridge").AddComponent<KitchenRicePaintBridge>();
            riceBridge.Configure(null, null, camera);
            controller.ConfigureSceneReferences(dropZone, riceBridge);

            Button nextButton = BuildButton(
                "NextTableButton",
                ">",
                new Vector2(1f, 0.5f),
                new Vector2(-72f, 0f),
                new Vector2(84f, 84f));

            KitchenTableNavigator navigator = new GameObject("KitchenTableNavigator").AddComponent<KitchenTableNavigator>();
            navigator.Configure(movingTablesRoot, nextButton, TableSpacing, 4);
            navigator.TableChanged += index =>
            {
                if (index == 3)
                {
                    controller.CompleteAndSave();
                }
            };
        }

        private static Camera EnsureCamera()
        {
            Camera camera = Camera.main;
            if (camera == null)
            {
                GameObject cameraObject = new GameObject("Main Camera");
                camera = cameraObject.AddComponent<Camera>();
                camera.tag = "MainCamera";
            }

            camera.orthographic = true;
            camera.orthographicSize = 4.8f;
            camera.transform.position = new Vector3(0f, 0f, -10f);
            camera.backgroundColor = new Color(0.91f, 0.89f, 0.84f, 1f);
            return camera;
        }

        private static void EnsureEventSystem()
        {
            if (FindObjectOfType<EventSystem>() != null)
            {
                return;
            }

            GameObject eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<EventSystem>();
            eventSystem.AddComponent<StandaloneInputModule>();
        }

        private static KitchenDropZone BuildFixedKimbapMat()
        {
            Transform fixedRoot = new GameObject("FixedKimbapMatRoot").transform;
            KitchenPlaceholderFactory.CreateSpriteObject(
                "BambooMat",
                fixedRoot,
                new Vector3(0f, -0.75f, 0f),
                new Vector2(5.4f, 2.4f),
                new Color(0.76f, 0.67f, 0.46f, 1f),
                1);
            KitchenPlaceholderFactory.CreateSpriteObject(
                "DropGuide",
                fixedRoot,
                new Vector3(0f, -0.75f, -0.02f),
                new Vector2(4.8f, 1.8f),
                new Color(0.95f, 0.88f, 0.68f, 0.9f),
                2);
            KitchenPlaceholderFactory.CreateLabel("Kimbap Mat / Drop Area", fixedRoot, new Vector3(0f, -2.25f, -0.1f));

            GameObject dropObject = new GameObject("KitchenDropZone");
            dropObject.transform.SetParent(fixedRoot, false);
            dropObject.transform.localPosition = new Vector3(0f, -0.75f, -0.2f);
            return dropObject.AddComponent<KitchenDropZone>();
        }

        private static void BuildTable(
            Transform movingTablesRoot,
            int index,
            string title,
            IReadOnlyList<KitchenIngredientDefinition> definitions,
            KitchenController controller,
            KitchenDropZone dropZone,
            Camera camera)
        {
            Transform tableRoot = new GameObject(title).transform;
            tableRoot.SetParent(movingTablesRoot, false);
            tableRoot.localPosition = new Vector3(TableSpacing * index, 0f, 1f);

            KitchenPlaceholderFactory.CreateSpriteObject(
                $"{title}_Background",
                tableRoot,
                new Vector3(0f, 0f, 0.3f),
                new Vector2(7.6f, 6.6f),
                new Color(0.96f, 0.95f, 0.91f, 1f),
                -5);
            KitchenPlaceholderFactory.CreateLabel(title, tableRoot, new Vector3(0f, 2.75f, -0.1f), 52, 0.09f);

            float startX = -2.8f;
            for (int i = 0; i < definitions.Count; i++)
            {
                Vector3 position = new Vector3(startX + (i * 1.4f), 1.85f, -0.2f);
                CreateIngredientSource(tableRoot, definitions[i], position, controller, dropZone, camera);
            }
        }

        private static void BuildCompleteTable(Transform movingTablesRoot, int index, KitchenController controller, Camera camera)
        {
            Transform tableRoot = new GameObject("Complete Table").transform;
            tableRoot.SetParent(movingTablesRoot, false);
            tableRoot.localPosition = new Vector3(TableSpacing * index, 0f, 1f);

            KitchenPlaceholderFactory.CreateSpriteObject(
                "Complete Table_Background",
                tableRoot,
                new Vector3(0f, 0f, 0.3f),
                new Vector2(7.6f, 6.6f),
                new Color(0.94f, 0.96f, 0.93f, 1f),
                -5);
            KitchenPlaceholderFactory.CreateLabel("Complete Table", tableRoot, new Vector3(0f, 2.75f, -0.1f), 52, 0.09f);
            KitchenPlaceholderFactory.CreateLabel("Completed / Saved XLSX", tableRoot, new Vector3(0f, 1.25f, -0.1f), 44, 0.075f);
            BuildCompleteTableButton(tableRoot, controller, camera);
        }

        private static void BuildCompleteTableButton(Transform parent, KitchenController controller, Camera camera)
        {
            GameObject canvasObject = new GameObject("CompleteTableCanvas");
            canvasObject.transform.SetParent(parent, false);
            canvasObject.transform.localPosition = new Vector3(0f, -1.35f, -0.2f);

            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = camera;
            canvas.sortingOrder = 60;
            canvasObject.AddComponent<GraphicRaycaster>();

            RectTransform canvasRect = canvasObject.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(220f, 80f);
            canvasRect.localScale = new Vector3(0.01f, 0.01f, 0.01f);

            GameObject buttonObject = new GameObject("CompleteButton");
            buttonObject.transform.SetParent(canvasObject.transform, false);
            RectTransform buttonRect = buttonObject.AddComponent<RectTransform>();
            buttonRect.anchorMin = new Vector2(0.5f, 0.5f);
            buttonRect.anchorMax = new Vector2(0.5f, 0.5f);
            buttonRect.pivot = new Vector2(0.5f, 0.5f);
            buttonRect.anchoredPosition = Vector2.zero;
            buttonRect.sizeDelta = new Vector2(180f, 56f);

            Image image = buttonObject.AddComponent<Image>();
            image.color = new Color(0.15f, 0.7f, 0.68f, 1f);

            Button button = buttonObject.AddComponent<Button>();
            button.onClick.AddListener(() => controller.CompleteAndSave());

            GameObject textObject = new GameObject("Text");
            textObject.transform.SetParent(buttonObject.transform, false);
            RectTransform textRect = textObject.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            Text label = textObject.AddComponent<Text>();
            label.text = "Complete";
            label.alignment = TextAnchor.MiddleCenter;
            label.color = Color.white;
            label.fontSize = 28;
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (label.font == null)
            {
                label.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            }
        }

        private static void CreateIngredientSource(
            Transform parent,
            KitchenIngredientDefinition definition,
            Vector3 position,
            KitchenController controller,
            KitchenDropZone dropZone,
            Camera camera)
        {
            GameObject source = new GameObject($"{definition.DisplayName}_Source");
            source.transform.SetParent(parent, false);
            source.transform.localPosition = position;

            KitchenPlaceholderFactory.CreateSpriteObject("Bowl", source.transform, Vector3.zero, new Vector2(0.85f, 0.5f), new Color(0.55f, 0.55f, 0.58f, 1f), 4);
            KitchenPlaceholderFactory.CreateSpriteObject("Ingredient", source.transform, new Vector3(0f, 0.18f, -0.05f), new Vector2(0.62f, 0.26f), definition.PlaceholderColor, 5);
            KitchenPlaceholderFactory.CreateLabel(definition.DisplayName, source.transform, new Vector3(0f, -0.55f, -0.1f), 34, 0.055f);
            source.AddComponent<BoxCollider2D>().size = new Vector2(1.05f, 0.8f);

            KitchenIngredientSource ingredientSource = source.AddComponent<KitchenIngredientSource>();
            Vector2 dragSize = definition.Category == KitchenIngredientCategory.Seaweed
                ? new Vector2(2.4f, 1.7f)
                : new Vector2(2.8f, 0.28f);
            ingredientSource.Configure(definition, controller, dropZone, camera, dragSize);
        }

        private static Button BuildButton(string name, string text, Vector2 anchor, Vector2 anchoredPosition, Vector2 size)
        {
            Canvas canvas = FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                GameObject canvasObject = new GameObject("KitchenCanvas");
                canvas = canvasObject.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;

                CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1280f, 720f);
                scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                scaler.matchWidthOrHeight = 0.5f;

                canvasObject.AddComponent<GraphicRaycaster>();
            }

            GameObject buttonObject = new GameObject(name);
            buttonObject.transform.SetParent(canvas.transform, false);
            RectTransform rectTransform = buttonObject.AddComponent<RectTransform>();
            rectTransform.anchorMin = anchor;
            rectTransform.anchorMax = anchor;
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = anchoredPosition;
            rectTransform.sizeDelta = size;

            Image image = buttonObject.AddComponent<Image>();
            image.color = new Color(0.15f, 0.7f, 0.68f, 1f);
            Button button = buttonObject.AddComponent<Button>();

            GameObject textObject = new GameObject("Text");
            textObject.transform.SetParent(buttonObject.transform, false);
            RectTransform textRect = textObject.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            Text label = textObject.AddComponent<Text>();
            label.text = text;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = Color.white;
            label.fontSize = 28;
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (label.font == null)
            {
                label.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            }

            return button;
        }

        private static IReadOnlyList<KitchenIngredientDefinition> BuildSeaweedDefinitions()
        {
            return new[]
            {
                new KitchenIngredientDefinition("plain-seaweed", "Plain Seaweed", IngredientType.Seaweed, KitchenIngredientCategory.Seaweed, new Color(0.08f, 0.16f, 0.11f, 1f)),
                new KitchenIngredientDefinition("roasted-seaweed", "Roasted Seaweed", IngredientType.Seaweed, KitchenIngredientCategory.Seaweed, new Color(0.12f, 0.2f, 0.13f, 1f)),
                new KitchenIngredientDefinition("salted-seaweed", "Salted Seaweed", IngredientType.Seaweed, KitchenIngredientCategory.Seaweed, new Color(0.18f, 0.24f, 0.16f, 1f)),
                new KitchenIngredientDefinition("sesame-seaweed", "Sesame Seaweed", IngredientType.Seaweed, KitchenIngredientCategory.Seaweed, new Color(0.1f, 0.18f, 0.1f, 1f)),
                new KitchenIngredientDefinition("thick-seaweed", "Thick Seaweed", IngredientType.Seaweed, KitchenIngredientCategory.Seaweed, new Color(0.05f, 0.11f, 0.08f, 1f))
            };
        }

        private static IReadOnlyList<KitchenIngredientDefinition> BuildRiceDefinitions()
        {
            return new[]
            {
                new KitchenIngredientDefinition("white-rice", "White Rice", IngredientType.Rice, KitchenIngredientCategory.Rice, new Color(1f, 0.97f, 0.86f, 1f), "white-rice"),
                new KitchenIngredientDefinition("seasoned-rice", "Seasoned Rice", IngredientType.Rice, KitchenIngredientCategory.Rice, new Color(0.98f, 0.86f, 0.62f, 1f), "seasoned-rice"),
                new KitchenIngredientDefinition("brown-rice", "Brown Rice", IngredientType.Rice, KitchenIngredientCategory.Rice, new Color(0.75f, 0.58f, 0.38f, 1f), "brown-rice"),
                new KitchenIngredientDefinition("black-rice", "Black Rice", IngredientType.Rice, KitchenIngredientCategory.Rice, new Color(0.35f, 0.29f, 0.36f, 1f), "black-rice"),
                new KitchenIngredientDefinition("spicy-rice", "Spicy Rice", IngredientType.Rice, KitchenIngredientCategory.Rice, new Color(0.95f, 0.5f, 0.35f, 1f), "spicy-rice")
            };
        }

        private static IReadOnlyList<KitchenIngredientDefinition> BuildFillingDefinitions()
        {
            return new[]
            {
                new KitchenIngredientDefinition("ham", "Ham", IngredientType.Ham, KitchenIngredientCategory.Filling, new Color(0.95f, 0.44f, 0.42f, 1f)),
                new KitchenIngredientDefinition("egg", "Egg", IngredientType.Egg, KitchenIngredientCategory.Filling, new Color(1f, 0.88f, 0.28f, 1f)),
                new KitchenIngredientDefinition("carrot", "Carrot", IngredientType.Carrot, KitchenIngredientCategory.Filling, new Color(1f, 0.45f, 0.12f, 1f)),
                new KitchenIngredientDefinition("spinach", "Spinach", IngredientType.Spinach, KitchenIngredientCategory.Filling, new Color(0.22f, 0.65f, 0.24f, 1f)),
                new KitchenIngredientDefinition("pickled-radish", "Pickled Radish", IngredientType.PickledRadish, KitchenIngredientCategory.Filling, new Color(1f, 0.82f, 0.18f, 1f))
            };
        }
    }
}
