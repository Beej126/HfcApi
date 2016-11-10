#r "D:\home\site\wwwroot\GetProc\SqlClientHelpers.dll"
#r "System.Data"
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
    foreach (System.Data.DataRow dr in t.Rows)
    {
      row = new JObject();
      foreach (var col in t.Columns)
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

  /*
  SqlClientHelpers.Proc.DefaultConnectionString = $"User ID={Env("sqluser")};Password={Env("sqlpassword")};Initial Catalog=SlingshotAX;Data Source={Env("sqlserver")};";
  traceLog.Info($"DefaultConnectionString: {SqlClientHelpers.Proc.DefaultConnectionString}");

  using (var proc = new SqlClientHelpers.Proc($"dbo.{parms.Get("procName")}")) { 
    var sqlparms = proc.Parameters;
    foreach(var nv in parms) { 
      if (sqlparms.Contains(nv.Name)) {
        traceLog.Info($"name: {nv.Name}, value: {nv.Value}");
        proc[nv.Name] = nv.Value;
      }
    }
  }

  return req.CreateResponse(HttpStatusCode.OK, 
    DataTableToJson(proc.ExecDataSet().Tables).ToString(), 
    System.Net.Http.Formatting.JsonMediaTypeFormatter.DefaultMediaType);
    */
  req.CreateResponse(HttpStatusCode.OK, "testing");
}
