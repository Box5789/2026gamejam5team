using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

namespace KimbapGame.EditorTools
{
    public static class KitchenIngredientSheetDownloadMenu
    {
        private const string DownloadUrl = "https://docs.google.com/spreadsheets/d/13gq3ZV-LhXbPbYeEcljMLyYC_6GyXHLdkcgjHbalqcI/export?format=csv&gid=0";
        private const string OutputAssetPath = "Assets/StreamingAssets/Kitchen/ingredients.csv";

        [MenuItem("Tools/Kimbap/Download Ingredient Sheet")]
        private static void DownloadIngredientSheet()
        {
            UnityWebRequest request = UnityWebRequest.Get(DownloadUrl);
            UnityWebRequestAsyncOperation operation = request.SendWebRequest();
            operation.completed += _ => CompleteDownload(request);
        }

        private static void CompleteDownload(UnityWebRequest request)
        {
            try
            {
                if (request.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogWarning($"Ingredient sheet download failed. Existing local CSV was preserved. Error: {request.error}");
                    return;
                }

                string directory = Path.GetDirectoryName(OutputAssetPath);
                if (!string.IsNullOrEmpty(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                File.WriteAllText(OutputAssetPath, request.downloadHandler.text, new UTF8Encoding(false));
                AssetDatabase.ImportAsset(OutputAssetPath);
                Debug.Log($"Updated local kitchen ingredient data: {OutputAssetPath}");
            }
            finally
            {
                request.Dispose();
            }
        }
    }
}
