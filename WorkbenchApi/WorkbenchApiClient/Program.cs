using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WorkbenchApiClient.Swagger;

namespace WorkbenchApiClient
{
    class Program
    {
        static void Main(string[] args)
        {
            Task.Run(async () => await MainAsync()).Wait();

            Console.WriteLine("Press any key to continue...");

            Console.ReadLine();
        }

        static async Task MainAsync()
        {
            var client = new CompanyListApiWebProxy(new Uri("https://web.workbench.co.nz/WorkbenchV4/"));
            var result = await client.Post(new GridRequestParametersApi
            {
                predicate = new DynamicPredicateApi
                {
                    PredicateRows = new List<DynamicPredicateRowApi>
                    {
                        new DynamicPredicateRowApi
                        {
                            Display = true,
                            LeftOperand = "CompanyName",
                            Operator = DynamicPredicateRowApi.OperatorValues.Like,
                            RightOperand = new List<string>{ "Work" } 
                        }
                    }
                },
                page = 1,
                rows = 20,
                sidx = "CompanyName",
                sord = "asc"
            });
            Console.WriteLine("Total Records:" + result.records);
        }
    }
}
