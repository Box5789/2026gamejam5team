using KimbapGame.Kitchen;
using KimbapGame.Order;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace KimbapGame.Tests.Kitchen
{
    public sealed class KitchenSceneBootstrapTests
    {
        [TearDown]
        public void TearDown()
        {
            DestroyIfExists("KitchenSceneBootstrapTests");
            DestroyIfExists("KitchenController");
            DestroyIfExists("FixedKimbapMatRoot");
            DestroyIfExists("MovingTablesRoot");
            DestroyIfExists("KitchenRicePaintBridge");
            DestroyIfExists("KitchenTableNavigator");
            DestroyIfExists("KitchenCanvas");
            DestroyIfExists("EventSystem");
            DestroyIfExists("Main Camera");
        }

        [Test]
        public void BuildScene_CreatesCompleteButtonOnlyInsideCompleteTable()
        {
            GameObject bootstrapObject = new GameObject("KitchenSceneBootstrapTests");
            KitchenSceneBootstrap bootstrap = bootstrapObject.AddComponent<KitchenSceneBootstrap>();

            if (GameObject.Find("RollButton") == null)
            {
                bootstrap.BuildScene();
            }

            GameObject completeTable = GameObject.Find("Complete Table");
            GameObject rollButton = GameObject.Find("RollButton");
            GameObject completeButton = FindChild(completeTable.transform, "CompleteButton");
            GameObject submitButton = FindChild(completeTable.transform, "SubmitButton");
            GameObject nextButton = GameObject.Find("NextTableButton");
            GameObject kitchenCanvas = GameObject.Find("KitchenCanvas");

            Assert.IsNotNull(rollButton);
            Assert.IsNotNull(completeButton);
            Assert.IsNotNull(submitButton);
            Assert.IsNotNull(nextButton);
            Assert.IsNotNull(kitchenCanvas);
            Assert.IsTrue(nextButton.transform.IsChildOf(kitchenCanvas.transform));
            Assert.IsTrue(rollButton.transform.IsChildOf(completeTable.transform));
            Assert.IsFalse(completeButton.transform.IsChildOf(kitchenCanvas.transform));
            Assert.IsTrue(completeButton.transform.IsChildOf(completeTable.transform));
            Assert.IsTrue(submitButton.transform.IsChildOf(completeTable.transform));
            Assert.IsFalse(completeButton.activeSelf);
            Assert.IsFalse(submitButton.activeSelf);
            Assert.IsNotNull(rollButton.GetComponent<Button>());
            Assert.IsNotNull(completeButton.GetComponent<Button>());
            Assert.IsNotNull(submitButton.GetComponent<Button>());
            Assert.IsNotNull(submitButton.GetComponent<KitchenReturnNavigator>());
        }

        private static GameObject FindChild(Transform root, string objectName)
        {
            Transform[] children = root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < children.Length; i++)
            {
                if (children[i].name == objectName)
                {
                    return children[i].gameObject;
                }
            }

            return null;
        }

        private static void DestroyIfExists(string objectName)
        {
            GameObject target = GameObject.Find(objectName);
            if (target != null)
            {
                Object.DestroyImmediate(target);
            }
        }
    }
}
