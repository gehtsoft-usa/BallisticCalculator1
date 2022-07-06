namespace BallisticCalculator
{ 
    internal static class HashUtil
    {
        public static int CodeCombine(int o1, int o2)
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 23 + o1;
                hash = hash * 23 + o2;
                return hash;
            }
        }       

        public static int HashCombine(object o1, object o2)
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 23 + (o1 ?? 0).GetHashCode();
                hash = hash * 23 + (o2 ?? 0).GetHashCode();
                return hash;
            }
        }

        public static int HashCombine(object o1, object o2, object o3)
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 23 + (o1 ?? 0).GetHashCode();
                hash = hash * 23 + (o2 ?? 0).GetHashCode();
                hash = hash * 23 + (o3 ?? 0).GetHashCode();
                return hash;
            }
        }

        public static int HashCombine(object o1, object o2, object o3, object o4)
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 23 + (o1 ?? 0).GetHashCode();
                hash = hash * 23 + (o2 ?? 0).GetHashCode();
                hash = hash * 23 + (o3 ?? 0).GetHashCode();
                hash = hash * 23 + (o4 ?? 0).GetHashCode();
                return hash;
            }
        }

        public static int HashCombine(object o1, object o2, object o3, object o4, object o5)
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 23 + (o1 ?? 0).GetHashCode();
                hash = hash * 23 + (o2 ?? 0).GetHashCode();
                hash = hash * 23 + (o3 ?? 0).GetHashCode();
                hash = hash * 23 + (o4 ?? 0).GetHashCode();
                hash = hash * 23 + (o5 ?? 0).GetHashCode();
                return hash;
            }
        }

        public static int HashCombine(params object[] args)
        {
            unchecked
            {
                int hash = 17;
                for (int i = 0; i < args.Length; i++)
                    hash = hash * 23 + (args[i] ?? 0).GetHashCode();
                return hash;
            }
        }
    }
}
