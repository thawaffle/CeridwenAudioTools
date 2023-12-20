using System;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;

namespace com.ceridwen.audio
{
    internal class EncapsulatedConcurrentDictionary<K, I, O> : EncapsulatedDictionary<K, I, O, ConcurrentDictionary<K, O>>
    {

        #region Constructors/Destructors
        
        public EncapsulatedConcurrentDictionary(ObservableCollection<O> collection, Func<O, K> keySelector, Func<O, I> uidSelector) : base(collection, keySelector, uidSelector, new ConcurrentDictionary<K, O>())
        {

        }

        #endregion

    }
}
