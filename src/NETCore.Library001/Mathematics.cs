namespace NETCore.Library001
{
    public class Mathematics
    {
        private int Number1 { get; set; }
        private int Number2 { get; set; }
        public Mathematics(int number1, int number2)
        {
            Number1 = number1;
            Number2 = number2;
        }

        public int Add()
        {
            return Number1 + Number2;
        }
    }
}
