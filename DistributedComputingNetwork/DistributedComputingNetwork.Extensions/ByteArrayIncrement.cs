namespace DistributedComputingNetwork.Extensions
{
    public static class ByteArrayExtensions
    {
        public static void Increment(this byte[] array)
        {
            int index = array.Length - 1;
            array[index]++;
            if (array[index] != 0) return;
            index--;
            while (index >= 0)
            {
                array[index]++;
                if (array[index] != 0)
                    break;
                index--;
            }
        }
    }
}
