using System;
using System.Collections.Generic;
using System.Linq;

namespace UnityMcp.Tools
{
    // ---------------- ���Ƚ������ַ����� StringComparer����������ʹ������������� ----------------
    public sealed class ObjectKeyComparer : IEqualityComparer<object>
    {
        private readonly StringComparer _sc;
        public ObjectKeyComparer(StringComparer sc) => _sc = sc;
        public new bool Equals(object? x, object? y)
        {
            if (ReferenceEquals(x, y)) return true;
            if (x is null || y is null) return false;
            if (x is string sx && y is string sy) return _sc.Equals(sx, sy);
            return x.Equals(y);
        }
        public int GetHashCode(object obj)
        {
            if (obj is string s) return _sc.GetHashCode(s);
            return obj.GetHashCode();
        }
    }
}
