using System;

namespace Tool
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");
            new Startup().Migrate();
            Console.ReadKey();
        }
    }
}
