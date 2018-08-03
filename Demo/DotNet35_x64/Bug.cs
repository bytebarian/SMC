namespace Test
{
    public class Bug
    {
        public void DoWork()
        {
            int[] table = null;

            for(int i = 0; i < 10; i++)
            {
                table[i] = i;
            }
        }
    }
}
