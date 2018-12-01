namespace SBR {
    public struct Pair<T1, T2> {
        public T1 t1;
        public T2 t2;
        public Pair(T1 t1, T2 t2) {
            this.t1 = t1;
            this.t2 = t2;
        }

        public override int GetHashCode() {
            return t1.GetHashCode() ^ t2.GetHashCode();
        }

        public override bool Equals(object obj) {
            if (!(obj is Pair<T1, T2>)) {
                return false;
            }

            var o = (Pair<T1, T2>)obj;

            return t1.Equals(o.t1) && t2.Equals(o.t2);
        }

        public static bool operator ==(Pair<T1, T2> lhs, Pair<T1, T2> rhs) {
            return lhs.Equals(rhs);
        }

        public static bool operator !=(Pair<T1, T2> lhs, Pair<T1, T2> rhs) {
            return !lhs.Equals(rhs);
        }
    }

    public struct UnorderedPair<T> {
        public T t1;
        public T t2;

        public UnorderedPair(T t1, T t2) {
            this.t1 = t1;
            this.t2 = t2;
        }

        public bool Contains(T item) {
            return t1.Equals(item) || t2.Equals(item);
        }

        public bool Adjacent(UnorderedPair<T> other) {
            return Contains(other.t1) || Contains(other.t2);
        }

        public T Not(T item) {
            if ((t1.Equals(item)) != (t2.Equals(item))) {
                if (t1.Equals(item)) {
                    return t2;
                } else {
                    return t1;
                }
            } else {
                throw new System.ArgumentException("Argument not either element!");
            }
        }

        public override int GetHashCode() {
            return t1.GetHashCode() ^ t2.GetHashCode();
        }

        public override bool Equals(object obj) {
            if (!(obj is UnorderedPair<T>)) {
                return false;
            }

            var o = (UnorderedPair<T>)obj;

            return (t1.Equals(o.t1) && t2.Equals(o.t2)) || (t1.Equals(o.t2) && t2.Equals(o.t1));
        }

        public static bool operator ==(UnorderedPair<T> lhs, UnorderedPair<T> rhs) {
            return lhs.Equals(rhs);
        }

        public static bool operator !=(UnorderedPair<T> lhs, UnorderedPair<T> rhs) {
            return !lhs.Equals(rhs);
        }
    }
}