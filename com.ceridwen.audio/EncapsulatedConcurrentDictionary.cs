using System;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;

namespace com.ceridwen.audio
{
    internal class EncapsulatedConcurrentDictionary<K, O> : EncapsulatedDictionary<K, O, ConcurrentDictionary<K, O>>
    {

        #region Constructors/Destructors
        
        public EncapsulatedConcurrentDictionary(ObservableCollection<O> collection, Func<O, K> keySelector) : base(collection, keySelector, new ConcurrentDictionary<K, O>())
        {

        }

        #endregion

    }
}
