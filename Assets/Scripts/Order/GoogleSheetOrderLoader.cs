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
                    index = GetAny(row, header, "Index", "인덱스", "순서", "번호"),
                    customerName = GetAny(row, header, "이름", "손님", "손님이름", "Customer", "CustomerName"),
                    orderDialogue = GetAny(row, header, "주문-대사", "주문대사", "주문 대사", "OrderDialogue"),
                    orderImageName = GetAny(row, header, "주문-이미지", "주문-이미지이름", "주문이미지", "주문 이미지", "OrderImage", "OrderImageName"),
                    hintDialogue = GetAny(row, header, "힌트-대사", "힌트대사", "힌트 대사", "HintDialogue"),
                    hintImageName = GetAny(row, header, "힌트-이미지", "힌트-이미지이름", "힌트이미지", "힌트 이미지", "HintImage", "HintImageName"),
                    successDialogue = GetAny(row, header, "성공-대사", "성공대사", "성공 대사", "SuccessDialogue"),
                    successImageName = GetAny(row, header, "성공-이미지", "성공-이미지이름", "성공이미지", "성공 이미지", "SuccessImage", "SuccessImageName"),
                    failDialogue = GetAny(row, header, "실패-대사", "실패대사", "실패 대사", "FailDialogue"),
                    failImageName = GetAny(row, header, "실패-이미지", "실패-이미지이름", "실패이미지", "실패 이미지", "FailImage", "FailImageName"),
                    seaweedName = GetAny(row, header, "김-이름", "김이름", "김 이름", "김", "Seaweed", "SeaweedName"),
                    seaweedCount = ParseCount(GetAny(row, header, "김-개수", "김-갯수", "김개수", "김갯수", "SeaweedCount")),
                    riceName = GetAny(row, header, "밥-이름", "밥이름", "밥 이름", "밥", "Rice", "RiceName"),
                    riceCount = ParseCount(GetAny(row, header, "밥-개수", "밥-갯수", "밥개수", "밥갯수", "RiceCount")),
                    fillingName = GetAny(row, header, "속-이름", "속이름", "속 이름", "속재료", "속재료-이름", "Filling", "FillingName"),
                    fillingCount = ParseCount(GetAny(row, header, "속-개수", "속-갯수", "속개수", "속갯수", "속재료개수", "FillingCount"))
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

        private static string GetAny(List<string> row, Dictionary<string, int> header, params string[] columnNames)
        {
            for (int i = 0; i < columnNames.Length; i++)
            {
                if (header.TryGetValue(Normalize(columnNames[i]), out int index) && index < row.Count)
                {
                    return row[index].Trim();
                }
            }

            return string.Empty;
        }

        private static string Normalize(string value)
        {
            return (value ?? string.Empty).Trim().Replace(" ", string.Empty).Replace("-", string.Empty).Replace("_", string.Empty).ToLowerInvariant();
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
