using KimbapGame.Data;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace KimbapGame.Order
{
    public class KitchenReturnNavigator : MonoBehaviour
    {
        [SerializeField]
        private string fallbackOrderSceneName = "order";
        [SerializeField]
        private Button returnButton;

        private void Awake()
        {
            if (returnButton == null)
            {
                returnButton = GetComponent<Button>();
            }
        }

        private void OnEnable()
        {
            if (returnButton != null)
            {
                returnButton.onClick.AddListener(ReturnToOrderScene);
            }
        }

        private void OnDisable()
        {
            if (returnButton != null)
            {
                returnButton.onClick.RemoveListener(ReturnToOrderScene);
            }
        }

        public void ReturnToOrderScene()
        {
            string sceneName = string.IsNullOrWhiteSpace(SharedOrderContext.ReturnSceneName)
                ? fallbackOrderSceneName
                : SharedOrderContext.ReturnSceneName;
            SceneManager.LoadScene(sceneName);
        }
    }
}
