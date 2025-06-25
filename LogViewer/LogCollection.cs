using System.Collections;
using System.Collections.Specialized;
using System.ComponentModel;

namespace LogViewer
{
    public class LogCollection : INotifyCollectionChanged, INotifyPropertyChanged, IList<LogEventArgs>
    {
        public event NotifyCollectionChangedEventHandler? CollectionChanged;
        public event PropertyChangedEventHandler? PropertyChanged;

        public int Count
        {
            get { lock (_lockObject) { return _logs.Count; } }
        }
        public IEnumerable<LogEventArgs> Items
        {
            get { lock (_lockObject) { return new List<LogEventArgs>(_logs); } }
        }

        public bool IsReadOnly => false;

        public LogEventArgs this[int index]
        {
            get { lock (_lockObject) { return _logs[index]; } }
            set
            {
                if (value is null) throw new ArgumentNullException(nameof(value));
                bool added = false;
                lock (_lockObject)
                {
                    if (_logSet.Add(value))
                    {
                        _logs[index] = value;
                        added = true;
                    }
                }
                if (added)
                    OnCollectionChanged(NotifyCollectionChangedAction.Replace, value, index);
                else
                    throw new InvalidOperationException("Item already exists in the collection.");
            }
        }

        private List<LogEventArgs> _logs = [];
        private HashSet<LogEventArgs> _logSet = new();
        private object _lockObject = new();

        public bool Add(LogEventArgs logEvent)
        {
            if (logEvent is null) throw new ArgumentNullException(nameof(logEvent));

            bool added = false;
            lock (_lockObject)
            {
                added = _logSet.Add(logEvent);
                if (added)
                    _logs.Add(logEvent);
            }
            if (added)
                OnCollectionChanged(NotifyCollectionChangedAction.Add, new List<LogEventArgs>() { logEvent });

            return added;
        }

        public int AddRange(IEnumerable<LogEventArgs> logEvents)
        {
            if (logEvents is null) throw new ArgumentNullException(nameof(logEvents));

            bool added = false;
            List<LogEventArgs> addedEvents = [];
            lock (_lockObject)
            {
                foreach (var logEvent in logEvents)
                {
                    if (_logSet.Add(logEvent))
                    {
                        addedEvents.Add(logEvent);
                        added = true;
                    }
                }
                if (added)
                    _logs.AddRange(addedEvents);
            }
            if (added)
                OnCollectionChanged(NotifyCollectionChangedAction.Add, addedEvents);
            return addedEvents.Count;
        }

        public bool Remove(LogEventArgs logEvent)
        {
            if (logEvent is null) throw new ArgumentNullException(nameof(logEvent));

            bool removed = false;
            lock (_lockObject)
            {
                if (_logSet.Remove(logEvent))
                {
                    int index = _logs.IndexOf(logEvent);
                    if (index >= 0)
                    {
                        _logs.RemoveAt(index);
                        removed = true;
                    }
                }
            }
            if (removed)
                OnCollectionChanged(NotifyCollectionChangedAction.Remove, new List<LogEventArgs>() { logEvent });
            return removed;
        }

        public int RemoveRange(int startIndex, int count)
        {
            if (startIndex < 0 || count < 0)
                throw new ArgumentOutOfRangeException("Invalid range specified for removal.");

            List<LogEventArgs> itemsToRemove = [];
            lock (_lockObject)
            {
                itemsToRemove = _logs.Skip(startIndex).Take(count).ToList();
                if (itemsToRemove.Count > 0)
                {
                    foreach (var item in itemsToRemove)
                    {
                        _logSet.Remove(item);
                    }
                    _logs.RemoveRange(startIndex, itemsToRemove.Count);
                }
            }
            if (itemsToRemove.Count > 0)
                OnCollectionChanged(NotifyCollectionChangedAction.Remove, itemsToRemove);
            return itemsToRemove.Count;
        }

        public int RemoveRange(IEnumerable<LogEventArgs> logEvents)
        {
            if (logEvents is null) throw new ArgumentNullException(nameof(logEvents));

            List<LogEventArgs> removedEvents = [];
            lock (_lockObject)
            {
                foreach (var logEvent in logEvents)
                {
                    if (_logSet.Remove(logEvent))
                        removedEvents.Add(logEvent);
                }
                if (removedEvents.Count > 0)
                {
                    foreach (var item in removedEvents)
                    {
                        int index = _logs.IndexOf(item);
                        if (index >= 0) _logs.RemoveAt(index);
                    }
                }
            }
            if (removedEvents.Count > 0)
                OnCollectionChanged(NotifyCollectionChangedAction.Remove, removedEvents);
            return removedEvents.Count;
        }

        public void Clear()
        {
            lock (_lockObject)
            {
                _logSet.Clear();
                _logs.Clear();
            }
            OnCollectionChanged(NotifyCollectionChangedAction.Reset);
        }

        protected void OnCollectionChanged(NotifyCollectionChangedAction action)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Items)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Count)));
            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(action));
        }

        protected void OnCollectionChanged(NotifyCollectionChangedAction action, object? item = null, int index = -1)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Items)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Count)));
            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(action, changedItem: item, index: index));
        }

        protected void OnCollectionChanged(NotifyCollectionChangedAction action, IList? items = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Count)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Items)));
            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(action, changedItems: items));
        }

        public int IndexOf(LogEventArgs item) => _logs.IndexOf(item);

        public void Insert(int index, LogEventArgs item)
        {
            bool added = false;
            lock (_lockObject)
            {
                added = _logSet.Add(item);
                if (added)
                    _logs.Insert(index, item);
            }
            if (added)
                OnCollectionChanged(NotifyCollectionChangedAction.Add, item, index);
            else
                throw new InvalidOperationException("Item already exists in the collection.");
        }

        public void RemoveAt(int index)
        {
            if (index < 0 || index >= _logs.Count)
                throw new ArgumentOutOfRangeException(nameof(index), "Index is out of range.");

            bool removed = false;
            LogEventArgs? item = null;
            lock (_lockObject)
            {
                item = _logs[index];
                removed = _logSet.Remove(item);
                if (removed)
                    _logs.RemoveAt(index);
            }
            if (removed)
                OnCollectionChanged(NotifyCollectionChangedAction.Remove, item, index);
            else
                throw new InvalidOperationException("Item not found in the collection.");
        }

        void ICollection<LogEventArgs>.Add(LogEventArgs item) => Add(item);

        public bool Contains(LogEventArgs item) => _logSet.Contains(item);

        public void CopyTo(LogEventArgs[] array, int arrayIndex)
        {
            if (array is null) throw new ArgumentNullException(nameof(array));

            if (arrayIndex < 0 || arrayIndex + _logs.Count > array.Length)
                throw new ArgumentOutOfRangeException(nameof(arrayIndex), "Array index is out of range.");

            lock (_lockObject)
            {
                _logs.CopyTo(array, arrayIndex);
            }
        }

        public IEnumerator<LogEventArgs> GetEnumerator()
        {
            List<LogEventArgs> snapshot;
            lock (_lockObject)
            {
                snapshot = new(_logs);
            }
            return snapshot.GetEnumerator();
        }
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}