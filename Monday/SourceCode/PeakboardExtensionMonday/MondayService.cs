using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PeakboardExtensionMonday.MondayEntities;
using System;
using System.Collections.Generic;
using System.Data;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace PeakboardExtensionMonday
{
    public class MondayService
    {
        private MondayClient _mondayClient;

        public MondayService()
        {

        }
        public MondayService(MondayClient client)
        {
            _mondayClient = client;
        }

        /// <summary>
        /// Send query as a HTTP POST and return the response
        /// </summary>
        private string GetResponseData(string query)
        {
            StringContent content = new StringContent(query, Encoding.UTF8, "application/json");
            using (var response = _mondayClient.PostAsync("", content))
            {
                if (!response.Result.IsSuccessStatusCode)
                {
                    throw new JsonException($"The response from {_mondayClient.BaseAddress} was {response.Result.StatusCode}");
                }
                return response.Result.Content.ReadAsStringAsync().Result;
            }
        }

        /// <summary>
        /// Get the 'data' node from a GraphQL response if it exists and no errors occurred, else throw a HttpException.
        /// </summary>
        private dynamic ParseGraphQLResponseBoard(string responseString, string collectionKey = "")
        {
            JObject responseObject = JObject.Parse(responseString);

            // The 'errors' and 'data' keys are part of the standard response format set in the GraphQL spec (see https://graphql.github.io/graphql-spec/draft/#sec-Response)
            if (responseObject["errors"] != null)
            {
                throw new JsonException($"The request was successful but contained errors: " +
                                       $"{JsonConvert.DeserializeObject<dynamic>(responseObject["errors"].ToString())}");
            }
            if (responseObject["data"] == null)
            {
                throw new JsonException("The request was successful but contained no data");
            }

            dynamic data;
            data = collectionKey == "" ? JsonConvert.DeserializeObject<dynamic>(responseObject["data"].ToString()) :
                                         JsonConvert.DeserializeObject<dynamic>(responseObject["data"][collectionKey].ToString());

            return data;
        }

        private dynamic ParseGraphQLResponseGroup(string responseString, string collectionKey = "")
        {
            JObject responseObject = JObject.Parse(responseString);

            // The 'errors' and 'data' keys are part of the standard response format set in the GraphQL spec (see https://graphql.github.io/graphql-spec/draft/#sec-Response)
            if (responseObject["errors"] != null)
            {
                throw new JsonException($"The request was successful but contained errors: " +
                                       $"{JsonConvert.DeserializeObject<dynamic>(responseObject["errors"].ToString())}");
            }
            if (responseObject["data"] == null)
            {
                throw new JsonException("The request was successful but contained no data");
            }

            dynamic data;
            data = collectionKey == "" ? JsonConvert.DeserializeObject<dynamic>(responseObject["data"].ToString()) :
                                         JsonConvert.DeserializeObject<dynamic>(responseObject["data"]["boards"][0][collectionKey].ToString());

            return data;
        }

        public List<Board> GetBoards()
        {
            string query = "{ \"query\": \"" + @"{ boards { id name } }" + "\" }";
            string response = GetResponseData(query);
            dynamic data = ParseGraphQLResponseBoard(response, "boards");
            List<Board> boards = JsonConvert.DeserializeObject<List<Board>>(data.ToString());
            return boards;
        }

        public List<Group> GetGroups(int boardId)
        {
            string query = "{ \"query\": \"" + @"{ boards(ids: "+boardId +") { groups { id title } } }" + "\" }";
            string response = GetResponseData(query);
            dynamic data = ParseGraphQLResponseGroup(response, "groups");
            List<Group> groups = JsonConvert.DeserializeObject<List<Group>>(data.ToString());
            return groups;
        }

        public Board GetBoardWithItems(int boardId)
        {
            string query = "{ \"query\": \"" + @"{ boards(ids: " + boardId + ") { name items { id name column_values { title text } } } }" + "\" }";
            string response = GetResponseData(query);
            dynamic data = ParseGraphQLResponseBoard(response, "boards");
            List<Board> boards = JsonConvert.DeserializeObject<List<Board>>(data.ToString());

            if (boards == null || boards.Count < 1)
                return null;

            return boards[0];
        }

        public Group GetGroupWithItems(int boardId, string groupId)
        {
            string query = "{ \"query\": \"" + @"{ boards(ids: " + boardId + ") { groups (ids: " + groupId + ") { items { id name column_values { title text } } } } }" + "\" }";
            string response = GetResponseData(query);
            dynamic data = ParseGraphQLResponseGroup(response, "groups");
            List<Group> groups = JsonConvert.DeserializeObject<List<Group>>(data.ToString());

            if (groups == null || groups.Count < 1)
                return null;

            return groups[0];
        }

        public async Task<string> GetDataFromQuery(string url, string token, string query)
        {
            var httpClient = new HttpClient
            {
                BaseAddress = new Uri(url)
            };

            httpClient.DefaultRequestHeaders.Add("User-Agent", "MyConsoleApp");
            httpClient.DefaultRequestHeaders.Add("Authorization", token);

            var queryObject = new
            {
                query = query,
                variables = new { }
            };

            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Post,
                Content = new StringContent(JsonConvert.SerializeObject(queryObject), Encoding.UTF8, "application/json")
            };

            //dynamic responseObj;
            DataTable dt=new DataTable();
            string responseString;
            using (var response = await httpClient.SendAsync(request))
            {
                response.EnsureSuccessStatusCode();

                responseString = await response.Content.ReadAsStringAsync();
                //dt = (DataTable)JsonConvert.DeserializeObject(responseString, dt.GetType());
                //responseObj = JsonConvert.DeserializeObject<dynamic>(responseString);
            }



            return responseString;
        }





        


    }
}
