using System.Collections;
using System.Collections.Specialized;
using System.ComponentModel;
using PropertyChanged;

namespace LogViewer
{
    /// <summary>
    /// Represents a thread-safe, observable collection of <see cref="LogEventArgs"/> for use in log viewers and data-bound UIs.
    /// Supports fast lookup, duplicate prevention, and notifies listeners of changes.
    /// </summary>
    public class LogCollection : INotifyCollectionChanged, INotifyPropertyChanged, IList<LogEventArgs>
    {
        /// <inheritdoc/>
        public event NotifyCollectionChangedEventHandler? CollectionChanged;
        /// <inheritdoc/>
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// Gets the number of log entries in the collection.
        /// </summary>
        public int Count
        {
            get { lock (_lockObject) { return _logs.Count; } }
        }
        /// <summary>
        /// Gets a snapshot of the log entries in the collection.
        /// </summary>
        public IEnumerable<LogEventArgs> Items
        {
            get { lock (_lockObject) { return [.. _logs]; } }
        }

        /// <summary>
        /// Gets a value indicating whether the collection is read-only. Always returns false.
        /// </summary>
        public bool IsReadOnly => false;

        /// <summary>
        /// Gets or sets the <see cref="LogEventArgs"/> at the specified index.
        /// Setting an item replaces it only if it is not already present in the collection.
        /// </summary>
        /// <param name="index">The zero-based index of the element to get or set.</param>
        /// <exception cref="ArgumentNullException">Thrown if value being assigned is null</exception>
        /// <exception cref="InvalidOperationException">Thrown if the item already exists in the collection.</exception>
        public LogEventArgs this[int index]
        {
            get { lock (_lockObject) { return _logs[index]; } }
            set
            {
                ArgumentNullException.ThrowIfNull(value, paramName: nameof(value));

                bool added = false;
                lock (_lockObject)
                {
                    // Only allow replacement if the new value is not already present
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

        // Internal storage for log entries and fast lookup.
        private readonly List<LogEventArgs> _logs = [];
        private readonly HashSet<LogEventArgs> _logSet = [];
        private readonly object _lockObject = new();

        /// <summary>
        /// Adds a log event to the collection if it does not already exist.
        /// </summary>
        /// <param name="logEvent">The log event to add.</param>
        /// <returns>True if the log event was added; false if it was a duplicate.</returns>
        /// <exception cref="ArgumentNullException">Thrown if the logEvent is null.</exception>
        public bool Add(LogEventArgs logEvent)
        {
            ArgumentNullException.ThrowIfNull(logEvent, paramName: nameof(logEvent));

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

        /// <summary>
        /// Adds a range of log events to the collection, skipping duplicates.
        /// </summary>
        /// <param name="logEvents">The log events to add.</param>
        /// <returns>The number of log events actually added.</returns>
        /// <exception cref="ArgumentNullException">Thrown if the logEvents or one of it's elements are null.</exception>
        public int AddRange(IEnumerable<LogEventArgs> logEvents)
        {
            ArgumentNullException.ThrowIfNull(logEvents, paramName: nameof(logEvents));

            bool added = false;
            List<LogEventArgs> addedEvents = [];
            int startIndex = -1;
            lock (_lockObject)
            {
                startIndex = _logs.Count;
                foreach (var logEvent in logEvents)
                {
                    ArgumentNullException.ThrowIfNull(logEvent, paramName: nameof(logEvent));
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
                OnCollectionChanged(NotifyCollectionChangedAction.Add, addedEvents, startIndex);
            return addedEvents.Count;
        }

        /// <summary>
        /// Removes a log event from the collection.
        /// </summary>
        /// <param name="logEvent">The log event to remove.</param>
        /// <returns>True if the log event was removed; false if it was not found.</returns>
        /// <exception cref="ArgumentNullException">Thrown if the logEvent is null.</exception>
        public bool Remove(LogEventArgs logEvent)
        {
            ArgumentNullException.ThrowIfNull(logEvent, paramName: nameof(logEvent));

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

        /// <summary>
        /// Removes a range of log events from the collection, starting at the specified index.
        /// </summary>
        /// <param name="startIndex">The zero-based starting index.</param>
        /// <param name="count">The number of items to remove. Removal stops when collection is empty if count exceeds total elements.</param>
        /// <returns>The number of items actually removed.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if startIndex or count is negative.</exception>
        public int RemoveRange(int startIndex, int count)
        {
            if (startIndex < 0)
                throw new ArgumentOutOfRangeException(nameof(startIndex), "Invalid starting index specified for removal");
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count), "Count must be non-negative");

            List<LogEventArgs> itemsToRemove = [];
            lock (_lockObject)
            {
                itemsToRemove = [.. _logs.Skip(startIndex).Take(count)];
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
                OnCollectionChanged(NotifyCollectionChangedAction.Remove, itemsToRemove, startIndex);
            return itemsToRemove.Count;
        }

        /// <summary>
        /// Removes all log events from the collection.
        /// </summary>
        public void Clear()
        {
            lock (_lockObject)
            {
                _logSet.Clear();
                _logs.Clear();
            }
            OnCollectionChanged(NotifyCollectionChangedAction.Reset);
        }

        /// <summary>
        /// Raises collection and property changed events for a collection change.
        /// </summary>
        /// <param name="action">The type of change that occurred.</param>
        [SuppressPropertyChangedWarnings]
        protected void OnCollectionChanged(NotifyCollectionChangedAction action)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Items)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Count)));
            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(action));
        }

