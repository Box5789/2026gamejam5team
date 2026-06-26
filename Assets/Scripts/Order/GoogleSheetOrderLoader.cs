using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using KimbapGame.Data;
using UnityEngine;
using UnityEngine.Networking;

namespace KimbapGame.Order
{
    public class GoogleSheetOrderLoader : MonoBehaviour
    {
        [SerializeField]
        private string csvUrl = "https://docs.google.com/spreadsheets/d/13gq3ZV-LhXbPbYeEcljMLyYC_6GyXHLdkcgjHbalqcI/export?format=csv&gid=650686695";

        public string CsvUrl
        {
            get => csvUrl;
            set => csvUrl = value;
        }

        public IEnumerator LoadOrders(Action<List<SheetOrderData>> onLoaded, Action<string> onFailed)
        {
            UnityWebRequest request = UnityWebRequest.Get(csvUrl);
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                string error = request.error;
                request.Dispose();
                onFailed?.Invoke(error);
                yield break;
            }

            string csv = request.downloadHandler.text;
            request.Dispose();

            try
            {
                List<SheetOrderData> orders = Parse(csv);
                onLoaded?.Invoke(orders);
            }
            catch (Exception exception)
            {
                onFailed?.Invoke(exception.Message);
            }
        }

        public List<SheetOrderData> Parse(string csv)
        {
            List<List<string>> rows = ParseCsv(csv);
            List<SheetOrderData> orders = new List<SheetOrderData>();
            if (rows.Count <= 1)
            {
                return orders;
            }

            Dictionary<string, int> header = BuildHeader(rows[0]);
            for (int i = 1; i < rows.Count; i++)
            {
                List<string> row = rows[i];
                if (IsEmptyRow(row))
                {
                    continue;
                }

                SheetOrderData order = new SheetOrderData
                {
                    index = Get(row, header, "Index"),
                    customerName = Get(row, header, "이름"),
                    orderDialogue = Get(row, header, "주문-대사"),
                    orderImageName = Get(row, header, "주문-이미지"),
                    hintDialogue = Get(row, header, "힌트-대사"),
                    hintImageName = Get(row, header, "힌트-이미지"),
                    successDialogue = Get(row, header, "성공-대사"),
                    successImageName = Get(row, header, "성공-이미지"),
                    failDialogue = Get(row, header, "실패-대사"),
                    failImageName = Get(row, header, "실패-이미지"),
                    seaweedName = Get(row, header, "김-이름"),
                    seaweedCount = ParseCount(Get(row, header, "김-갯수")),
                    riceName = Get(row, header, "밥-이름"),
                    riceCount = ParseCount(Get(row, header, "밥-갯수")),
                    fillingName = Get(row, header, "속-이름"),
                    fillingCount = ParseCount(Get(row, header, "속-갯수"))
                };

                AddRepeatedIngredient(order.ingredients, order.seaweedName, order.seaweedCount, IngredientType.Seaweed);
                AddRepeatedIngredient(order.ingredients, order.riceName, order.riceCount, IngredientType.Rice);
                AddRepeatedIngredient(order.ingredients, order.fillingName, order.fillingCount, null);
                orders.Add(order);
            }

            return orders;
        }

        private static Dictionary<string, int> BuildHeader(List<string> row)
        {
            Dictionary<string, int> header = new Dictionary<string, int>();
            for (int i = 0; i < row.Count; i++)
            {
                string key = Normalize(row[i]);
                if (!header.ContainsKey(key))
                {
                    header.Add(key, i);
                }
            }

            return header;
        }

        private static string Get(List<string> row, Dictionary<string, int> header, string columnName)
        {
            return header.TryGetValue(Normalize(columnName), out int index) && index < row.Count
                ? row[index].Trim()
                : string.Empty;
        }

        private static string Normalize(string value)
        {
            return (value ?? string.Empty).Trim().Replace(" ", string.Empty).ToLowerInvariant();
        }

        private static bool IsEmptyRow(List<string> row)
        {
            for (int i = 0; i < row.Count; i++)
            {
                if (!string.IsNullOrWhiteSpace(row[i]))
                {
                    return false;
                }
            }

            return true;
        }

        private static int ParseCount(string value)
        {
            return int.TryParse(value, out int count) ? Mathf.Max(0, count) : 0;
        }

        private static void AddRepeatedIngredient(List<IngredientType> ingredients, string ingredientName, int count, IngredientType? fallback)
        {
            if (count <= 0)
            {
                return;
            }

            IngredientType ingredient = IngredientNameMapper.ToIngredientType(ingredientName, fallback);
            for (int i = 0; i < count; i++)
            {
                ingredients.Add(ingredient);
            }
        }

        private static List<List<string>> ParseCsv(string csv)
        {
            List<List<string>> rows = new List<List<string>>();
            List<string> row = new List<string>();
            StringBuilder field = new StringBuilder();
            bool inQuotes = false;

            for (int i = 0; i < csv.Length; i++)
            {
                char c = csv[i];
                if (inQuotes)
                {
                    if (c == '"')
                    {
                        if (i + 1 < csv.Length && csv[i + 1] == '"')
                        {
                            field.Append('"');
                            i++;
                        }
                        else
                        {
                            inQuotes = false;
                        }
                    }
                    else
                    {
                        field.Append(c);
                    }
                }
                else
                {
                    if (c == '"')
                    {
                        inQuotes = true;
                    }
                    else if (c == ',')
                    {
                        row.Add(field.ToString());
                        field.Clear();
                    }
                    else if (c == 13)
                    {
                    }
                    else if (c == 10)
                    {
                        row.Add(field.ToString());
                        rows.Add(row);
                        row = new List<string>();
                        field.Clear();
                    }
                    else
                    {
                        field.Append(c);
                    }
                }
            }

            if (field.Length > 0 || row.Count > 0)
            {
                row.Add(field.ToString());
                rows.Add(row);
            }

            return rows;
        }
    }
}
