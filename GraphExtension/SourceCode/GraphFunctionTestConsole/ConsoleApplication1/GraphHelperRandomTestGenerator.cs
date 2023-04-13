using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using PeakboardExtensionGraph;
using PeakboardExtensionGraph.UserAuth;

namespace ConsoleApplication1
{
    public class GraphHelperRandomTestGenerator
    {
        private GraphHelperUserAuth _helper;
        private int _seed;
        private List<Tuple<string, RequestParameters>> _testSuite;
        private int _testCount;
        private string _filePath;

        public GraphHelperRandomTestGenerator(GraphHelperUserAuth helper, int seed, int testCount, string filePath)
        {
            _helper = helper;
            _seed = seed;
            _testCount = testCount;
            _filePath = filePath;
            _testSuite = new List<Tuple<string, RequestParameters>>();
        }
        
        public List<Tuple<string, RequestParameters>> GetSuite()
        {
            return this._testSuite;
        }

        public void InitTestSuite()
        
        {
            Random rand = new Random(_seed);

            for(int j = 0; j < _testCount; j++){
                // get random request
                int requestIndex = rand.Next(0, RequestData.Requests.Length);
                var request = RequestData.Requests[requestIndex];
                string select = "", orderby = "", filter = "";
                int top, skip;

                // get random parameters

                // select param
                if (rand.Next(0, 2) == 1)
                {
                    int amount = rand.Next(1, RequestData.SelectParams[request].Length);
                    var selectParameters = ((string[])RequestData.SelectParams[request].Clone()).ToList();
                    for (int i = 0; i < amount; i++)
                    {
                        int index = rand.Next(0, selectParameters.Count);
                        select += $"{selectParameters[index]},";
                        selectParameters.Remove(selectParameters[index]);
                    }

                    select = select.Remove(select.Length - 1);
                }

                // order by param
                if (rand.Next(0, 2) == 1 && RequestData.OrderByParams[request] != null)
                {
                    int amount = rand.Next(1, RequestData.OrderByParams[request].Length);
                    var orderByParameters = ((string[])RequestData.OrderByParams[request].Clone()).ToList();

                    for (int i = 0; i < amount; i++)
                    {
                        int index = rand.Next(0, orderByParameters.Count);
                        orderby += $"{orderByParameters[index]},";
                        orderByParameters.Remove(orderByParameters[index]);
                    }

                    orderby = orderby.Remove(orderby.Length - 1);
                }

                // filter param
                if (rand.Next(0, 2) == 1 && RequestData.FilterParams.ContainsKey(request))
                {
                    filter = RequestData.FilterParams[request];
                }

                // top & skip
                top = rand.Next(0, 50);
                skip = 0; // rand.Next(0, 50);

                var parameters = new RequestParameters()
                {
                    Select = select,
                    OrderBy = orderby,
                    Filter = filter,
                    Top = top,
                    Skip = skip
                };

                _testSuite.Add(new Tuple<string, RequestParameters>(request, parameters));
            }

        }

        public async Task RunTestSuite()
        {
            var writer = new StreamWriter(_filePath);
            int count = 1;
            int allowedExceptions = 0;
            int forbiddenExceptions = 0;
            int totalExceptions = 0;
            
            await writer.WriteLineAsync($"Running testsuite with seed {_seed} including {_testCount} tests...");
            await writer.WriteLineAsync("---------------------------------------------------------------------------------------");

            foreach (var test in _testSuite)
            {
                try
                {
                    await _helper.GetAsync($"/{test.Item1}", test.Item2);
                }
                catch (Exception ex)
                {
                    if (ex is MsGraphException)
                    {
                        MsGraphException msex = (MsGraphException)ex;
                        allowedExceptions++;
                        totalExceptions++;
                        await writer.WriteLineAsync($"Caught Exception at Test {count}:");
                        await writer.WriteLineAsync("\tAllowed: Yes   Expected: No");
                        await writer.WriteLineAsync($"\tApi Call: {msex.Url}");
                        await writer.WriteLineAsync($"\tMessage: {msex.Message}");
                    }
                    else
                    {
                        forbiddenExceptions++;
                        totalExceptions++;
                        await writer.WriteLineAsync($"Caught Exception at Test {count}:");
                        await writer.WriteLineAsync("\tAllowed: No   Expected: No");
                        await writer.WriteLineAsync($"\tApi Call: {test.Item1}");
                        await writer.WriteLineAsync($"\tParameter: {test.Item2}");
                        await writer.WriteLineAsync($"\tMessage: {ex.Message}");
                    }
                    
                }

                count++;
                
                if(count % 100 == 0) Console.Write(".");
                
            }
            await writer.WriteLineAsync("---------------------------------------------------------------------------------------");
            await writer.WriteLineAsync($"Executed {_testCount} tests");
            await writer.WriteLineAsync($"Successful tests:\t {_testCount - totalExceptions}/{_testCount}");
            await writer.WriteLineAsync($"Allowed exceptions:\t {allowedExceptions}/{totalExceptions}");
            await writer.WriteLineAsync($"Forbidden exceptions:\t {forbiddenExceptions}/{totalExceptions}\n");
            await writer.WriteLineAsync($"Error rate (including allowed exceptions):\t {(double)totalExceptions/(double)_testCount * 100}%");
            await writer.WriteLineAsync($"Error rate (only forbidden exceptions):\t {(double)forbiddenExceptions/(double)_testCount * 100}%");

            writer.Close();
        }


    }
}