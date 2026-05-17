# Peakboard Extension: Microsoft Graph

This extension connects Peakboard to [Microsoft Graph](https://learn.microsoft.com/en-us/graph/overview), the unified API surface for Microsoft 365 and Microsoft Entra ID (Azure AD). It ships custom lists for reading users and Microsoft 365 groups from your tenant, plus Microsoft Planner data (plans, buckets and tasks).

The Planner data sources are designed to be chained: start with **Microsoft 365 Groups** to find the `GroupId`, then **Planner Plans** to find a `PlanId`, then **Planner Tasks** (and optionally **Planner Buckets**) to drive the dashboard.

## Data Sources

### Microsoft Graph Users

Lists users from your tenant via `GET /users` (top N).

| Parameter | Description |
|-----------|-------------|
| TenantId | Microsoft Entra (Azure AD) tenant ID, e.g. `00000000-0000-0000-0000-000000000000` |
| ClientId | App registration (application) ID |
| ClientSecret | Client secret value (masked) |
| MaxRows | Maximum number of users to return (1 - 999, default 50) |

**Output columns**

| Column | Type | Description |
|--------|------|-------------|
| id | String | Object ID (GUID) of the user |
| displayName | String | Display name |
| userPrincipalName | String | UPN, e.g. `alice@contoso.com` |
| mail | String | Primary SMTP address (may be empty for unlicensed users) |
| jobTitle | String | Job title (may be empty) |
| accountEnabled | Boolean | Whether the account is enabled |

---

### Microsoft 365 Groups

Lists Microsoft 365 ("Unified") groups in your tenant via `GET /groups`. Use this to discover the `GroupId` values you'll plug into the Planner Plans list. Security groups and distribution lists are excluded — only groups that can own Planner plans or Teams are returned.

| Parameter | Description |
|-----------|-------------|
| TenantId, ClientId, ClientSecret | Same as above |
| MaxRows | Maximum number of groups to return (1 - 999, default 100) |

**Output columns**

| Column | Type | Description |
|--------|------|-------------|
| id | String | Group ID (use this as the `GroupId` in the Planner Plans list) |
| displayName | String | Group display name |
| description | String | Description (may be empty) |
| mail | String | Group primary SMTP address |
| visibility | String | `Public`, `Private`, or `HiddenMembership` |
| isTeamsEnabled | Boolean | True if a Microsoft Teams team is also provisioned on this group |
| createdDateTime | String | ISO 8601 timestamp |

---

### Microsoft Planner Plans

Lists the Planner plans owned by a given Microsoft 365 group / Team via `GET /groups/{groupId}/planner/plans`. Use this to discover the `planId` values you'll plug into the Buckets and Tasks lists.

| Parameter | Description |
|-----------|-------------|
| TenantId, ClientId, ClientSecret | Same as above |
| GroupId | Object ID of the Microsoft 365 group / Team that owns the plans |

**Output columns**

| Column | Type | Description |
|--------|------|-------------|
| id | String | Plan ID (use this for the Buckets and Tasks lists) |
| title | String | Plan title |
| owner | String | Owning group ID |
| createdDateTime | String | ISO 8601 timestamp |

---

### Microsoft Planner Buckets

Lists the buckets (board columns) inside a plan via `GET /planner/plans/{planId}/buckets`. Join these with the Tasks list on `bucketId` if you want friendly bucket names in your dashboard.

| Parameter | Description |
|-----------|-------------|
| TenantId, ClientId, ClientSecret | Same as above |
| PlanId | The plan ID (from the Plans list) |

**Output columns**

| Column | Type | Description |
|--------|------|-------------|
| id | String | Bucket ID |
| name | String | Bucket display name |
| planId | String | Plan this bucket belongs to |
| orderHint | String | Order hint used by Planner to sort buckets |

---

### Microsoft Planner Tasks

Lists tasks inside a plan via `GET /planner/plans/{planId}/tasks`. This is the main read endpoint for Planner dashboards.

| Parameter | Description |
|-----------|-------------|
| TenantId, ClientId, ClientSecret | Same as above |
| PlanId | The plan ID (from the Plans list) |

**Output columns**

| Column | Type | Description |
|--------|------|-------------|
| id | String | Task ID |
| title | String | Task title |
| notes | String | The task's long description / notes (from the task details sub-resource). Empty if the task has no description |
| checklist | String | The task's checklist as a **JSON array string** — see shape below. `[]` if there is no checklist |
| checklistTotal | Number | Number of checklist items |
| checklistChecked | Number | Number of checklist items that are checked (use with `checklistTotal` for a progress ratio without parsing the JSON) |
| planId | String | Plan this task belongs to |
| bucketId | String | Bucket the task is in (join against the Buckets list) |
| percentComplete | Number | 0 = not started, 50 = in progress, 100 = complete |
| priority | Number | 1 = urgent, 3 = important, 5 = medium, 9 = low |
| dueDateTime | String | ISO 8601, empty if no due date |
| createdDateTime | String | ISO 8601 |
| completedDateTime | String | ISO 8601, empty if not yet completed |
| tags | String | Comma-separated Planner category labels applied to the task. If a category has a custom label on the plan that text is used (e.g. `Urgent, Bug`); if it is just a colour with no custom label, the colour name is used exactly as the Planner UI shows it (e.g. `Blue, Pink`) |
| assigneeIds | String | Comma-separated user GUIDs (join against the Users list) |
| assigneeNames | String | Comma-separated user display names, resolved from Microsoft Graph. Falls back to the GUID if a user has been deleted or cannot be read |
| assigneeCount | Number | How many users are assigned |

> **Note on `assigneeNames`**: each unique assignee triggers one additional `GET /users/{id}` call per refresh (de-duplicated, so a 50-task plan with 5 distinct assignees costs 5 extra calls, not 50). For very large plans you can ignore `assigneeNames` and join the `assigneeIds` GUIDs against the **Users** custom list inside Peakboard instead.
>
> **Note on `notes` / `checklist`**: both live on the task *details* sub-resource, which the Planner API does **not** allow to be expanded inline. Each task therefore costs one extra `GET /planner/tasks/{id}/details` call per refresh (this one cannot be de-duplicated). A 50-task plan = 50 extra calls. On large, frequently-refreshed plans this adds latency and Graph throttling risk. The good news: `notes`, `checklist`, `checklistTotal` and `checklistChecked` all come from that **same single call**, so adding the checklist costs nothing extra on top of notes. `tags` also has no extra cost: the category label map is fetched once per plan per refresh.

**`checklist` JSON shape** — a string containing a JSON array, one object per checklist item:

```json
[
  { "id": "<guid>", "title": "Order parts", "isChecked": true,  "orderHint": "8585..." },
  { "id": "<guid>", "title": "Test rig",    "isChecked": false, "orderHint": "8586..." }
]
```

In Peakboard, parse it with the JSON functions to iterate items, or just bind `checklistChecked` / `checklistTotal` directly for a progress indicator. Array order is not guaranteed by Microsoft Graph — sort by `orderHint` (ascending, ordinal string compare) if you need Planner's display order.

**Functions (write operations)**

The Planner Tasks list also exposes two callable functions, invoked from a Peakboard button/script (not on data refresh). Both require the `Tasks.ReadWrite.All` application permission.

#### `createTask`

Creates a new Planner task via `POST /planner/tasks`.

| Input | Required | Description |
|-------|----------|-------------|
| planId | No | Plan to create the task in. If left empty, the list's `PlanId` property is used |
| title | **Yes** | Task title |
| bucketId | No | Bucket to place the task in |
| dueDateTime | No | Due date, ISO 8601 (e.g. `2026-06-01T00:00:00Z`) |
| assigneeIds | No | Comma-separated user GUIDs to assign |
| priority | No | `1` urgent, `3` important, `5` medium, `9` low |

| Return | Description |
|--------|-------------|
| success | `"true"` or `"false"` |
| taskId | ID of the newly created task (empty on failure) |
| message | `OK`, or the Graph error detail |

#### `assignTaskToBucket`

Moves an existing task to another bucket via `PATCH /planner/tasks/{id}`.

| Input | Required | Description |
|-------|----------|-------------|
| taskId | **Yes** | ID of the task to move |
| bucketId | **Yes** | ID of the target bucket |

| Return | Description |
|--------|-------------|
| success | `"true"` or `"false"` |
| message | `OK`, or the Graph error detail |

> Planner uses optimistic concurrency. `assignTaskToBucket` first does a `GET /planner/tasks/{id}` to read the current `@odata.etag`, then sends the `PATCH` with an `If-Match` header carrying that ETag. This is handled internally — callers just pass `taskId` and `bucketId`. If the task is modified by someone else between the GET and the PATCH, Graph returns `412 Precondition Failed` and `message` will say so; simply call the function again.

> `success` is returned as the string `"true"` / `"false"` (Peakboard function string return). Compare against `"true"` in your dashboard logic.

## Authentication

This extension uses the **OAuth 2.0 client credentials flow** (app-only authentication). The extension authenticates *as the app itself* — no interactive user sign-in is required at refresh time, which means dashboards can refresh unattended.

### Setting up the Azure AD app

1. In the [Azure portal](https://portal.azure.com/), open **Microsoft Entra ID → App registrations → New registration**.
2. Give it a name (e.g. `Peakboard Graph Connector`), leave the redirect URI empty, and register.
3. Note the **Application (client) ID** and **Directory (tenant) ID** shown on the overview page.
4. Under **Certificates & secrets → Client secrets**, create a new client secret and copy the **Value** immediately (it is shown only once).
5. Under **API permissions**, click **Add a permission → Microsoft Graph → Application permissions** and add the permissions you need:
   - `User.Read.All` — required for the Users list and for resolving `assigneeNames` in Planner Tasks
   - `Group.Read.All` — required for the Microsoft 365 Groups list
   - `Tasks.Read.All` — required for *reading* the Planner Plans, Buckets and Tasks lists
   - `Tasks.ReadWrite.All` — required for the `createTask` and `assignTaskToBucket` functions (this permission also covers reads, so you do not additionally need `Tasks.Read.All` if you grant this)
6. Click **Grant admin consent for &lt;tenant&gt;** (an administrator must do this once).

You only need to grant the permissions for the lists you actually use. The same app registration can be reused across all five lists.

### Why application permissions, not delegated?

Application permissions let the extension query org-wide data without a logged-in user. This matches how a Peakboard dashboard works — it refreshes on a timer, in the background. Delegated permissions would require interactive sign-in and refresh-token management, which is impractical inside an unattended dashboard.

The trade-off: there is no "current user" concept, so endpoints like `/me` and `/me/planner/tasks` are not available. Use the explicit forms instead (`/users/{id}`, `/users/{id}/planner/tasks`) — or in this extension, query Planner data by `planId`.

### Adding more endpoints later

Additional custom lists can be added by:

1. Granting the required application permission on the same app registration (e.g. `Group.Read.All`, `Tasks.ReadWrite.All` for writes).
2. Adding a new `CustomListBase` class that reuses `MicrosoftGraphExtension.CreateGraphClient(data)` to acquire a pre-authenticated `HttpClient`.

## Notes

- Access tokens have a default lifetime of ~1 hour. The extension acquires a fresh token on every refresh, so token expiry is never an issue in practice.
- Firewall: the extension calls `https://login.microsoftonline.com` and `https://graph.microsoft.com`. Both must be reachable from the host running Peakboard.
- All date/time columns are returned as ISO 8601 strings (e.g. `2026-05-15T13:45:00Z`). Use Peakboard's date functions to format or filter them.
