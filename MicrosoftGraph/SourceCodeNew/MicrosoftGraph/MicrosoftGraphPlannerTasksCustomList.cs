using System;
using System.Collections.Generic;
using System.Net.Http;
using Newtonsoft.Json.Linq;
using Peakboard.ExtensionKit;

namespace MicrosoftGraph
{
    [Serializable]
    [CustomListIcon("MicrosoftGraph.MicrosoftGraph.png")]
    class MicrosoftGraphPlannerTasksCustomList : CustomListBase
    {
        /// <summary>
        /// Planner has 25 fixed category color slots. A slot only appears in the plan's
        /// categoryDescriptions when the user gave it a custom text label; otherwise Graph
        /// returns null and the Planner UI just shows the slot's default colour name
        /// ("Blue", "Pink", ...). This is the documented slot -> default colour mapping
        /// (category1 == Color0, ... category25 == Color24).
        /// </summary>
        private static readonly Dictionary<string, string> DefaultCategoryColors = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["category1"]  = "Pink",
            ["category2"]  = "Red",
            ["category3"]  = "Yellow",
            ["category4"]  = "Green",
            ["category5"]  = "Blue",
            ["category6"]  = "Purple",
            ["category7"]  = "Bronze",
            ["category8"]  = "Lime",
            ["category9"]  = "Aqua",
            ["category10"] = "Gray",
            ["category11"] = "Silver",
            ["category12"] = "Brown",
            ["category13"] = "Cranberry",
            ["category14"] = "Orange",
            ["category15"] = "Peach",
            ["category16"] = "Marigold",
            ["category17"] = "Light Green",
            ["category18"] = "Dark Green",
            ["category19"] = "Teal",
            ["category20"] = "Light Blue",
            ["category21"] = "Dark Blue",
            ["category22"] = "Lavender",
            ["category23"] = "Plum",
            ["category24"] = "Light Gray",
            ["category25"] = "Dark Gray",
        };

        protected override CustomListDefinition GetDefinitionOverride()
        {
            return new CustomListDefinition
            {
                ID = "MicrosoftGraphPlannerTasks",
                Name = "Microsoft Planner Tasks",
                Description = "Lists tasks of a Microsoft Planner plan, including completion, priority, due date and assignee IDs / names. Provides functions to create a task and to move a task to a bucket.",
                PropertyInputPossible = true,
                PropertyInputDefaults =
                {
                    new CustomListPropertyDefinition() { Name = "TenantId", Value = "" },
                    new CustomListPropertyDefinition() { Name = "ClientId", Value = "" },
                    new CustomListPropertyDefinition() { Name = "ClientSecret", TypeDefinition = TypeDefinition.String.With(masked: true) },
                    new CustomListPropertyDefinition() { Name = "PlanId", Value = "" },
                },
                Functions = new CustomListFunctionDefinitionCollection
                {
                    new CustomListFunctionDefinition
                    {
                        Name = "createTask",
                        Description = "Creates a new Planner task in the plan set by this list's PlanId property. Requires the Tasks.ReadWrite.All application permission. Returns the new task ID, or a string starting with 'ERROR:' on failure.",
                        InputParameters = new CustomListFunctionInputParameterDefinitionCollection
                        {
                            new CustomListFunctionInputParameterDefinition { Name = "title", Description = "Task title (required).", Optional = false, Type = CustomListFunctionParameterTypes.String },
                            new CustomListFunctionInputParameterDefinition { Name = "notes", Description = "Optional task description / notes (set on the task details after creation).", Optional = true, Type = CustomListFunctionParameterTypes.String },
                            new CustomListFunctionInputParameterDefinition { Name = "bucketId", Description = "Optional bucket to place the task in.", Optional = true, Type = CustomListFunctionParameterTypes.String },
                            new CustomListFunctionInputParameterDefinition { Name = "dueDateTime", Description = "Optional due date as ISO 8601, e.g. 2026-06-01T00:00:00Z.", Optional = true, Type = CustomListFunctionParameterTypes.String },
                            new CustomListFunctionInputParameterDefinition { Name = "assigneeIds", Description = "Optional comma-separated user GUIDs to assign.", Optional = true, Type = CustomListFunctionParameterTypes.String },
                            new CustomListFunctionInputParameterDefinition { Name = "priority", Description = "Optional priority: 1 urgent, 3 important, 5 medium, 9 low.", Optional = true, Type = CustomListFunctionParameterTypes.Number },
                        },
                        ReturnParameters = new CustomListFunctionReturnParameterDefinitionCollection
                        {
                            new CustomListFunctionReturnParameterDefinition { Name = "result", Type = CustomListFunctionParameterTypes.String },
                        }
                    },
                    new CustomListFunctionDefinition
                    {
                        Name = "assignTaskToBucket",
                        Description = "Moves an existing Planner task to a different bucket. Requires the Tasks.ReadWrite.All application permission. Returns 'OK', or a string starting with 'ERROR:' on failure.",
                        InputParameters = new CustomListFunctionInputParameterDefinitionCollection
                        {
                            new CustomListFunctionInputParameterDefinition { Name = "taskId", Description = "ID of the task to move (required).", Optional = false, Type = CustomListFunctionParameterTypes.String },
                            new CustomListFunctionInputParameterDefinition { Name = "bucketId", Description = "ID of the target bucket (required).", Optional = false, Type = CustomListFunctionParameterTypes.String },
                        },
                        ReturnParameters = new CustomListFunctionReturnParameterDefinitionCollection
                        {
                            new CustomListFunctionReturnParameterDefinition { Name = "result", Type = CustomListFunctionParameterTypes.String },
                        }
                    }
                }
            };
        }

