﻿using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;

namespace Edubase.Services.Texuna.Glimpse
{
    public class ApiTraceList : IList<ApiTraceData>
    {
        private readonly List<ApiTraceData> _data;

        private readonly int _sessionRetentionCount;

        public ApiTraceList()
        {
            _data = new List<ApiTraceData>();

            var configValue = ConfigurationManager.AppSettings["ApiTraceSessionRetentionCount"];
            if (!int.TryParse(configValue, out _sessionRetentionCount))
            {
                _sessionRetentionCount = 25;
            }
        }

        public IEnumerator<ApiTraceData> GetEnumerator()
        {
            return _data.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _data.GetEnumerator();
        }

        public void Add(ApiTraceData item)
        {
            _data.Add(item);
            TrimCollection();
        }

        public void Clear()
        {
            _data.Clear();
        }

        public bool Contains(ApiTraceData item)
        {
            return _data.Contains(item);
        }

        public void CopyTo(ApiTraceData[] array, int arrayIndex)
        {
            _data.CopyTo(array, arrayIndex);
            TrimCollection();
        }

        public bool Remove(ApiTraceData item)
        {
            return _data.Remove(item);
        }

        public int Count => _data.Count;

        public bool IsReadOnly => false;

        public int IndexOf(ApiTraceData item)
        {
            return _data.IndexOf(item);
        }

        public void Insert(int index, ApiTraceData item)
        {
            _data.Insert(index, item);
            TrimCollection();
        }

        public void RemoveAt(int index)
        {
            _data.RemoveAt(index);
        }

        public ApiTraceData this[int index]
        {
            get { return _data[index]; }
            set { _data[index] = value; }
        }

        private void TrimCollection()
        {
            while (_data.Count > _sessionRetentionCount)
            {
                var minStartTime = _data.Min(d => d.StartTime);
                var oldest = _data.Single(d => d.StartTime == minStartTime);
                _data.Remove(oldest);
            }
        }
    }
}