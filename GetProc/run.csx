#r "SqlClientHelpers.dll"
using System.Net;
using System;
using System.Data;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public static string Env(string name) => System.Environment.GetEnvironmentVariable(name /*, EnvironmentVariableTarget.Process*/);

public static JArray DataTableToJson(this DataTableCollection tables)
{
  var tablesArray = new JArray();

  foreach (DataTable t in tables)
  {
    JArray rowsArray = new JArray();
    JObject row;
    foreach (System.Data.DataRow dr in source.Rows)
    {
      row = new JObject();
      foreach (System.Data.DataColumn col in source.Columns)
      {
        row.Add(col.ColumnName.Trim(), JToken.FromObject(dr[col]));
      }
      rowsArray.Add(row);
    }
    tablesArray.Add(rowsArray);
  }
  return tablesArray;
}

private static TraceWriter traceLog;
public static async Task<HttpResponseMessage> Run(HttpRequestMessage req, TraceWriter log)
{
    traceLog = log;

    traceLog.Info("[Run]");
  /*
  log.Info($"C# HTTP trigger function processed a request. RequestUri={req.RequestUri}");

  // parse query parameter
  string name = req.GetQueryNameValuePairs()
      .FirstOrDefault(q => string.Compare(q.Key, "name", true) == 0)
      .Value;

  // Get request body
  dynamic data = await req.Content.ReadAsAsync<object>();

  // Set name to query string or body data
  name = name ?? data?.name;

  return name == null
      ? req.CreateResponse(HttpStatusCode.BadRequest, "Please pass a name on the query string or in the request body")
      : req.CreateResponse(HttpStatusCode.OK, "Hello " + name);
  */

  var parms = req.GetQueryNameValuePairs();

  SqlClientHelpers.Proc.DefaultConnectionString = $"User ID={Env.ScriptArgs[0]};Password={Env.ScriptArgs[1]};Initial Catalog=SlingshotAX;Data Source={Env.ScriptArgs[2]};";

  using (var proc = new SqlClientHelpers.Proc($"dbo.{parms["procName"]}"))
    proc.AssignValues(parms);

  return req.CreateResponse(HttpStatusCode.OK, DataTableToJson(proc.ExecDataSet().Tables), 
    System.Net.Http.Formatting.JsonMediaTypeFormatter.DefaultMediaType);
}