        protected override CustomListExecuteReturnContext ExecuteFunctionOverride(CustomListData data, CustomListExecuteParameterContext context)
        {
            var ret = new CustomListExecuteReturnContext();

            if (context.FunctionName.Equals("createTask", StringComparison.InvariantCultureIgnoreCase))
            {
                // planId is always taken from the list's PlanId property, never a function arg.
                var planId = data.Properties["PlanId"];
                var title = Arg(context, 0);
                var notes = Arg(context, 1);
                var bucketId = Arg(context, 2);
                var dueDateTime = Arg(context, 3);
                var assigneeIds = Arg(context, 4);
                var priority = Arg(context, 5);

                CreateTask(data, planId, title, notes, bucketId, dueDateTime, assigneeIds, priority, ret);
            }
            else if (context.FunctionName.Equals("assignTaskToBucket", StringComparison.InvariantCultureIgnoreCase))
            {
                var taskId = Arg(context, 0);
                var bucketId = Arg(context, 1);

                AssignTaskToBucket(data, taskId, bucketId, ret);
            }

            return ret;
        }

        /// <summary>Reads the i-th function argument as a trimmed string, or "" if absent.</summary>
        private static string Arg(CustomListExecuteParameterContext context, int index)
        {
            if (context?.Values == null || index < 0 || index >= context.Values.Count)
                return string.Empty;
            return context.Values[index]?.StringValue?.Trim() ?? string.Empty;
        }

