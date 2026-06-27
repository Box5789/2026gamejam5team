using System;
using System.Collections;
using System.Collections.Generic;
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
            List<List<string>> rows = CsvTableParser.Parse(csv);
            List<SheetOrderData> orders = new List<SheetOrderData>();
            if (rows.Count <= 1)
            {
                return orders;
            }

            Dictionary<string, int> header = CsvTableParser.BuildHeader(rows[0]);
            for (int i = 1; i < rows.Count; i++)
            {
                List<string> row = rows[i];
                if (CsvTableParser.IsEmptyRow(row))
                {
                    continue;
                }

                SheetOrderData order = new SheetOrderData
                {
                    index = CsvTableParser.GetAny(row, header, "Index", "인덱스", "순서", "번호"),
                    customerName = CsvTableParser.GetAny(row, header, "이름", "손님", "손님이름", "Customer", "CustomerName"),
                    orderDialogue = CsvTableParser.GetAny(row, header, "주문-대사", "주문대사", "주문 대사", "OrderDialogue"),
                    orderImageName = CsvTableParser.GetAny(row, header, "주문-이미지", "주문-이미지이름", "주문이미지", "주문 이미지", "OrderImage", "OrderImageName"),
                    hintDialogue = CsvTableParser.GetAny(row, header, "힌트-대사", "힌트대사", "힌트 대사", "HintDialogue"),
                    hintImageName = CsvTableParser.GetAny(row, header, "힌트-이미지", "힌트-이미지이름", "힌트이미지", "힌트 이미지", "HintImage", "HintImageName"),
                    successDialogue = CsvTableParser.GetAny(row, header, "성공-대사", "성공대사", "성공 대사", "SuccessDialogue"),
                    successImageName = CsvTableParser.GetAny(row, header, "성공-이미지", "성공-이미지이름", "성공이미지", "성공 이미지", "SuccessImage", "SuccessImageName"),
                    failDialogue = CsvTableParser.GetAny(row, header, "실패-대사", "실패대사", "실패 대사", "FailDialogue"),
                    failImageName = CsvTableParser.GetAny(row, header, "실패-이미지", "실패-이미지이름", "실패이미지", "실패 이미지", "FailImage", "FailImageName"),
                    seaweedName = CsvTableParser.GetAny(row, header, "김-이름", "김이름", "김 이름", "김", "Seaweed", "SeaweedName"),
                    seaweedCount = ParseCount(CsvTableParser.GetAny(row, header, "김-개수", "김-갯수", "김개수", "김갯수", "SeaweedCount")),
                    riceName = CsvTableParser.GetAny(row, header, "밥-이름", "밥이름", "밥 이름", "밥", "Rice", "RiceName"),
                    riceCount = ParseCount(CsvTableParser.GetAny(row, header, "밥-개수", "밥-갯수", "밥개수", "밥갯수", "RiceCount")),
                    fillingName = CsvTableParser.GetAny(row, header, "속-이름", "속이름", "속 이름", "속재료", "속재료-이름", "Filling", "FillingName"),
                    fillingCount = ParseCount(CsvTableParser.GetAny(row, header, "속-개수", "속-갯수", "속개수", "속갯수", "속재료개수", "FillingCount"))
                };

                AddRepeatedIngredient(order.ingredients, order.seaweedName, order.seaweedCount, IngredientType.Seaweed);
                AddRepeatedIngredient(order.ingredients, order.riceName, order.riceCount, IngredientType.Rice);
                AddRepeatedIngredient(order.ingredients, order.fillingName, order.fillingCount, IngredientType.GenericFilling);
                orders.Add(order);
            }

            return orders;
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

    }
}
