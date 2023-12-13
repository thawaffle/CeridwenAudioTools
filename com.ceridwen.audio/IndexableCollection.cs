using System;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace com.ceridwen.audio
{
    public class IndexableCollection<I, O>
    {
        private ConcurrentDictionary<I, O> Items { get; } = new ConcurrentDictionary<I, O>();
        private Func<O, I> Index { get; }
        private ObservableCollection<O> Collection { get; }
        public IndexableCollection(ObservableCollection<O> collection, Func<O, I> index)
        {
            Index = index;
            Collection = collection;
            Refresh();
            Collection.CollectionChanged += CollectionChanged;
        }

        ~IndexableCollection()
        {
            Collection.CollectionChanged -= CollectionChanged;
        }

        private void CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            switch(e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    foreach(var o in e.NewItems) { Add((O)o); }
                    break;
                case NotifyCollectionChangedAction.Remove:
                    foreach(var o in e.OldItems) {  Remove((O)o); }
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

        public bool TryFind(I index, out O value)
        {
            return Items.TryGetValue(index, out value);
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
            Items?.TryAdd(Index(item), item);
        }

        private void Remove(O item)
        {
            Items?.TryRemove(Index(item), out _);
        }
    }
}
