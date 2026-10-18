namespace Wickfall
{
    /// <summary>A stable string hash (FNV-1a), the same on every run and platform.</summary>
    public static class StableHash
    {
        public static int Of(string text)
        {
            unchecked
            {
                uint hash = 2166136261;
                if (text != null)
                {
                    for (int i = 0; i < text.Length; i++)
                    {
                        hash ^= text[i];
                        hash *= 16777619;
                    }
                }
                return (int)hash;
            }
        }
    }
}