        private void CreateTask(CustomListData data, string planId, string title, string notes, string bucketId,
            string dueDateTime, string assigneeIds, string priority, CustomListExecuteReturnContext ret)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(planId))
                {
                    ret.Add("ERROR: the list's PlanId property is empty; set it to create tasks.");
                    return;
                }
                if (string.IsNullOrWhiteSpace(title))
                {
                    ret.Add("ERROR: title is required.");
                    return;
                }

                var bodyObj = new JObject
                {
                    ["planId"] = planId,
                    ["title"] = title,
                };
                if (!string.IsNullOrWhiteSpace(bucketId))
                    bodyObj["bucketId"] = bucketId;
                if (!string.IsNullOrWhiteSpace(dueDateTime))
                    bodyObj["dueDateTime"] = dueDateTime;
                if (!string.IsNullOrWhiteSpace(priority) && int.TryParse(priority, out var prio))
                    bodyObj["priority"] = prio;

                if (!string.IsNullOrWhiteSpace(assigneeIds))
                {
                    var assignments = new JObject();
                    foreach (var rawId in assigneeIds.Split(','))
                    {
                        var id = rawId.Trim();
                        if (string.IsNullOrWhiteSpace(id)) continue;
                        assignments[id] = new JObject
                        {
                            ["@odata.type"] = "#microsoft.graph.plannerAssignment",
                            ["orderHint"] = " !",
                        };
                    }
                    if (assignments.HasValues)
                        bodyObj["assignments"] = assignments;
                }

                using var http = MicrosoftGraphExtension.CreateGraphClient(data);
                using var content = new StringContent(bodyObj.ToString(Newtonsoft.Json.Formatting.None),
                    System.Text.Encoding.UTF8, "application/json");
                using var resp = http.PostAsync("planner/tasks", content).GetAwaiter().GetResult();
                var respBody = resp.Content.ReadAsStringAsync().GetAwaiter().GetResult();

                if (resp.IsSuccessStatusCode)
                {
                    var newId = JObject.Parse(respBody)["id"]?.ToString() ?? string.Empty;
                    this.Log.Info($"Created Planner task '{title}' ({newId}) in plan {planId}.");

                    // The description ("notes") can't be set on POST /planner/tasks — it lives on
                    // the task details sub-resource and needs a follow-up ETag PATCH. The task is
                    // already created at this point, so a notes failure is logged but does NOT
                    // turn the result into an error: we still return the new task ID.
                    if (!string.IsNullOrWhiteSpace(notes) && !string.IsNullOrWhiteSpace(newId))
                    {
                        var notesError = SetTaskDescription(http, newId, notes);
                        if (!string.IsNullOrEmpty(notesError))
                            this.Log.Warning($"Task {newId} created, but setting notes failed: {notesError}");
                    }

                    ret.Add(newId);
                }
                else
                {
                    this.Log.Warning($"createTask failed: HTTP {(int)resp.StatusCode}: {respBody}");
                    ret.Add($"ERROR: Microsoft Graph returned {(int)resp.StatusCode} {resp.ReasonPhrase}: {respBody}");
                }
            }
            catch (Exception ex)
            {
                this.Log.Error($"createTask threw: {ex.Message}");
                ret.Add($"ERROR: {ex.Message}");
            }
        }

        /// <summary>
        /// Sets a task's description ("notes"). The description lives on the task DETAILS
        /// sub-resource and uses optimistic concurrency, so this does a GET to read the
        /// details @odata.etag, then a PATCH with an If-Match header.
        /// Returns an empty string on success, or a human-readable error string on failure.
        /// </summary>
        private string SetTaskDescription(HttpClient http, string taskId, string notes)
        {
            try
            {
                using var getResp = http.GetAsync($"planner/tasks/{taskId}/details").GetAwaiter().GetResult();
                var getBody = getResp.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                if (!getResp.IsSuccessStatusCode)
                    return $"could not read task details: {(int)getResp.StatusCode} {getResp.ReasonPhrase}: {getBody}";

                var etag = JObject.Parse(getBody)["@odata.etag"]?.ToString();
                if (string.IsNullOrWhiteSpace(etag))
                    return "task details did not return an ETag.";

                using var patch = new HttpRequestMessage(new HttpMethod("PATCH"), $"planner/tasks/{taskId}/details");
                patch.Headers.TryAddWithoutValidation("If-Match", etag);
                var patchBody = new JObject { ["description"] = notes };
                patch.Content = new StringContent(patchBody.ToString(Newtonsoft.Json.Formatting.None),
                    System.Text.Encoding.UTF8, "application/json");

                using var patchResp = http.SendAsync(patch).GetAwaiter().GetResult();
                if (patchResp.IsSuccessStatusCode)
                    return string.Empty;

                var patchRespBody = patchResp.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                return $"{(int)patchResp.StatusCode} {patchResp.ReasonPhrase}: {patchRespBody}";
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }

        private void AssignTaskToBucket(CustomListData data, string taskId, string bucketId,
            CustomListExecuteReturnContext ret)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(taskId))
                {
                    ret.Add("ERROR: taskId is required.");
                    return;
                }
                if (string.IsNullOrWhiteSpace(bucketId))
                {
                    ret.Add("ERROR: bucketId is required.");
                    return;
                }

                using var http = MicrosoftGraphExtension.CreateGraphClient(data);

                // Planner uses optimistic concurrency: a PATCH must echo the task's current
                // @odata.etag back in an If-Match header, so we GET the task first.
                using var getResp = http.GetAsync($"planner/tasks/{taskId}").GetAwaiter().GetResult();
                var getBody = getResp.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                if (!getResp.IsSuccessStatusCode)
                {
                    this.Log.Warning($"assignTaskToBucket: could not read task {taskId}: HTTP {(int)getResp.StatusCode}: {getBody}");
                    ret.Add($"ERROR: could not read task {taskId}: {(int)getResp.StatusCode} {getResp.ReasonPhrase}: {getBody}");
                    return;
                }

                var etag = JObject.Parse(getBody)["@odata.etag"]?.ToString();
                if (string.IsNullOrWhiteSpace(etag))
                {
                    ret.Add($"ERROR: task {taskId} did not return an ETag; cannot update.");
                    return;
                }

                using var patch = new HttpRequestMessage(new HttpMethod("PATCH"), $"planner/tasks/{taskId}");
                patch.Headers.TryAddWithoutValidation("If-Match", etag);
                patch.Headers.TryAddWithoutValidation("Prefer", "return=representation");
                var patchBody = new JObject { ["bucketId"] = bucketId };
                patch.Content = new StringContent(patchBody.ToString(Newtonsoft.Json.Formatting.None),
                    System.Text.Encoding.UTF8, "application/json");

                using var patchResp = http.SendAsync(patch).GetAwaiter().GetResult();
                var patchRespBody = patchResp.Content.ReadAsStringAsync().GetAwaiter().GetResult();

                if (patchResp.IsSuccessStatusCode)
                {
                    this.Log.Info($"Moved task {taskId} to bucket {bucketId}.");
                    ret.Add("OK");
                }
                else
                {
                    this.Log.Warning($"assignTaskToBucket failed: HTTP {(int)patchResp.StatusCode}: {patchRespBody}");
                    ret.Add($"ERROR: Microsoft Graph returned {(int)patchResp.StatusCode} {patchResp.ReasonPhrase}: {patchRespBody}");
                }
            }
            catch (Exception ex)
            {
                this.Log.Error($"assignTaskToBucket threw: {ex.Message}");
                ret.Add($"ERROR: {ex.Message}");
            }
        }

        protected override void CheckDataOverride(CustomListData data)
        {
            if (string.IsNullOrWhiteSpace(data.Properties["TenantId"]))
                throw new InvalidOperationException("TenantId is required.");
            if (string.IsNullOrWhiteSpace(data.Properties["ClientId"]))
                throw new InvalidOperationException("ClientId is required.");
            if (string.IsNullOrWhiteSpace(data.Properties["ClientSecret"]))
                throw new InvalidOperationException("ClientSecret is required.");
            if (string.IsNullOrWhiteSpace(data.Properties["PlanId"]))
                throw new InvalidOperationException("PlanId is required.");
        }

        protected override CustomListColumnCollection GetColumnsOverride(CustomListData data)
        {
            return new CustomListColumnCollection
            {
                new CustomListColumn("id", CustomListColumnTypes.String),
                new CustomListColumn("title", CustomListColumnTypes.String),
                new CustomListColumn("notes", CustomListColumnTypes.String),
                new CustomListColumn("checklist", CustomListColumnTypes.String),
                new CustomListColumn("checklistTotal", CustomListColumnTypes.Number),
                new CustomListColumn("checklistChecked", CustomListColumnTypes.Number),
                new CustomListColumn("planId", CustomListColumnTypes.String),
                new CustomListColumn("bucketId", CustomListColumnTypes.String),
                new CustomListColumn("percentComplete", CustomListColumnTypes.Number),
                new CustomListColumn("priority", CustomListColumnTypes.Number),
                new CustomListColumn("dueDateTime", CustomListColumnTypes.String),
                new CustomListColumn("createdDateTime", CustomListColumnTypes.String),
                new CustomListColumn("completedDateTime", CustomListColumnTypes.String),
                new CustomListColumn("tags", CustomListColumnTypes.String),
                new CustomListColumn("assigneeIds", CustomListColumnTypes.String),
                new CustomListColumn("assigneeNames", CustomListColumnTypes.String),
                new CustomListColumn("assigneeCount", CustomListColumnTypes.Number),
            };
        }

        protected override CustomListObjectElementCollection GetItemsOverride(CustomListData data)
        {
            var planId = data.Properties["PlanId"].Trim();
            var url = $"planner/plans/{planId}/tasks";

            using var http = MicrosoftGraphExtension.CreateGraphClient(data);
            using var response = http.GetAsync(url).GetAwaiter().GetResult();
            var body = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();

            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException(
                    $"Microsoft Graph returned {(int)response.StatusCode} {response.ReasonPhrase} for /{url}: {body}");
            }

            this.Log.Info($"Fetched Planner tasks for plan {planId}.");

            var json = JObject.Parse(body);
            var tasks = json["value"] as JArray ?? new JArray();

            // First pass: collect the set of unique assignee user IDs across all tasks,
            // then resolve each one to a displayName via /users/{id} exactly once.
            // This keeps the Graph call count at O(unique users) instead of O(tasks * assignees).
            var taskAssignees = new List<List<string>>(tasks.Count);
            var uniqueUserIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var task in tasks)
            {
                var ids = ExtractAssigneeIds(task["assignments"] as JObject);
                taskAssignees.Add(ids);
                foreach (var id in ids) uniqueUserIds.Add(id);
            }

            var userNameLookup = ResolveUserDisplayNames(http, uniqueUserIds);

            // Tags: Planner stores per-task applied categories as { "category1": true, ... }.
            // The human-readable label lives on the PLAN, fetched once here.
            var categoryLabels = FetchPlanCategoryMap(http, planId);

            var items = new CustomListObjectElementCollection();
            for (int i = 0; i < tasks.Count; i++)
            {
                var task = tasks[i];
                var assigneeIds = taskAssignees[i];
                var assigneeNames = new List<string>(assigneeIds.Count);
                foreach (var id in assigneeIds)
                {
                    assigneeNames.Add(userNameLookup.TryGetValue(id, out var name) ? name : id);
                }

                var taskId = task["id"]?.ToString() ?? string.Empty;
                var details = FetchTaskDetails(http, taskId);
                var tags = ExtractTags(task["appliedCategories"] as JObject, categoryLabels);

                var item = new CustomListObjectElement();
                item.Add("id", taskId);
                item.Add("title", task["title"]?.ToString() ?? string.Empty);
                item.Add("notes", details.Description);
                item.Add("checklist", details.ChecklistJson);
                item.Add("checklistTotal", (double)details.ChecklistTotal);
                item.Add("checklistChecked", (double)details.ChecklistChecked);
                item.Add("planId", task["planId"]?.ToString() ?? string.Empty);
                item.Add("bucketId", task["bucketId"]?.ToString() ?? string.Empty);
                item.Add("percentComplete", task["percentComplete"]?.ToObject<double>() ?? 0);
                item.Add("priority", task["priority"]?.ToObject<double>() ?? 0);
                item.Add("dueDateTime", task["dueDateTime"]?.ToString() ?? string.Empty);
                item.Add("createdDateTime", task["createdDateTime"]?.ToString() ?? string.Empty);
                item.Add("completedDateTime", task["completedDateTime"]?.ToString() ?? string.Empty);
                item.Add("tags", string.Join(", ", tags));
                item.Add("assigneeIds", string.Join(",", assigneeIds));
                item.Add("assigneeNames", string.Join(", ", assigneeNames));
                item.Add("assigneeCount", (double)assigneeIds.Count);
                items.Add(item);
            }

            return items;
        }

        /// <summary>
        /// Holds the parts of a task's DETAILS sub-resource we surface as columns.
        /// </summary>
        private struct TaskDetails
        {
            public string Description;
            public string ChecklistJson;
            public int ChecklistTotal;
            public int ChecklistChecked;

            public static TaskDetails Empty => new TaskDetails
            {
                Description = string.Empty,
                ChecklistJson = "[]",
                ChecklistTotal = 0,
                ChecklistChecked = 0,
            };
        }

        /// <summary>
        /// Fetches a task's long description ("notes") and checklist. Planner keeps both
        /// on the task DETAILS sub-resource, and the tasks collection does not support
        /// $expand=details, so this is one extra Graph call per task — but description and
        /// checklist come back together, so the checklist is "free" on top of notes.
        /// Failures fall back to empty values so a single bad task never breaks the refresh.
        /// </summary>
        private TaskDetails FetchTaskDetails(HttpClient http, string taskId)
        {
            if (string.IsNullOrWhiteSpace(taskId)) return TaskDetails.Empty;
            try
            {
                using var resp = http.GetAsync($"planner/tasks/{taskId}/details?$select=description,checklist")
                    .GetAwaiter().GetResult();
                if (resp.IsSuccessStatusCode)
                {
                    var body = resp.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                    var json = JObject.Parse(body);
                    var result = TaskDetails.Empty;
                    result.Description = json["description"]?.ToString() ?? string.Empty;
                    BuildChecklist(json["checklist"] as JObject, ref result);
                    return result;
                }
                this.Log.Warning($"Task details for {taskId} returned HTTP {(int)resp.StatusCode}.");
            }
            catch (Exception ex)
            {
                this.Log.Warning($"Task details for {taskId} failed: {ex.Message}");
            }
            return TaskDetails.Empty;
        }

        /// <summary>
        /// Planner returns the checklist as an object keyed by checklist-item GUID, with
        /// odata noise. We flatten it to a clean JSON array that is easy to iterate in
        /// Peakboard: [ { "id", "title", "isChecked", "orderHint" }, ... ].
        /// Array order is not guaranteed by Graph; "orderHint" is included so the consumer
        /// can sort if display order matters.
        /// </summary>
        private static void BuildChecklist(JObject checklist, ref TaskDetails details)
        {
            var arr = new JArray();
            if (checklist != null)
            {
                foreach (var prop in checklist.Properties())
                {
                    if (!(prop.Value is JObject entry)) continue;
                    var isChecked = entry["isChecked"]?.ToObject<bool>() ?? false;
                    arr.Add(new JObject
                    {
                        ["id"] = prop.Name,
                        ["title"] = entry["title"]?.ToString() ?? string.Empty,
                        ["isChecked"] = isChecked,
                        ["orderHint"] = entry["orderHint"]?.ToString() ?? string.Empty,
                    });
                    details.ChecklistTotal++;
                    if (isChecked) details.ChecklistChecked++;
                }
            }
            details.ChecklistJson = arr.ToString(Newtonsoft.Json.Formatting.None);
        }

        /// <summary>
        /// Reads the plan's categoryDescriptions, i.e. the mapping of category slot
        /// ("category1".."category25") to its label text. Fetched once per refresh.
        /// </summary>
        private Dictionary<string, string> FetchPlanCategoryMap(HttpClient http, string planId)
        {
            var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            try
            {
                using var resp = http.GetAsync($"planner/plans/{planId}/details?$select=categoryDescriptions")
                    .GetAwaiter().GetResult();
                if (resp.IsSuccessStatusCode)
                {
                    var body = resp.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                    var cats = JObject.Parse(body)["categoryDescriptions"] as JObject;
                    if (cats != null)
                    {
                        foreach (var prop in cats.Properties())
                        {
                            var label = prop.Value?.ToString();
                            if (!string.IsNullOrWhiteSpace(label))
                            {
                                map[prop.Name] = label;
                            }
                        }
                    }
                }
                else
                {
                    this.Log.Warning($"Plan details for {planId} returned HTTP {(int)resp.StatusCode}; tags will show raw category slots.");
                }
            }
            catch (Exception ex)
            {
                this.Log.Warning($"Plan details for {planId} failed: {ex.Message}; tags will show raw category slots.");
            }
            return map;
        }

        /// <summary>
        /// Maps a task's appliedCategories object to a list of label strings.
        /// Only categories set to true are returned. Resolution order per slot:
        ///   1. the custom text label defined on the plan (categoryDescriptions), if any;
        ///   2. otherwise the slot's default Planner colour name ("Blue", "Pink", ...),
        ///      which is exactly what the Planner UI shows for an unlabelled category;
        ///   3. otherwise the raw slot name (e.g. "category27") so nothing is ever lost.
        /// </summary>
        private static List<string> ExtractTags(JObject appliedCategories, Dictionary<string, string> categoryLabels)
        {
            var tags = new List<string>();
            if (appliedCategories == null) return tags;

            foreach (var prop in appliedCategories.Properties())
            {
                if (prop.Value?.Type == JTokenType.Boolean && prop.Value.ToObject<bool>())
                {
                    if (categoryLabels.TryGetValue(prop.Name, out var label) && !string.IsNullOrWhiteSpace(label))
                        tags.Add(label);
                    else if (DefaultCategoryColors.TryGetValue(prop.Name, out var color))
                        tags.Add(color);
                    else
                        tags.Add(prop.Name);
                }
            }
            return tags;
        }

        /// <summary>
        /// Looks up displayName for each user ID via /users/{id}?$select=displayName.
        /// Deleted or inaccessible users silently fall back to their GUID in the caller.
        /// One Graph call per unique user — Planner plans typically have a small assignee pool,
        /// so this stays well below any throttling threshold.
        /// </summary>
        private Dictionary<string, string> ResolveUserDisplayNames(HttpClient http, HashSet<string> userIds)
        {
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var id in userIds)
            {
                if (string.IsNullOrWhiteSpace(id)) continue;
                try
                {
                    using var resp = http.GetAsync($"users/{id}?$select=displayName").GetAwaiter().GetResult();
                    if (resp.IsSuccessStatusCode)
                    {
                        var body = resp.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                        var name = JObject.Parse(body)["displayName"]?.ToString();
                        if (!string.IsNullOrWhiteSpace(name))
                        {
                            result[id] = name;
                            continue;
                        }
                    }
                    else
                    {
                        this.Log.Warning($"User lookup for {id} returned HTTP {(int)resp.StatusCode}.");
                    }
                }
                catch (Exception ex)
                {
                    this.Log.Warning($"User lookup for {id} failed: {ex.Message}");
                }
                // Caller will fall back to the GUID if a name is not in the dictionary.
            }
            return result;
        }

        /// <summary>
        /// The Planner /tasks endpoint returns assignments as an object keyed by user id.
        /// We flatten the keys to a list of user GUIDs.
        /// </summary>
        private static List<string> ExtractAssigneeIds(JObject assignments)
        {
            var ids = new List<string>();
            if (assignments == null) return ids;

            foreach (var prop in assignments.Properties())
            {
                if (!string.IsNullOrWhiteSpace(prop.Name))
                {
                    ids.Add(prop.Name);
                }
            }
            return ids;
        }
    }
}
