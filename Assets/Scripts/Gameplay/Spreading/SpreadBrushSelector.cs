using System;
using System.Collections.Generic;

namespace GameJam.Gameplay.Spreading
{
    public sealed class SpreadBrushSelector
    {
        public int SelectedIndex { get; private set; }

        public SpreadBrushSelector(int selectedIndex)
        {
            SelectedIndex = Math.Max(0, selectedIndex);
        }

        public void ClampToCount(int brushCount)
        {
            if (brushCount <= 0)
            {
                SelectedIndex = 0;
                return;
            }

            if (SelectedIndex >= brushCount)
            {
                SelectedIndex = brushCount - 1;
            }
        }

        public bool SelectIndex(int brushCount, int index)
        {
            if (index < 0 || index >= brushCount)
            {
                return false;
            }

            SelectedIndex = index;
            return true;
        }

        public bool SelectId(IReadOnlyList<SpreadBrushDefinition> brushes, string id)
        {
            if (brushes == null || string.IsNullOrWhiteSpace(id))
            {
                return false;
            }

            for (int i = 0; i < brushes.Count; i++)
            {
                SpreadBrushDefinition brush = brushes[i];
                if (brush != null && string.Equals(brush.Id, id, StringComparison.Ordinal))
                {
                    SelectedIndex = i;
                    return true;
                }
            }

            return false;
        }
    }
}
