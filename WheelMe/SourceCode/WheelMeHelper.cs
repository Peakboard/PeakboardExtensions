using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using WheelMe.DTO;

namespace WheelMe
{
    class WheelMeHelper
    {
        // TODO: Check extension lifecycle if positions will be updated
        private static Dictionary<string, Dictionary<string, string>> myPositionMap = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);

        public static string GetPositionNameFromId(HttpClient client, string floorId, string ID)
        {
            if (string.IsNullOrWhiteSpace(ID))
                return string.Empty;

            EnsureMappingIsFetched(client, floorId);
            if (myPositionMap.TryGetValue(floorId, out var floorItems))
            {
                return floorItems.TryGetValue(ID, out var id) ? id : ID;
            }

            return ID;
        }
        
        private static void EnsureMappingIsFetched(HttpClient client, string floorId)
        {
            if (myPositionMap.Count == 0 || !myPositionMap.TryGetValue(floorId, out var value) || value.Count == 0)
            {
                Task.Run(async () => await EnsureMappingIsFetchedAsync(client, floorId)).GetAwaiter().GetResult();
            }
        }

        private static async Task EnsureMappingIsFetchedAsync(HttpClient client, string floorId)
        {;
            var uri = $"api/public/maps/{floorId}/positions";
            var result = await client.GetRequestAsync<PositionDto[]>(uri);

            var floorItems = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (myPositionMap.TryGetValue(floorId, out var value))
            {
                floorItems = value;
            }
            else
            {
                myPositionMap.Add(floorId, floorItems);
            }

            foreach (var position in result)
            {
                var key = position.Id.ToString("D");
                floorItems.Add(key, position.Name);
            }
        }
    }
}