        /// <summary>
        /// Raises collection and property changed events for a collection change involving a single item.
        /// </summary>
        /// <param name="action">The type of change that occurred.</param>
        /// <param name="item">The item involved in the change.</param>
        /// <param name="index">The index at which the change occurred.</param>
        [SuppressPropertyChangedWarnings]
        protected void OnCollectionChanged(NotifyCollectionChangedAction action, object? item = null, int index = -1)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Items)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Count)));
            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(action, changedItem: item, index: index));
        }

        /// <summary>
        /// Raises collection and property changed events for a collection change involving multiple items.
        /// </summary>
        /// <param name="action">The type of change that occurred.</param>
        /// <param name="items">The items involved in the change.</param>
        /// <param name="index">The index at which the change occurred.</param>
        [SuppressPropertyChangedWarnings]
        protected void OnCollectionChanged(NotifyCollectionChangedAction action, IList? items = null, int index = -1)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Count)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Items)));
            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(action, changedItems: items, index));
        }

        /// <summary>
        /// Returns the index of the specified log event in the collection.
        /// </summary>
        public int IndexOf(LogEventArgs item)
        {
            int index = -1;
            lock (_lockObject)
            {
                index = _logs.IndexOf(item);
            }
            return index;
        }

        /// <summary>
        /// Inserts a log event at the specified index if it does not already exist.
        /// </summary>
        /// <param name="index">The zero-based index at which to insert the item.</param>
        /// <param name="item">The log event to insert.</param>
        /// <exception cref="InvalidOperationException">Thrown if the item already exists in the collection.</exception>
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

        /// <summary>
        /// Removes the log event at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index of the item to remove.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if the index is out of range.</exception>
        /// <exception cref="InvalidOperationException">Thrown if the item is not found in the collection.</exception>
        public void RemoveAt(int index)
        {
            bool removed = false;
            LogEventArgs? item = null;
            lock (_lockObject)
            {
                if (index < 0 || index >= _logs.Count)
                    throw new ArgumentOutOfRangeException(nameof(index), "Index is out of range.");

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

        // Explicit interface implementation for ICollection<T>.Add
        void ICollection<LogEventArgs>.Add(LogEventArgs item) => Add(item);

        /// <summary>
        /// Determines whether the collection contains the specified log event.
        /// </summary>
        public bool Contains(LogEventArgs item)
        {
            bool contains = false;
            lock (_lockObject)
            {
                contains = _logSet.Contains(item);
            }
            return contains;
        }

        /// <summary>
        /// Copies the elements of the collection to an array, starting at the specified array index.
        /// </summary>
        /// <param name="array">The destination array.</param>
        /// <param name="arrayIndex">The zero-based index in the array at which copying begins.</param>
        /// <exception cref="ArgumentNullException">Thrown if the logEvent is null.</exception>
        public void CopyTo(LogEventArgs[] array, int arrayIndex)
        {
            ArgumentNullException.ThrowIfNull(array, paramName: nameof(array));

            if (arrayIndex < 0 || arrayIndex + _logs.Count > array.Length)
                throw new ArgumentOutOfRangeException(nameof(arrayIndex), "Array index is out of range.");

            lock (_lockObject)
            {
                _logs.CopyTo(array, arrayIndex);
            }
        }

        /// <summary>
        /// Returns an enumerator that iterates through a snapshot of the collection.
        /// </summary>
        public IEnumerator<LogEventArgs> GetEnumerator()
        {
            List<LogEventArgs> snapshot;
            lock (_lockObject)
            {
                snapshot = [.. _logs];
            }
            return snapshot.GetEnumerator();
        }
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}