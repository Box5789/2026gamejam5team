using KimbapGame.Kitchen;
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

            if (GameObject.Find("CompleteButton") == null)
            {
                bootstrap.BuildScene();
            }

            GameObject completeButton = GameObject.Find("CompleteButton");
            GameObject kitchenCanvas = GameObject.Find("KitchenCanvas");

            Assert.IsNotNull(completeButton);
            Assert.IsNotNull(kitchenCanvas);
            Assert.IsFalse(completeButton.transform.IsChildOf(kitchenCanvas.transform));
            Assert.IsTrue(completeButton.transform.IsChildOf(GameObject.Find("Complete Table").transform));
            Assert.IsNotNull(completeButton.GetComponent<Button>());
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
