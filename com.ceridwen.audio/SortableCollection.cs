using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace com.ceridwen.audio
{
    public class AudioDeviceNameComparer : IComparer<String>
    {
        public int Compare(String s1, String s2)
        {
            int n1, n2;
            char[] delim = { ' ', '-' };

            try
            {
                n1 = int.Parse(s1.Split(delim).First());
            }
            catch (Exception)
            {
                n1 = 0;
            }
            try
            {
                n2 = int.Parse(s2.Split(delim).First());
            }
            catch (Exception)
            {
                n2 = 0;
            }

            if (n1 == n2)
                return String.Compare(s1, s2);
            else if (n1 > n2)
                return 1;
            else
                return -1;
        }
    }
    public class SortableCollection<K, O>
    {
        public int Count {  get { return Items.Count; } }

        private SortedList<K, O> Items { get; }
        private ObservableCollection<O> Collection { get; }
        private Func<O, K> KeySelector { get; }
        public SortableCollection(ObservableCollection<O> collection, Func<O,K> keySelector, IComparer<K> comparer)
        {
            Collection = collection;
            KeySelector = keySelector;
            Items = new SortedList<K, O>(comparer);
            Refresh();
            Collection.CollectionChanged += CollectionChanged;
        }

        ~SortableCollection()
        {
            Collection.CollectionChanged -= CollectionChanged;
        }

        public int IndexOf(O item)
        {
            return Items.IndexOfValue(item);
        }

        public O ElementAtOrDefault(int index)
        {
            return Items.ElementAtOrDefault(index).Value;
        }

        private void CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    foreach (var o in e.NewItems) { Add((O)o); }
                    break;
                case NotifyCollectionChangedAction.Remove:
                    foreach (var o in e.OldItems) { Remove((O)o); }
                    break;
                case NotifyCollectionChangedAction.Reset:
                    Refresh();
                    break;
                case NotifyCollectionChangedAction.Replace:
                    foreach (var o in e.OldItems) { Remove((O)o); }
                    foreach (var o in e.NewItems) { Add((O)o); }
                    break;
                case NotifyCollectionChangedAction.Move:
                    break;
            }

        }

        private void Refresh()
        {
            Items.Clear();
            foreach (var item in Collection)
            {
                Add(item);
            }

        }

        private void Add(O item)
        {
            Items?.Add(KeySelector(item), item);
        }

        private void Remove(O item)
        {
            Items?.Remove(KeySelector(item));
        }
    }
}
