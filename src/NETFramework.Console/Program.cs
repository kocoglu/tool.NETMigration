using System.Collections.Generic;

namespace NETFramework.Console
{
    class Program
    {
        static void Main(string[] args)
        {
            System.Console.WriteLine("Hello World!");
            var math = new Library001.Mathematics(3, 5);
            System.Console.WriteLine(math.Add());

            IEnumerable<TourVisio.Infrastructure.Data.Entities.mdlMarket> marketEntities;
            using (var repo = new TourVisio.Infrastructure.Data.Repository<TourVisio.Infrastructure.Data.Entities.mdlMarket>("Application Name=Tourvisio.WebService.Api.Mustafa; server=10.1.10.40; database=TVB2C2016; user id=TOURVISIO; Password=S1611n2301T; Max Pool Size=1000;"))
            {
                marketEntities = repo.GetList();
            }

            foreach (var marketEntity in marketEntities)
            {
                System.Console.WriteLine(marketEntity.Name);
            }

            System.Console.ReadKey();
        }
    }
}
