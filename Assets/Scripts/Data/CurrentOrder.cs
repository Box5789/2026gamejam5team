using System;

namespace KimbapGame.Data
{
    [Serializable]
    public class CurrentOrder
    {
        public OrderData order;
        public bool isCompleted;
    }
}
