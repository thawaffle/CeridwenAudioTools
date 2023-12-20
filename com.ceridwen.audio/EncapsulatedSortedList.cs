using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;


namespace com.ceridwen.audio
{
    public class EncapsulatedSortedList<K, I, O> : EncapsulatedDictionary<K, I, O, SortedList<K, O>> 
    {
        #region Constructors/Destructors

        public EncapsulatedSortedList(ObservableCollection<O> collection, Func<O, K> keySelector, Func<O, I> uidSelector, IComparer<K> comparer) : base(collection, keySelector, uidSelector, new SortedList<K, O>(comparer))
        {

        }

        #endregion

        #region Public Methods

        public int IndexOfValue(O item)
        {
            return this.Items.IndexOfValue(item);
        }

        public int IndexOfKey(K key)
        {
            return this.Items.IndexOfKey(key);
        }

        #endregion
    }

}
