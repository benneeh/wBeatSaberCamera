using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using wBeatSaberCamera.Models;

namespace wBeatSaberCamera.Utils
{
    public class DirtyBase : ObservableBase
    {
        public DirtyBase()
        {
            PropertyChanged += DirtyBase_PropertyChanged;
        }

        private void DirtyBase_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (!IsDirty && e.PropertyName != nameof(IsDirty))
            {
                IsDirty = ShouldBeDirty(e.PropertyName);
                //Console.WriteLine($"{this.GetType().Name}.{e.PropertyName} caused to be dirty");
            }
        }

        protected virtual bool ShouldBeDirty(string propertyName) => true;


        private bool _isDirty;

        public bool IsDirty
        {
            get => _isDirty;
            set
            {
                if (value == _isDirty)
                {
                    return;
                }

                _isDirty = value;
                OnPropertyChanged();
            }
        }

        public virtual void Clean()
        {
            IsDirty = false;
            //Console.WriteLine($"{this.GetType().Name} is now clean");
        }

        protected void SubscribeDirtyCollection<T>(ObservableCollection<T> observableCollection) where T : DirtyBase
        {
            if (observableCollection == null)
            {
                return;
            }

            observableCollection.CollectionChanged += DirtyCollection_CollectionChanged;
            SubscribeCollectionItems(observableCollection);
        }

        protected void UnsubscribeDirtyCollection<T>(ObservableCollection<T> observableCollection) where T : DirtyBase
        {
            if (observableCollection == null)
            {
                return;
            }

            observableCollection.CollectionChanged -= DirtyCollection_CollectionChanged;
            UnsubscribeCollectionItems(observableCollection);
        }

        protected void SubscribeDirtyCollection<TKey, TValue>(ObservableDictionary<TKey, TValue> observableDictionary)
        {
            if (observableDictionary == null)
            {
                return;
            }

            observableDictionary.CollectionChanged += DirtyDictionary_CollectionChanged<TKey, TValue>;
            SubscribeCollectionItems(observableDictionary.Values.OfType<DirtyBase>());
        }

        protected void UnsubscribeDirtyCollection<TKey, TValue>(ObservableDictionary<TKey, TValue> observableDictionary)
        {
            if (observableDictionary == null)
            {
                return;
            }

            observableDictionary.CollectionChanged -= DirtyDictionary_CollectionChanged<TKey, TValue>;
            UnsubscribeCollectionItems(observableDictionary.Values.OfType<DirtyBase>());
        }

        protected void SubscribeDirtyChild(DirtyBase dirtyChild)
        {
            if (dirtyChild == null)
            {
                return;
            }
            Console.WriteLine($"[{GetType().Name}] Subscribing to PropertyChanged event on " + dirtyChild.GetType().Name);
            dirtyChild.PropertyChanged += DirtyCollectionItem_PropertyChanged;
        }

        protected void UnsubscribeDirtyChild(DirtyBase dirtyChild)
        {
            if (dirtyChild == null)
            {
                return;
            }
            Console.WriteLine($"[{GetType().Name}] Unsubscribing from PropertyChanged event on " + dirtyChild.GetType().Name);
            dirtyChild.PropertyChanged -= DirtyCollectionItem_PropertyChanged;
        }

        private void SubscribeCollectionItems(IEnumerable<DirtyBase> profiles)
        {
            if (profiles == null)
            {
                return;
            }

            foreach (DirtyBase profile in profiles)
            {
                profile.PropertyChanged += DirtyCollectionItem_PropertyChanged;
            }
        }

        private void UnsubscribeCollectionItems(IEnumerable<DirtyBase> profiles)
        {
            if (profiles == null)
            {
                return;
            }
            foreach (DirtyBase profile in profiles)
            {
                UnsubscribeDirtyChild(profile);
            }
        }

        private void DirtyCollectionItem_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(IsDirty) && ((DirtyBase)sender).IsDirty)
            {
                IsDirty = true;
            }
        }

        private void DirtyCollection_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            SubscribeCollectionItems(e.NewItems?.OfType<DirtyBase>());
            UnsubscribeCollectionItems(e.OldItems?.OfType<DirtyBase>());
            IsDirty = true;
        }

        private void DirtyDictionary_CollectionChanged<TKey, TValue>(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            SubscribeCollectionItems(e.NewItems?.OfType<KeyValuePair<TKey, TValue>>().Select(x => x.Value).OfType<DirtyBase>());
            UnsubscribeCollectionItems(e.OldItems?.OfType<KeyValuePair<TKey, TValue>>().Select(x => x.Value).OfType<DirtyBase>());
            IsDirty = true;
        }
    }
}