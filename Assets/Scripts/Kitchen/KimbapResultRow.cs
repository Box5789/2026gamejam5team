using System;
using System.Linq;
using KimbapGame.Data;

namespace KimbapGame.Kitchen
{
    public sealed class KimbapResultRow
    {
        public KimbapResultRow()
        {
        }


        public string sessionId;
        public DateTime timestampUtc;
        public int orderId;
        public string orderName;
        public string customerDialogue;
        public string tableSequence;
        public string seaweedItems;
        public string riceItems;
        public string fillingItems;
        public string allIngredients;

        public KimbapResultRow(
            string sessionId,
            DateTime timestampUtc,
            OrderData orderData,
            string tableSequence,
            PreparedKimbapData preparedKimbap)
        {
            this.sessionId = sessionId;
            this.timestampUtc = timestampUtc;
            orderId = orderData == null ? 0 : orderData.orderId;
            orderName = orderData == null ? string.Empty : orderData.orderName;
            customerDialogue = orderData == null ? string.Empty : orderData.customerDialogue;
            this.tableSequence = tableSequence ?? string.Empty;
            seaweedItems = JoinItems(preparedKimbap == null ? null : preparedKimbap.seaweeds);
            riceItems = JoinItems(preparedKimbap == null ? null : preparedKimbap.riceItems);
            fillingItems = JoinItems(preparedKimbap == null ? null : preparedKimbap.fillings);
            allIngredients = preparedKimbap == null ? string.Empty : string.Join("|", preparedKimbap.AllItems.Select(item => item.ToExportString()).ToArray());
        }

        private static string JoinItems(System.Collections.Generic.IEnumerable<PreparedKimbapItem> items)
        {
            return items == null ? string.Empty : string.Join("|", items.Select(item => item.ToExportString()).ToArray());
        }
    }
}
