using System;
using System.Collections.ObjectModel;

namespace wBeatSaberCamera.Utils
{
    public class ObservableSet<T> : ObservableCollection<T>
    {
        protected override void InsertItem(int index, T item)
        {
            if (Contains(item))
            {
                return;
            }

            base.InsertItem(index, item);
        }

        protected override void SetItem(int index, T item)
        {
            int i = IndexOf(item);
            if (i >= 0 && i != index)
            {
                throw new InvalidOperationException();
            }

            base.SetItem(index, item);
        }
    }
}