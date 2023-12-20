using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;

namespace com.ceridwen.audio
{

    public abstract class EncapsulatedDictionary<K, I, O, D> where D: IDictionary<K, O>
    {

        #region Public Members

        public int Count {  get { return Items.Count; } }

        #endregion

        #region Private Members

        protected D Items { get; }
        protected ConcurrentDictionary<I,K> UIDLookup { get; } = new ConcurrentDictionary<I, K> ();
        private ObservableCollection<O> Collection { get; }
        private Func<O, K> KeySelector { get; }
        private Func<O, I> UIDSelector { get; }

        #endregion

        #region Constructors/Destructors

        public EncapsulatedDictionary(ObservableCollection<O> collection, Func<O,K> keySelector, Func<O, I> uidSelector, D items)
        {
            Collection = collection;
            KeySelector = keySelector;
            UIDSelector = uidSelector;
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

        private void Refresh(bool recursive = false)
        {
            Items.Clear();
            UIDLookup.Clear();
            foreach (var item in Collection)
            {
                Add(item, recursive);
            }

        }

        private void Add(O item, bool recursive = false)
        {
            try {
                if (item != null)
                {
                    if (KeySelector(item) != null)
                    {
                        if (UIDLookup.TryAdd(UIDSelector(item), KeySelector(item)))
                        {
                            Items?.Add(KeySelector(item), item);
                        }
                        else
                        {
                            if (!recursive)
                            {
                                Refresh(true);
                            }
                        }
                    }
                }
            } catch (Exception) { }
        }

    private void Remove(O item)
        {
            try
            {
                if (item != null)
                {
                    if (UIDLookup.TryRemove(UIDSelector(item), out var key))
                    {
                        if (key != null) { 
                            Items?.Remove(key);
                        } else
                        {
                            Refresh();
                            return; 
                        }
                    }
                    else
                    {
                        Refresh();
                        return;
                    }
                    if (KeySelector(item) != null)
                    {
                        Items?.Remove(KeySelector(item));
                    }
                }
            } catch (Exception) { }
        }

        #endregion
    }
}