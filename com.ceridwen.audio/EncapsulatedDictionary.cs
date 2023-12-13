using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;

namespace com.ceridwen.audio
{

    public abstract class EncapsulatedDictionary<K, O, D> where D: IDictionary<K, O>
    {

        #region Public Members

        public int Count {  get { return Items.Count; } }

        #endregion

        #region Private Members

        protected D Items { get; }
        private ObservableCollection<O> Collection { get; }
        private Func<O, K> KeySelector { get; }

        #endregion

        #region Constructors/Destructors

        public EncapsulatedDictionary(ObservableCollection<O> collection, Func<O,K> keySelector, D items)
        {
            Collection = collection;
            KeySelector = keySelector;
            Items = items;
            Refresh();
            Collection.CollectionChanged += CollectionChanged;
        }

        ~EncapsulatedDictionary()
        {
            Collection.CollectionChanged -= CollectionChanged;
        }

        #endregion

        #region Public Methods

        public O ElementAtOrDefault(int index)
        {
            return Items.ElementAtOrDefault(index).Value;
        }
        public bool TryFind(K index, out O value)
        {
            return Items.TryGetValue(index, out value);
        }

        #endregion

        #region Private Methods

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

        #endregion
    }
}